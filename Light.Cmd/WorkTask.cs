using System;
using System.Collections.Generic;
using System.Text;

namespace Light.Cmd
{
	class WorkTask
	{
		private WorkCommand _command;

		internal WorkCommand Command {
			get {
				return _command;
			}
		}

		private InjectionHandler _handler;

		public InjectionHandler Handler {
			get {
				return _handler;
			}
		}

		private CallbackHandler _callback;

		public CallbackHandler Callback {
			get {
				return _callback;
			}
		}

		public WorkTask (WorkCommand command, InjectionHandler handler, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			if (handler == null) {
				throw new ArgumentNullException ("handler");
			}
			this._command = command;
			this._handler = handler;
			this._callback = callback;
		}
	}
}
