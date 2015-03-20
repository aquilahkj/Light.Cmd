using System;
using System.Collections.Generic;
using System.Text;

namespace Light.Cmd
{
	class SocketExceptionEventArgs
	{
		Exception _exception = null;

		string _message = null;

		public SocketExceptionEventArgs (Exception exception, string message)
		{
			_exception = exception;
			_message = message;
		}

		public Exception Exception {
			get {
				return _exception;
			}
		}

		public string Message {
			get {
				return _message;
			}
		}
	}
}
