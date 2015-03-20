using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Light.Cmd
{
	class FtpUploadExecutor : BaseExecutor
	{
		//static readonly string FTP_REGEX = @"^ftp://([\w-]+\.)+[\w-]+(/.*)+$";

		public static void FtpUploadCommand (WorkCommand command, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			FtpUploadExecutor executor = new FtpUploadExecutor (command, callback);
			executor.Execute ();
		}

		string _local = null;

		string _remote = null;

		string _userId = null;

		string _password = null;

		bool _useBinary = true;

		bool _usePassive = true;

		bool _isContinue = false;

		bool _mkdir = false;

		string _host = null;

		int _port = 0;

		FtpUploadExecutor (WorkCommand command, CallbackHandler callback)
			: base (command, callback)
		{
			Dictionary<string, string> dict = Utility.ParseCommandLine (_command.CommandCode);

			if (dict.ContainsKey ("local")) {
				_local = dict ["local"];
			}

			if (dict.ContainsKey ("host")) {
				_host = dict ["host"];
			}
			if (string.IsNullOrEmpty (_host)) {
				throw new Exception ("host is not exists");
			}
			if (!Regex.IsMatch (_host, HOST_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase)) {
				throw new Exception ("host is not correct");
			}

			if (dict.ContainsKey ("port")) {
				if (!int.TryParse (dict ["port"], out _port)) {
					throw new Exception ("port is not correct");
				}
				if (_port <= 0 || _port >= 65535) {
					throw new Exception ("port range is not correct");
				}
			}

			if (dict.ContainsKey ("remote")) {
				_remote = dict ["remote"];
			}
			if (string.IsNullOrEmpty (_remote)) {
				throw new Exception ("remote is not correct");
			}
			if (!_remote.StartsWith ("/")) {
				_remote = "/" + _remote;
			}


			if (dict.ContainsKey ("userid")) {
				_userId = dict ["userid"];
				_password = dict ["password"];
			}

			if (dict.ContainsKey ("mkdir")) {
				if (!bool.TryParse (dict ["mkdir"], out _mkdir)) {
					throw new Exception ("mkdir is not correct");
				}
			}

			if (dict.ContainsKey ("usebinary")) {
				if (!bool.TryParse (dict ["usebinary"], out _useBinary)) {
					throw new Exception ("usebinary is not correct");
				}
			}

			if (dict.ContainsKey ("usepassive")) {
				if (!bool.TryParse (dict ["usepassive"], out _usePassive)) {
					throw new Exception ("usepassive is not correct");
				}
			}

			if (dict.ContainsKey ("iscontinue")) {
				if (!bool.TryParse (dict ["iscontinue"], out _isContinue)) {
					throw new Exception ("iscontinue is not correct");
				}
			}
		}

		public void Execute ()
		{
			FileInfo file = new FileInfo (_local);
			if (!file.Exists) {
				throw new Exception ("local file is not exists");
			}

			string ftpUri = string.Format ("ftp://{0}{1}{2}", _host, _port > 0 ? ":" + _port : string.Empty, _remote);
			if (ftpUri.EndsWith ("/")) {
				ftpUri = ftpUri + file.Name;
			}

			if (_mkdir) {
				string fullDir = _remote.Substring (0, _remote.LastIndexOf ("/"));
				string[] dirs = Utility.Split (fullDir, '/');
				string curDir = "/";
				foreach (string dir in dirs) {
					curDir += dir + "/";
					string dirUri = string.Format ("ftp://{0}{1}{2}", _host, _port > 0 ? ":" + _port : string.Empty, curDir);
					FtpMakeDir (dirUri);
				}
			}

			FtpWebRequest request = (FtpWebRequest)WebRequest.Create (ftpUri);
			request.Method = WebRequestMethods.Ftp.UploadFile;
			//使用ASCII模式
			request.UseBinary = _useBinary;
			//使用主动(port)模式
			request.UsePassive = _usePassive;
			if (!string.IsNullOrEmpty (_userId)) {
				request.Credentials = new NetworkCredential (_userId, _password);
			}
			long offset = 0;
			if (_isContinue) {
				long length = GetFileSize (ftpUri);
				if (length > 0 && file.Length > length) {
					offset = length;
					request.Method = WebRequestMethods.Ftp.AppendFile;
				}
			}
			RaiseCallback (CallbackType.OUTPUT, string.Format ("ftp {0}upload start,{1}", offset > 0 ? "continue " : "", request.RequestUri));

			request.ContentLength = file.Length;
			int len = 4096;
			byte[] buffer = new byte[len];
			long total = 0;
			using (Stream filestream = file.Open (FileMode.Open, FileAccess.Read)) {
				if (offset > 0) {
					filestream.Position = offset;
				}
				using (Stream ftpstream = request.GetRequestStream ()) {
					while (true) {
						int count = filestream.Read (buffer, 0, len);
						total += count;
						if (count > 0) {
							ftpstream.Write (buffer, 0, count);
						}
						else {
							break;
						}
					}
				}
			}
			request.Abort ();
			RaiseCallback (CallbackType.COMPLETE, string.Format ("ftp upload success,{0},size:{1}", request.RequestUri, total));
		}


		bool FtpMakeDir (string dirUri)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create (dirUri);
			if (!string.IsNullOrEmpty (_userId)) {
				request.Credentials = new NetworkCredential (_userId, _password);
			}
			request.Method = WebRequestMethods.Ftp.MakeDirectory;
			try {
				FtpWebResponse response = (FtpWebResponse)request.GetResponse ();
				response.Close ();
				return true;
			}
			catch {
				return false;
			}
			finally {
				request.Abort ();
			}
            
		}

		long GetFileSize (string filePath)
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create (filePath);
			if (!string.IsNullOrEmpty (_userId)) {
				request.Credentials = new NetworkCredential (_userId, _password);
			}
			request.Method = WebRequestMethods.Ftp.GetFileSize;
			try {
				using (FtpWebResponse response = (FtpWebResponse)request.GetResponse ()) {
					return response.ContentLength;
				}
			}
			catch {
				return 0L;
			}
			finally {
				request.Abort ();
			}
		}
	}
}
