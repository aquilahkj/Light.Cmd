using System;
using System.Collections.Generic;
using System.Text;

namespace Light.Cmd
{
    abstract class BaseExecutor
    {
        protected const string HOST_REGEX = @"^([\w-]+\.)+[\w-]+$";

        protected WorkCommand _command;

        protected CallbackHandler _callback;

        public BaseExecutor(WorkCommand command, CallbackHandler callback)
        {
            _command = command;
            _callback = callback;
        }

        protected void RaiseCallback(CallbackType type, string message)
        {
            if (_callback != null)
            {
                try
                {
                    _callback(_command, type, message);
                }
                catch
                {

                }
            }
        }
    }
}
