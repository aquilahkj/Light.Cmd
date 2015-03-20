using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Light.Cmd
{
	class ProcessExecutor : BaseExecutor
	{
		public static void ProcessCommand (WorkCommand command, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			ProcessExecutor executor = new ProcessExecutor (command, callback);
			executor.Execute ();
		}

		ManualResetEvent _outputEvent = new ManualResetEvent (false);

		ManualResetEvent _errorEvent = new ManualResetEvent (false);

		//readonly string _exitTag = Guid.NewGuid().ToString();

		ProcessExecutor (WorkCommand command, CallbackHandler callback)
			: base (command, callback)
		{

		}

		StringBuilder outputString = new StringBuilder ();

		StringBuilder errorString = new StringBuilder ();

		public void Execute ()
		{
			string cmdName = Utility.GetCmdName ();

			string cmd = _command.CommandCode;
			if (string.IsNullOrEmpty (cmd)) {
				return;
			}

			ProcessStartInfo startInfo = new ProcessStartInfo (cmdName);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			Encoding encoding = ConfigExecutor.GetEncoding ();
			if (encoding != null) {
				startInfo.StandardErrorEncoding = encoding;
				startInfo.StandardOutputEncoding = encoding;
			}

			try {
				Process process = new Process ();
				process.StartInfo = startInfo;
				process.EnableRaisingEvents = true;
				process.Start ();
 
				process.Exited += new EventHandler (process_Exited);
				process.OutputDataReceived += new DataReceivedEventHandler (process_OutputDataReceived);
				process.ErrorDataReceived += new DataReceivedEventHandler (process_ErrorDataReceived);

				StreamWriter writer = process.StandardInput;

				process.BeginOutputReadLine ();
				process.BeginErrorReadLine ();

				writer.WriteLine (cmd);
				//writer.WriteLine("echo " + _exitTag);
				writer.WriteLine ("exit");

				process.WaitForExit (1000 * ConfigExecutor.GetTimeout ());
				if (!process.HasExited) {
					_outputEvent.Set ();
					try {
						process.Kill ();
					}
					finally {
						process.Dispose ();
						RaiseCallback (CallbackType.ERROR, "process be killed");
					}
				}
			}
			catch (Exception e) {
				RaiseCallback (CallbackType.ERROR, e.Message);
			}
		}

		void process_Exited (object sender, EventArgs e)
		{
			_outputEvent.WaitOne (1000 * ConfigExecutor.GetTimeout ());
			_errorEvent.WaitOne (5000);
			string error = errorString.ToString ().Trim ();
			if (error.Length > 0) {
				RaiseCallback (CallbackType.ERROR, error);
			}
			else {
				RaiseCallback (CallbackType.COMPLETE, "process complete at " + DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss"));
			}
			string output = outputString.ToString ().Trim ();
			if (output.Length == 0) {
				RaiseCallback (CallbackType.OUTPUT, string.Empty);
			}
			else {
				RaiseCallback (CallbackType.OUTPUT, output);
			}
		}

		void process_OutputDataReceived (object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null) {
				_outputEvent.Set ();
			}
			else {
				outputString.AppendLine (e.Data);
			}
		}

		void process_ErrorDataReceived (object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null) {
				_errorEvent.Set ();
			}
			else {
				errorString.AppendLine (e.Data);
			}
		}
	}
}
