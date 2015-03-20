using System;
using System.Collections.Generic;
using System.Text;

namespace Light.Cmd
{
	public class WorkCommand
	{
		public WorkCommand (int id, string cmdFlag, int timespan, string commandCode)
		{
			if (id > 0) {
				_id = id;
			}
			if (string.IsNullOrEmpty (cmdFlag)) {
				_cmdFlag = CommandProcessor.SHELL;
			}
			else {
				_cmdFlag = cmdFlag;
			}

			if (timespan < -1) {
				_timespan = -1;
			}
			else {
				_timespan = timespan;
			}

			if (commandCode == null) {
				_commandCode = string.Empty;
			}
			else {
				_commandCode = commandCode.Trim ();
			}
		}

		int _id;

		public int Id {
			get {
				return _id;
			}
		}

		string _cmdFlag;

		public string CmdFlag {
			get {
				return _cmdFlag;
			}
		}

		int _timespan;

		public int Timespan {
			get {
				return _timespan;
			}
		}

		string _commandCode;

		public string CommandCode {
			get {
				return _commandCode;
			}
		}
	}
}
