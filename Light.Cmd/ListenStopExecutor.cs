using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Light.Cmd
{
	class ListenStopExecutor : BaseExecutor
	{
		static Dictionary<string, ListenExecutor> listenList = new Dictionary<string, ListenExecutor> ();

		public static void ListenStopCommand (WorkCommand command, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			ListenStopExecutor executor = new ListenStopExecutor (command, callback);
			executor.Execute ();
		}

		string _ip = null;

		int _port = ListenExecutor.DEFAULT_PORT;

		ListenStopExecutor (WorkCommand command, CallbackHandler callback)
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
			}
		}

		public void Execute ()
		{
			ListenExecutor exceutor = ListenExecutor.GetListenExecutor (string.Format ("{0}:{1}", _ip, _port));
			if (exceutor != null) {
				exceutor.ExecuteClose ();
				RaiseCallback (CallbackType.COMPLETE, "listen stop complete");
			}
			else {
				RaiseCallback (CallbackType.ERROR, "listen stop failed");
			}
		}

	}
}
