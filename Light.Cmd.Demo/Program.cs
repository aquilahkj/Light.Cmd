using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Light.Cmd.Demo
{
	class Program
	{
		static void Main (string[] args)
		{
			while (true) {
				string cmd = Console.ReadLine ();

                
				if (cmd == string.Empty) {
					//cmd = "$listen";
					//cmd = @"$ftpdownload local=D:\EDT\\  host=10.167.7.139 port=21 remote=Log4Net.rar userid=website password=qwerty";

					//cmd = @"$ftpdownload local=D:\\EDT\\ ftp=10.167.34.2/test\ -\ 测试.aspx userid=huangkunjie password=V1dVPTdxeXHa";

					//cmd = @"$ftpupload local=D:\\EDT\\EMIS.CUReport.rar host=10.167.34.2 remote=/test1/ userid=huangkunjie password=V1dVPTdxeXHa mkdir=true";
					cmd = @"$ftpupload local=D:\\EDT\\EMIS.CUReport.rar host=10.167.7.139 remote=/test1/ userid=website password=qwerty mkdir=true iscontinue=true";


					//cmd = @"$ftpdownload local=D:\\EDT\\ ftp=10.167.7.139/EMIS.CUReport\ -\ 副本.rar userid=website password=qwerty encoder=utf-8";
				}
				if (cmd == "exit") {
					break;
				}
				try {
					WorkCommand command = CommandParser.ParseCommnd (cmd);
					if (command != null) {
						CommandProcessor.ProcessCommand (command, Callback);
					}
				}
				catch (Exception e) {
					Console.WriteLine (e);
				}
			}
		}


		static void Callback (WorkCommand command, CallbackType type, string message)
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("{0}\t{1}\t{2}:", command.CmdFlag, command.CommandCode, type.ToString ());
			sb.AppendLine ();
			sb.AppendLine (message);
			Console.Write (sb.ToString ());
		}

		static void test ()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo (@"cmd.exe");
			//startInfo.Arguments = "-al";
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			//startInfo.StandardErrorEncoding = Encoding.UTF8;
			//startInfo.StandardOutputEncoding = Encoding.UTF8;

			Process process = Process.Start (startInfo);

			//			Process process = new Process();
			//			process.StartInfo.UseShellExecute = false;
			//			process.StartInfo.RedirectStandardInput = true;
			//			process.StartInfo.RedirectStandardOutput = true;
			//			process.StartInfo.RedirectStandardError = true;
			//			process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
			//			process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

			process.OutputDataReceived += HandleOutputDataReceived;
			process.ErrorDataReceived += HandleErrorDataReceived;

			StreamWriter writer = process.StandardInput;

			process.BeginOutputReadLine ();
			process.BeginErrorReadLine ();

			String inputText;

			do {
				Console.WriteLine ("Enter a text line (or press the Enter key to stop):");

				inputText = Console.ReadLine ();
				if (!String.IsNullOrEmpty (inputText)) {
					writer.WriteLine (inputText);
				}
			}
			while (!String.IsNullOrEmpty (inputText));
			process.WaitForExit ();
		}

		static void HandleOutputDataReceived (object sender, DataReceivedEventArgs e)
		{
			// Collect the sort command output.
			if (!String.IsNullOrEmpty (e.Data)) {
				// Add the text to the collected output.
				Console.WriteLine (e.Data);
			}
		}

		static void HandleErrorDataReceived (object sender, DataReceivedEventArgs e)
		{
			// Collect the sort command output.
			if (!String.IsNullOrEmpty (e.Data)) {
				// Add the text to the collected output.
				Console.WriteLine (e.Data);
			}
		}
	}
}
