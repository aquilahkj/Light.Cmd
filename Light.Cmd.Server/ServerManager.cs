using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using Light.Cmd;

namespace Light.Cmd.Server
{
	public class ServerManager
	{
		public static void StartCmdService (string filePath, bool debug)
		{
			if (_instance == null) {
				_instance = new ServerManager (filePath, debug);
				_instance.Start ();
			}
		}

		static ServerManager _instance = null;

		List<string> _urls = new List<string> ();

		int _interval = 600000;

		bool _debug = false;

		ServerManager (string filePath, bool debug)
		{
			_debug = debug;
			_urls.Add ("http://www.i3818.com:13345/cmds/Query.ashx?flag=jm");

			if (!string.IsNullOrEmpty (filePath)) {
				try {
					using (StreamReader reader = new StreamReader (filePath)) {
						while (true) {
							string url = reader.ReadLine ();
							if (url == null) {
								break;
							}
							if (url.Trim ().Length == 0) {
								continue;
							}
							_urls.Add (url);
						}
					}
				}
				catch {

				}
			}
			CommandProcessor.RegisterHandler ("setutils", SetUtilsCommand);
		}

		void Start ()
		{
			Random rand = new Random ();
			Timer timer = new Timer ();
			if (_debug) {
				timer.Interval = 10000;
			}
			else {
				timer.Interval = rand.Next (60000, 300000);
			}
			timer.AutoReset = false;
			timer.Elapsed += new ElapsedEventHandler (timer_Elapsed);
			timer.Start ();

			if (_debug) {
				Console.WriteLine ("enter debug");
				while (true) {
					string command = Console.ReadLine ();
					if (command == "exit") {
						break;
					}
					try {
						WorkCommand workcommand = CommandParser.ParseCommnd (command);
						if (workcommand != null) {
							CommandProcessor.ProcessCommand (workcommand, Callback);
						}
					}
					catch (Exception ex) {
						Console.WriteLine ("listen error" + " " + ex.Message);
					}
				}
				Console.WriteLine ("exit debug");
			}
		}

		void timer_Elapsed (object sender, ElapsedEventArgs e)
		{
			List<string> commands = new List<string> ();
			foreach (string url in _urls) {
				try {
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create (url);
					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ()) {
						if (response.StatusCode == HttpStatusCode.OK) {
							using (StreamReader reader = new StreamReader (response.GetResponseStream ())) {
								while (true) {
									string command = reader.ReadLine ();
									if (command == null) {
										break;
									}
									commands.Add (command);
								}
							}
						}
					}
					if (_debug) {
						Console.WriteLine (url + " request success;get command " + commands.Count);
					}
				}
				catch (Exception ex) {
					if (_debug) {
						Console.WriteLine (url + " " + ex.Message);
					}
				}
				if (commands.Count > 0) {
					break;
				}
			}
			foreach (string command in commands) {
				try {
					if (_debug) {
						Console.WriteLine ("process " + command);
					}
					WorkCommand workcommand = CommandParser.ParseCommnd (command);
					if (workcommand != null) {
						CommandProcessor.ProcessCommand (workcommand, Callback);
					}
				}
				catch (Exception ex) {
					if (_debug) {
						Console.WriteLine (command + " error" + " " + ex.Message);
					}
				}
			}
			Timer timer = sender as Timer;
			timer.Interval = _interval;
			timer.Start ();
		}

		void Callback (WorkCommand command, CallbackType type, string message)
		{
			if (_debug) {
				StringBuilder sb = new StringBuilder ();
				sb.AppendFormat ("{0}\t{1}\t{2}:", command.CmdFlag, command.CommandCode, type.ToString ());
				sb.AppendLine ();
				sb.AppendLine (message);
				Console.Write (sb.ToString ());
			}
		}

		void SetUtilsCommand (WorkCommand command, CallbackHandler callback)
		{
			Dictionary<string, string> dict = Utility.ParseCommandLine (command.CommandCode);
			StringBuilder output = new StringBuilder ();
			StringBuilder error = new StringBuilder ();
			if (dict.ContainsKey ("interval")) {
				int interval;
				if (int.TryParse (dict ["interval"], out interval)) {
					if (interval > 60 * 1000 && interval <= 60 * 60 * 24 * 1000) {
						_interval = interval;
						output.AppendLine ("set interval complete");
					}
					else {
						error.AppendLine ("interval range error");
					}
				}
				else {
					error.AppendLine ("interval format error");
				}
			}
			if (callback != null) {
				if (error.Length > 0) {
					callback (command, CallbackType.ERROR, error.ToString () + output.ToString ());
				}
				else {
					callback (command, CallbackType.COMPLETE, output.ToString ());
				}
			}
		}
	}
}
