using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace Light.Cmd
{
	public delegate void InjectionHandler (WorkCommand command, CallbackHandler callback);

	public delegate void CallbackHandler (WorkCommand command, CallbackType type, string message);

	public static class CommandProcessor
	{
		public static readonly string SHELL = "shell";

		public static readonly string CONFIG = "config";

		public static readonly string CONNECT = "connect";

		public static readonly string LISTEN = "listen";

		public static readonly string LISTEN_STOP = "listenstop";

		public static readonly string FTP_DOWNLOAD = "ftpdownload";

		public static readonly string FTP_UPLOAD = "ftpupload";

		public static readonly string EXIT = "exit";

		public static readonly string RESET = "reset";

		public static readonly string CLOSE = "close";

		static Dictionary<string, InjectionHandler> handlers = new Dictionary<string, InjectionHandler> ();

		static CommandProcessor ()
		{
			CommandProcessor.RegisterHandler (CommandProcessor.CLOSE, CloseCommand);
			CommandProcessor.RegisterHandler (CommandProcessor.SHELL, ProcessExecutor.ProcessCommand);
			CommandProcessor.RegisterHandler (CommandProcessor.CONFIG, ConfigExecutor.ConfigCommand);
			CommandProcessor.RegisterHandler (CommandProcessor.CONNECT, ConnectExecutor.ConnectionCommand);
			CommandProcessor.RegisterHandler (CommandProcessor.LISTEN, ListenExecutor.ListenCommand);
			CommandProcessor.RegisterHandler (CommandProcessor.LISTEN_STOP, ListenStopExecutor.ListenStopCommand);
			CommandProcessor.RegisterHandler (CommandProcessor.FTP_DOWNLOAD, FtpDownloadExecutor.FtpDownloadCommand);
			CommandProcessor.RegisterHandler (CommandProcessor.FTP_UPLOAD, FtpUploadExecutor.FtpUploadCommand);
		}

		public static void RegisterHandler (string name, InjectionHandler handler)
		{
			handlers [name.ToLower ().Trim ()] = handler;
		}

		public static void ProcessCommand (WorkCommand command, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			if (handlers.ContainsKey (command.CmdFlag.ToLower ().Trim ())) {
				InjectionHandler handler = handlers [command.CmdFlag];
				if (handler != null) {
					if (command.Timespan < 0) {
						try {
							handler (command, callback);
						}
						catch (Exception e) {
							if (callback != null) {
								callback (command, CallbackType.ERROR, e.Message);
							}
							else {
								throw e;
							}
						}
					}
					else {
						WorkTask task = new WorkTask (command, handler, callback);
						WorkTaskManager.AddWorkTask (task);
					}
				}
			}
		}

		static void CloseCommand (WorkCommand command, CallbackHandler callback)
		{
			Environment.Exit (0);
		}
	}
}
