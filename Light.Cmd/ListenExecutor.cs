using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Light.Cmd
{
	class ListenExecutor : BaseExecutor
	{
		public static readonly int DEFAULT_PORT = 3818;

		static Dictionary<string, ListenExecutor> listenList = new Dictionary<string, ListenExecutor> ();

		public static ListenExecutor GetListenExecutor (string listenEndPoint)
		{
			if (listenEndPoint == null) {
				throw new ArgumentNullException ("listenEndPoint");
			}
			if (listenList.ContainsKey (listenEndPoint)) {
				return listenList [listenEndPoint];
			}
			else {
				return null;
			}
		}

		public static void ListenCommand (WorkCommand command, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			ListenExecutor executor = new ListenExecutor (command, callback);
			executor.Execute ();
		}

		Socket _listen;

		string _ip = null;

		int _port = DEFAULT_PORT;

		int _during = 0;

		string _id = null;

		string _key = null;

		bool _isClose = false;

		DateTime _lastTime = DateTime.Now;

		Dictionary<ProcessConnection, DateTime> _processDict = new Dictionary<ProcessConnection, DateTime> ();

		ListenExecutor (WorkCommand command, CallbackHandler callback)
			: base (command, callback)
		{
			Dictionary<string, string> dict = Utility.ParseCommandLine (_command.CommandCode);

			if (dict.ContainsKey ("ip")) {
				_ip = dict ["ip"];
			}

			if (dict.ContainsKey ("port")) {
				if (!int.TryParse (dict ["port"], out _port)) {
					throw new Exception ("port is not correct");
				}
				if (_port <= 0 || _port >= 65535) {
					throw new Exception ("port range is not correct");
				}
			}

			if (dict.ContainsKey ("during")) {
				if (!int.TryParse (dict ["during"], out _during)) {
					throw new Exception ("during is not correct");
				}
			}

			if (dict.ContainsKey ("id")) {
				_id = dict ["id"];
				_key = dict ["key"];
			}
		}

		public void ExecuteClose ()
		{
			_isClose = true;
			_listen.Close ();
			foreach (KeyValuePair<ProcessConnection, DateTime> item in _processDict) {
				item.Key.Dispose ();
			}
		}

		public void Execute ()
		{
			_listen = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPAddress ip = null;
			if (!string.IsNullOrEmpty (_ip)) {
				ip = IPAddress.Parse (_ip);
			}
			else {
				ip = IPAddress.Any;
			}
			IPEndPoint ipe = new IPEndPoint (ip, _port);
			_listen.Bind (ipe);
			_listen.Listen (5);
			listenList.Add (string.Format ("{0}:{1}", _ip, _port), this);
			_listen.BeginAccept (new AsyncCallback (AcceptCallBack), _listen);
			RaiseCallback (CallbackType.COMPLETE, "listen start");
			if (_during > 0) {
				_lastTime = DateTime.Now;
				Timer timer = new Timer (_during * 1000);
				timer.AutoReset = false;
				timer.Elapsed += new ElapsedEventHandler (timer_Elapsed);
				timer.Start ();
			}
		}

		void timer_Elapsed (object sender, ElapsedEventArgs e)
		{
			Timer timer = sender as Timer;
			if (_processDict.Count == 0) {
				int span = (int)(DateTime.Now - _lastTime).TotalSeconds;
				if (span >= _during) {
					_isClose = true;
					_listen.Close ();
					listenList.Remove (string.Format ("{0}:{1}", _ip, _port));
				}
				else {
					timer.Interval = span * 1000;
					timer.Start ();
				}
			}
			else {
				_lastTime = DateTime.Now;
				timer.Interval = _during * 1000;
				timer.Start ();
			}
		}

		private void AcceptCallBack (IAsyncResult ar)
		{
			Socket listen = (Socket)ar.AsyncState;
			bool success;
			Socket socket = null;
			string message = null;
			try {
				socket = listen.EndAccept (ar);
				success = true;
			}
			catch (Exception e) {
				if (_isClose) {
					RaiseCallback (CallbackType.OUTPUT, "listen stop");
					return;
				}
				else {
					message = e.Message;
					success = false;
				}
			}

			listen.BeginAccept (new AsyncCallback (AcceptCallBack), listen);

			if (success) {
				try {
					ProcessConnection process = null;
					if (!string.IsNullOrEmpty (_id)) {
						process = new ProcessConnection (_id, _key, socket);
					}
					else {
						process = new ProcessConnection (socket);
					}
					process.OnSocketException += new SocketExceptionEventHandler (process_OnSocketException);
					process.OnProcessExit += new ProcessExitEventHandler (process_OnProcessExit);
					//process.BindSocket(socket);
					_processDict [process] = DateTime.Now;
					process.SendAuthData ();
				}
				catch (Exception e) {
					message = e.Message;
					success = false;
				}
			}
			if (success) {
				IPEndPoint ipEndoint = socket.RemoteEndPoint as IPEndPoint;
				if (ipEndoint != null) {
					RaiseCallback (CallbackType.OUTPUT, string.Format ("success receive socket from {0}:{1}", ipEndoint.Address, ipEndoint.Port));
				}
			}
			else {
				RaiseCallback (CallbackType.OUTPUT, string.Format ("receive socket error, {0}", message));
			}
		}

		void process_OnSocketException (object sender, SocketExceptionEventArgs args)
		{
			ProcessConnection process = sender as ProcessConnection;
			string endpoint = process.SocketEndPoint;
			CloseProcess (process);
			RaiseCallback (CallbackType.OUTPUT, string.Format ("receive socket error {0} \r\nmessage:{1}\r\nexception:{2}", endpoint, args.Message, args.Exception));
		}

		void process_OnProcessExit (object sender, EventArgs args)
		{
			ProcessConnection process = sender as ProcessConnection;
			string endpoint = process.SocketEndPoint;
			CloseProcess (process);
			RaiseCallback (CallbackType.OUTPUT, string.Format ("receive socket close {0}", endpoint));
		}

		private void CloseProcess (ProcessConnection process)
		{
			process.OnSocketException -= new SocketExceptionEventHandler (process_OnSocketException);
			process.OnProcessExit -= new ProcessExitEventHandler (process_OnProcessExit);
			process.Dispose ();
			_processDict.Remove (process);
			_lastTime = DateTime.Now;
		}
	}
}
