using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Light.Cmd
{
	public static class CommandParser
	{
		static readonly string RegexTimespan = @"^\d+";

		static readonly string RegexDate = @"^(((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-)) (20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d)$";

		static readonly string RegexTime = @"^((20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d)$";

		public static WorkCommand ParseCommnd (string commandLine)
		{
			if (string.IsNullOrEmpty (commandLine)) {
				return null;
			}
			commandLine = commandLine.Trim ();
			if (commandLine.StartsWith ("#")) {
				return null;
			}
			int id = 0;
			string cmdFlag = null;
			int timespan = -1;
			string commandCode = null;
			Match idMatch = Regex.Match (commandLine, @"^\[\d+\]", RegexOptions.Compiled);
			if (idMatch.Success) {
				string strid = commandLine.Substring (1, idMatch.Length - 2);
				if (!int.TryParse (strid, out id)) {
					throw new Exception ("id parse error");
				}
				commandLine = commandLine.Substring (idMatch.Length);
			}
			if (commandLine.StartsWith ("$")) {
				int index = commandLine.IndexOf (" ");
				if (index > 0) {
					cmdFlag = commandLine.Substring (1, index - 1).Trim ();
					commandCode = commandLine.Substring (index + 1);
				}
				else {
					cmdFlag = commandLine.Substring (1).Trim ();
					commandCode = string.Empty;
				}

				int timeindex = cmdFlag.IndexOf ("@");
				if (timeindex >= 0) {
					string timestr = cmdFlag.Substring (timeindex + 1);
					cmdFlag = cmdFlag.Substring (0, timeindex);
					if (!string.IsNullOrEmpty (timestr)) {
						if (Regex.IsMatch (timestr, RegexTimespan, RegexOptions.Compiled)) {
							if (!int.TryParse (timestr, out timespan)) {
								throw new Exception ("command parse error,timespan value error");
							}
						}
						else if (Regex.IsMatch (timestr, RegexDate, RegexOptions.Compiled)) {
							DateTime dt;
							if (!DateTime.TryParse (timestr, out dt)) {
								throw new Exception ("command parse error,datetime format error");
							}
							timespan = Convert.ToInt32 ((dt - DateTime.Now).TotalSeconds);
							if (timespan < 0) {
								timespan = 0;
							}
						}
						else if (Regex.IsMatch (timestr, RegexTime, RegexOptions.Compiled)) {
							DateTime dt;
							if (!DateTime.TryParse (DateTime.Now.Date.ToString ("yyyy-MM-dd ") + timestr, out dt)) {
								throw new Exception ("command parse error,time value error");
							}
							timespan = Convert.ToInt32 ((dt - DateTime.Now).TotalSeconds);
							if (timespan < 0) {
								timespan = 0;
							}
						}
						else {
							throw new Exception ("command parse error,time format error");
						}
					}
				}
			}
			else {
				commandCode = commandLine;
			}

			WorkCommand workCommand = new WorkCommand (id, cmdFlag, timespan, commandCode);
			return workCommand;
		}
	}
}
