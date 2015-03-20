using System;
using System.Collections.Generic;
using System.Text;

namespace Light.Cmd
{
	class ConfigExecutor : BaseExecutor
	{
		static int Timeout = 600;

		public static bool SetTimeout (int timeout)
		{
			if (timeout >= 0) {
				Timeout = timeout;
				return true;
			}
			else {
				return false;
			}
		}

		public static int GetTimeout ()
		{
			return Timeout;
		}

		static Encoding ReceivedEncoding = null;

		public static bool SetEncoding (string encode)
		{
			if (string.IsNullOrEmpty (encode) || encode.Equals ("null", StringComparison.OrdinalIgnoreCase)) {
				ReceivedEncoding = null;
				return true;
			}
			else if (encode.Equals ("default", StringComparison.OrdinalIgnoreCase)) {
				ReceivedEncoding = Encoding.Default;
				return true;
			}
			else {
				try {
					Encoding e = Encoding.GetEncoding (encode);
					ReceivedEncoding = e;
					return true;
				}
				catch {
					return false;
				}
			}
		}

		public static Encoding GetEncoding ()
		{
			return ReceivedEncoding;
		}

		public static void ConfigCommand (WorkCommand command, CallbackHandler callback)
		{
			ConfigExecutor executor = new ConfigExecutor (command, callback);
			executor.Execute ();
		}

		ConfigExecutor (WorkCommand command, CallbackHandler callback)
			: base (command, callback)
		{

		}

		public void Execute ()
		{
			Dictionary<string, string> dict = Utility.ParseCommandLine (_command.CommandCode);
			StringBuilder output = new StringBuilder ();
			StringBuilder error = new StringBuilder ();
			if (dict.ContainsKey ("timeout")) {
				int timeout;
				if (int.TryParse (dict ["timeout"], out timeout)) {
					if (ConfigExecutor.SetTimeout (timeout)) {
						output.AppendLine ("set timeout complete");
					}
					else {
						error.AppendLine ("set timeout failed");
					}
				}
				else {
					error.AppendLine ("timeout value error");
				}
			}

			if (dict.ContainsKey ("encoding")) {
				string encode = dict ["encoding"];

				if (ConfigExecutor.SetEncoding (encode)) {
					error.AppendLine ("set encoding complete");
				}
				else {
					error.AppendLine ("set encoding failed");
				}
			}
			if (error.Length > 0) {
				RaiseCallback (CallbackType.ERROR, error.ToString () + output.ToString ());
			}
			else {
				RaiseCallback (CallbackType.COMPLETE, output.ToString ());
			}
		}
	}
}
