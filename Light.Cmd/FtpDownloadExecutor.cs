using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Light.Cmd
{
	class FtpDownloadExecutor : BaseExecutor
	{
		static readonly string FTP_REGEX = @"^ftp://([\w-]+\.)+[\w-]+(/.+)+$";


		public static void FtpDownloadCommand (WorkCommand command, CallbackHandler callback)
		{
			if (command == null) {
				throw new ArgumentNullException ("command");
			}
			FtpDownloadExecutor executor = new FtpDownloadExecutor (command, callback);
			executor.Execute ();
		}

		string _local = null;

		string _remote = null;

		string _userId = null;

		string _password = null;

		bool _useBinary = true;

		bool _usePassive = true;

		bool _isContinue = true;

		string _host = null;

		int _port = 0;

		FtpDownloadExecutor (WorkCommand command, CallbackHandler callback)
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
			if (_remote.EndsWith ("/")) {
				throw new Exception ("ftpuri format is not error");
			}

			if (dict.ContainsKey ("userid")) {
				_userId = dict ["userid"];
				_password = dict ["password"];
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
			string ftpUri = string.Format ("ftp://{0}{1}{2}", _host, _port > 0 ? ":" + _port : string.Empty, _remote);
			string ftpfileName = ftpUri.Substring (ftpUri.LastIndexOf ("/") + 1);
			FileInfo file = null;
			if (string.IsNullOrEmpty (_local)) {
				file = new FileInfo (ftpfileName);
			}
			else {
				file = new FileInfo (_local);
				if (string.IsNullOrEmpty (file.Name)) {
					file = new FileInfo (_local + ftpfileName);
				}
			}

			FtpWebRequest request = (FtpWebRequest)WebRequest.Create (ftpUri);
			//下载命令
			request.Method = WebRequestMethods.Ftp.DownloadFile;
			//使用ASCII模式
			request.UseBinary = _useBinary;
			//使用主动(port)模式
			request.UsePassive = _usePassive;
			//FTP登录名密码
			if (!string.IsNullOrEmpty (_userId)) {
				request.Credentials = new NetworkCredential (_userId, _password);
			}

			if (file.Exists && _isContinue) {
				request.ContentOffset = file.Length;
			}

			//request.BeginGetResponse(new AsyncCallback(RequestCallback), request);
			RaiseCallback (CallbackType.OUTPUT, string.Format ("ftp {0}download start,{1}", request.ContentOffset > 0 ? "continue " : "", request.RequestUri));

			using (FtpWebResponse response = (FtpWebResponse)request.GetResponse ()) {
				if (response.StatusCode == FtpStatusCode.ClosingData || response.StatusCode == FtpStatusCode.OpeningData || response.StatusCode == FtpStatusCode.DataAlreadyOpen || response.StatusCode == FtpStatusCode.FileActionOK) {
					if (!file.Directory.Exists) {
						file.Directory.Create ();
					}
					FileMode fileMode = FileMode.Create;
					if (file.Exists) {
						if (_isContinue) {
							fileMode = FileMode.Append;
						}
						else {
							file.Delete ();
						}
					}

					int len = 4096;
					byte[] buffer = new byte[len];
					long total = 0;
					using (Stream ftpstream = response.GetResponseStream ()) {
						using (Stream filestream = file.Open (fileMode, FileAccess.Write)) {
							while (true) {
								int count = ftpstream.Read (buffer, 0, len);
								total += count;
								if (count > 0) {
									filestream.Write (buffer, 0, count);
								}
								else {
									break;
								}
							}
						}
					}
					RaiseCallback (CallbackType.COMPLETE, string.Format ("ftp download success,{0},size:{1}", response.ResponseUri, total));
				}
				else {
					RaiseCallback (CallbackType.ERROR, "ftp response error,status:" + response.StatusCode);
				}
			}
		}

		/*
        void RequestCallback(IAsyncResult result)
        {
            FtpWebRequest request = result.AsyncState as FtpWebRequest;
            FtpWebResponse response = null;
            try
            {
                response = (FtpWebResponse)request.EndGetResponse(result);
            }
            catch (Exception e)
            {
                RaiseCallback(CallbackType.ERROR, "ftp response failed\r\n" + e.Message);
                return;
            }
            if (response.StatusCode == FtpStatusCode.ClosingData || response.StatusCode == FtpStatusCode.OpeningData || response.StatusCode == FtpStatusCode.DataAlreadyOpen || response.StatusCode == FtpStatusCode.FileActionOK)
            {
                int len = 4096;
                byte[] buffer = new byte[len];
                long total = 0;

                try
                {
                    if (!_file.Directory.Exists)
                    {
                        _file.Directory.Create();
                    }
                    FileMode fileMode = FileMode.Create;
                    if (_file.Exists)
                    {
                        if (_isContinue)
                        {
                            fileMode = FileMode.Append;
                        }
                        else
                        {
                            _file.Delete();
                        }
                    }

                    using (Stream ftpstream = response.GetResponseStream())
                    {
                        using (Stream filestream = _file.Open(fileMode, FileAccess.Write))
                        {
                            while (true)
                            {
                                int count = ftpstream.Read(buffer, 0, len);
                                total += count;
                                if (count > 0)
                                {
                                    filestream.Write(buffer, 0, count);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    RaiseCallback(CallbackType.ERROR, "ftp response error\r\n" + e.Message);
                    return;
                }
                RaiseCallback(CallbackType.COMPLETE, string.Format("ftp download success,{0},size:{1}", response.ResponseUri, total));
            }
            else
            {
                RaiseCallback(CallbackType.ERROR, "ftp response error,status:" + response.StatusCode);
            }
            response.Close();
        }
         */
	}
}
