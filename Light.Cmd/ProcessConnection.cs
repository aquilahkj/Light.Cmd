using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Light.Cmd
{
	delegate void SocketExceptionEventHandler (object sender, SocketExceptionEventArgs args);

	delegate void ProcessExitEventHandler (object sender, EventArgs args);

	class ProcessConnection : IDisposable
	{
		public event SocketExceptionEventHandler OnSocketException;

		public event ProcessExitEventHandler OnProcessExit;

		ReadWriteStream _stream = new ReadWriteStream ();

		int _dataLength = -1;

		Process _process = null;

		Socket _socket = null;

		bool _socketEnable = false;

		bool _isLogon = false;

		bool _isExit = false;

		public bool IsConnnect {
			get {
				return _socketEnable;
			}
		}

		public bool IsProcessRun {
			get {
				lock (this) {
					if (_isExit) {
						return false;
					}
					if (_process == null) {
						return false;
					}
					else {
						return !_process.HasExited;
					}
				}
			}
		}

		byte[] _buffer = new byte[4096];

		string _id = null;

		string _key = null;

		DateTime _lastTime = DateTime.Now;

		string _socketEndPoint = null;

		//int _checkDuringTime = 5000;

		public ProcessConnection (Socket socket)
		{
			BindSocket (socket);
			_isLogon = false;
		}

		public ProcessConnection (string id, string key, Socket socket)
		{
			if (string.IsNullOrEmpty (id)) {
				throw new ArgumentNullException ("id");
			}
			if (string.IsNullOrEmpty (key)) {
				throw new ArgumentNullException ("key");
			}
			_id = id;
			_key = key;
			BindSocket (socket);
			_isLogon = false;
		}

		private Process CreateProcess ()
		{
			string cmdName = Utility.GetCmdName ();
			ProcessStartInfo startInfo = new ProcessStartInfo (cmdName);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			Encoding encoding = ConfigExecutor.GetEncoding ();
			if (encoding != null) {
				startInfo.StandardErrorEncoding = encoding;
				startInfo.StandardOutputEncoding = encoding;
			}

			Process process = null;
			try {
				process = new Process ();
				process.StartInfo = startInfo;
				process.EnableRaisingEvents = true;
				process.Start ();

				process.Exited += new EventHandler (process_Exited);
				process.OutputDataReceived += new DataReceivedEventHandler (process_OutputDataReceived);
				process.ErrorDataReceived += new DataReceivedEventHandler (process_ErrorDataReceived);

				process.BeginOutputReadLine ();
				process.BeginErrorReadLine ();
			}
			catch (Exception e) {
				SentOutputData (e.Message);
			}
			return process;
			//_process = process;
		}

		private void BindSocket (Socket socket)
		{
			if (socket == null) {
				throw new ArgumentNullException ("socket");
			}

			if (_socketEnable) {
				CloseSocket (_socket);
			}
			IPEndPoint ipEndpoint = socket.RemoteEndPoint as IPEndPoint;
			_socketEndPoint = string.Format ("{0}:{1}", ipEndpoint.Address, ipEndpoint.Port);
			socket.BeginReceive (_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback (ReceiveSocketInputCallback), socket);
			_socket = socket;
			_socketEnable = true;
			_isExit = false;
          
		}

		public void SendAuthData ()
		{
			if (_id != null) {
				_isLogon = false;
				SentOutputData (_id);
			}
			else {
				_isLogon = true;
				SentOutputData ("anonymous authentication");
			}
		}

		private void CommandInput (string data)
		{
			try {
				WorkCommand command = CommandParser.ParseCommnd (data);
				if (command != null) {
					if (command.CmdFlag == CommandProcessor.SHELL && command.Timespan < 0) {
						Process process = null;
						lock (this) {
							if (_process == null) {
								_process = CreateProcess ();
							}
							process = _process;
						}
						if (process != null) {
							process.StandardInput.WriteLine (command.CommandCode);
						}
					}
					else {
						CommandProcessor.ProcessCommand (command, CommandCallback);
					}
				}
			}
			catch (Exception e) {
				SentOutputData (e.Message);
			}

		}

		private void ProcessInput (string data)
		{
			string cmd = null;
			if (data == null) {
				return;
			}
			cmd = data.Trim ();
			if (cmd == string.Empty) {
				return;
			}
			if (!_isLogon) {
				lock (this) {
					if (!_isLogon) {
						if (cmd == _key) {
							_isLogon = true;
							SentOutputData ("process state:ok");
							return;
						}
						else {
							cmd = CommandProcessor.EXIT;
						}
					}
				}
			}


			if (cmd == CommandProcessor.EXIT) {
				lock (this) {
					_isExit = true;
				}
				ReleaseProcess ();
				if (OnProcessExit != null) {
					OnProcessExit (this, new EventArgs ());
				}
			}
			else if (cmd == CommandProcessor.RESET) {
				ReleaseProcess ();
			}
			else {
				CommandInput (cmd);
			}
		}

		private void ReleaseProcess ()
		{
			Process process = null;
			lock (this) {
				if (_process != null) {
					process = _process;
					_process = null;
				}
			}

			if (process != null) {
				try {
					process.StandardInput.WriteLine ("exit");
					process.WaitForExit (1000 * 5);
					if (!process.HasExited) {
						process.Kill ();
					}
				}
				catch (Exception e) {
					SentOutputData (e.Message);
					return;
				}
				finally {
					process.Exited -= new EventHandler (process_Exited);
					process.ErrorDataReceived -= new DataReceivedEventHandler (process_ErrorDataReceived);
					process.OutputDataReceived -= new DataReceivedEventHandler (process_OutputDataReceived);
					process.Dispose ();
				}
			}

		}

		private void process_Exited (object sender, EventArgs e)
		{
			SentOutputData ("process exit");
		}

		private void process_OutputDataReceived (object sender, DataReceivedEventArgs e)
		{
			//Process process = sender as Process;
			//process.Refresh();
			//process.StandardInput.WriteLine();
			//Console.WriteLine(e.Data);
			SentOutputData (e.Data);
		}

		private void process_ErrorDataReceived (object sender, DataReceivedEventArgs e)
		{
			//Process process = sender as Process;
			//process.Refresh();
			//process.StandardInput.WriteLine();
			SentOutputData (e.Data);
		}

		private void CommandCallback (WorkCommand command, CallbackType type, string message)
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("{0}\t{1}\t{2}:", command.CmdFlag, command.CommandCode, type.ToString ());
			sb.AppendLine ();
			sb.AppendLine (message);
			SentOutputData (sb.ToString ());
		}

		private void SentOutputData (string data)
		{
			if (!string.IsNullOrEmpty (data)) {
				Socket socket = _socket;
				//Socket socket = null;
				//lock (this)
				//{
				//    if (_socketEnable)
				//    {
				//        socket = _socket;
				//    }
				//    else
				//    {
				//        return;
				//    }
				//}
				if (socket != null && socket.Connected) {
					try {
						byte[] databuf = Encoding.UTF8.GetBytes (data);
						byte[] totalbuf = new byte[databuf.Length + 4];
						byte[] lenbuf = BitConverter.GetBytes (databuf.Length);
						Buffer.BlockCopy (lenbuf, 0, totalbuf, 0, 4);
						Buffer.BlockCopy (databuf, 0, totalbuf, 4, databuf.Length);
						socket.Send (totalbuf);
						_lastTime = DateTime.Now;
					}
					catch (Exception e) {
						//if (_isExit)
						//{
						//    return;
						//}
						//CloseSocket(socket);
						//if (OnSocketException != null)
						//{
						//    SocketExceptionEventArgs args = new SocketExceptionEventArgs(e, "send data error");
						//    OnSocketException.BeginInvoke(this, args, null, null);
						//}
					}
				}
			}
		}

		/// <summary>
		/// ReceiveData的回调函数
		/// </summary>
		/// <param name="ar">IAsyncResult实例</param>
		private void ReceiveSocketInputCallback (IAsyncResult ar)
		{
			Socket socket = ar.AsyncState as Socket;//获得调用时传递的StateObject对象
			try {
				int len = socket.EndReceive (ar);
				_lastTime = DateTime.Now;
				if (len > 0) {//判断是否接受到了新信息
					_stream.Write (_buffer, 0, len);
					byte[] lenbuf = new byte[4];
					while (true) {
						if (_dataLength == -1) {
							if (_stream.Length > 4) {
								_stream.Read (lenbuf, 0, 4);
								_dataLength = BitConverter.ToInt32 (lenbuf, 0);
							}
						}

						if (_dataLength >= 0 && _stream.Length >= _dataLength) {
							byte[] strbuf = new byte[_dataLength];
							_stream.Read (strbuf, 0, _dataLength);
							string data = Encoding.UTF8.GetString (strbuf);
							ProcessInput (data);
							_dataLength = -1;
						}
						else if (_dataLength == 0) {
							_dataLength = -1;
						}
						else {
							break;
						}
					}
					socket.BeginReceive (_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback (ReceiveSocketInputCallback), socket);
				}
				else {
					//Console.WriteLine("receive len " + len + " " + DateTime.Now.ToString("HH:mm:ss.fff"));
					//CloseSocket(socket);
					//Console.WriteLine("CloseSocket " + DateTime.Now.ToString("HH:mm:ss.fff"));
					//ReleaseProcess();
					//Console.WriteLine("ReleaseProcess " + DateTime.Now.ToString("HH:mm:ss.fff"));
					if (OnProcessExit != null) {
						OnProcessExit (this, new EventArgs ());
						// Console.WriteLine("OnProcessExit " + len + " " + DateTime.Now.ToString("HH:mm:ss.fff"));
					}
				}
			}
			catch (Exception e) {
				if (!IsProcessRun) {
					return;
				}
				//CloseSocket(socket);
				if (OnSocketException != null) {
					SocketExceptionEventArgs args = new SocketExceptionEventArgs (e, "receive data error");
					OnSocketException.BeginInvoke (this, args, null, null);
				}
			}
		}

		private void CloseSocket (Socket socket)
		{
			if (socket == null) {
				return;
			}
			lock (this) {
				if (_socketEnable) {
					_socketEnable = false;
					_isLogon = false;
					socket.Close ();
				}
			}
		}

		public string SocketEndPoint {
			get {
				return _socketEndPoint;
			}
		}

		#region dispose

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!m_disposed) {
				if (disposing) {

				}
				//Console.WriteLine("dispose start");
				_stream.Dispose ();
				CloseSocket (_socket);
				ReleaseProcess ();

				_socket = null;
				m_disposed = true;
			}
		}

		~ProcessConnection ()
		{
			Dispose (false);
		}

		private bool m_disposed;

		#endregion
	}
}