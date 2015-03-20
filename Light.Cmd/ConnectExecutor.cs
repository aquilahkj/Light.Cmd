using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;


namespace Light.Cmd
{
	class ConnectExecutor : BaseExecutor
	{
		static readonly int DEFAULT_PORT = 13818;

		public static void ConnectionCommand (WorkCommand command, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			ConnectExecutor executor = new ConnectExecutor (command, callback);
			executor.Execute ();
		}

		string _host = null;

		int _port = DEFAULT_PORT;

		int _retryTimes = 30;

		int _currentTimes = 0;

		ProcessConnection _process = null;

		bool _isClose = false;

		string _id = null;

		string _key = null;

		ConnectExecutor (WorkCommand command, CallbackHandler callback)
			: base (command, callback)
		{
			Dictionary<string, string> dict = Utility.ParseCommandLine (_command.CommandCode);

			if (dict.ContainsKey ("host")) {
				_host = dict ["host"];
			}
			if (string.IsNullOrEmpty (_host)) {
				throw new Exception ("host is not exists");
			}
			if (!Regex.IsMatch (_host, HOST_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase)) {
				throw new Exception ("host is not correct");
			}

			if (dict.ContainsKey ("port")) {
				if (!int.TryParse (dict ["port"], out _port)) {
					throw new Exception ("port is not correct");
				}
				if (_port <= 0 || _port >= 65535) {
					throw new Exception ("port range is not correct");
				}
			}

			if (dict.ContainsKey ("retryTiems")) {
				if (!int.TryParse (dict ["retryTiems"], out _retryTimes)) {
					throw new Exception ("retryTimes is not correct");
				}
			}

			if (dict.ContainsKey ("id")) {
				_id = dict ["id"];
				_key = dict ["key"];
			}
		}

		void process_OnSocketException (object sender, SocketExceptionEventArgs args)
		{
			if (!_isClose) {
				ConnectSocket ();
			}
		}

		void process_OnProcessExit (object sender, EventArgs args)
		{
			_isClose = true;
			_process.OnSocketException -= new SocketExceptionEventHandler (process_OnSocketException);
			_process.OnProcessExit -= new ProcessExitEventHandler (process_OnProcessExit);
			_process.Dispose ();
			_process = null;
			RaiseCallback (CallbackType.COMPLETE, "request complete");
		}

		public void Execute ()
		{
			Socket socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			if (!string.IsNullOrEmpty (_id)) {
				_process = new ProcessConnection (_id, _key, socket);
			}
			else {
				_process = new ProcessConnection (socket);
			}
			_process.OnProcessExit += new ProcessExitEventHandler (process_OnProcessExit);
			_process.OnSocketException += new SocketExceptionEventHandler (process_OnSocketException);
			socket.BeginConnect (_host, _port, new AsyncCallback (ConnectCallBack), socket);
		}

		/// <summary>
		/// BeginConnnect的回调函数
		/// </summary>
		/// <param name="ar">IAsyncResult接口实例</param>
		private void ConnectCallBack (IAsyncResult ar)
		{
			Socket socket = (Socket)ar.AsyncState;
			bool success;
			string message = null;
			try {
				socket.EndConnect (ar);
				success = socket.Connected;
			}
			catch (Exception e) {
				//RaiseCallback(CallbackType.ERROR, e.Message);
				message = e.Message;
				success = false;
			}

			try {
				if (!success) {
					if (_currentTimes > _retryTimes) {
						_process.OnSocketException -= new SocketExceptionEventHandler (process_OnSocketException);
						_process.OnProcessExit -= new ProcessExitEventHandler (process_OnProcessExit);
						_process.Dispose ();
						_process = null;
						RaiseCallback (CallbackType.ERROR, message);
					}
					else {
						_currentTimes++;
						Timer timer = new Timer (60 * 1000);
						timer.AutoReset = false;
						timer.Elapsed += new ElapsedEventHandler (timer_Elapsed);
						timer.Start ();
					}
				}
				else {
					if (_process != null) {
						_currentTimes = 0;
						//_process.BindSocket(socket);
					}
				}
			}
			catch (Exception e) {
				RaiseCallback (CallbackType.ERROR, e.Message);
			}
		}

		void timer_Elapsed (object sender, ElapsedEventArgs e)
		{
			Timer timer = sender as Timer;
			if (timer == null) {
				return;
			}
			ConnectSocket ();
			timer.Stop ();
			timer.Close ();
		}

		void ConnectSocket ()
		{
			Socket socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.BeginConnect (_host, _port, new AsyncCallback (ConnectCallBack), socket);
		}
	}
}
