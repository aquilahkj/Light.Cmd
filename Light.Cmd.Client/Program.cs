using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Light.Cmd.Client
{
	class Program
	{
		static byte[] _buffer = new byte[4096];

		static int _dataLength = -1;

		static ReadWriteStream _stream = new ReadWriteStream ();

		static bool _state = false;

		static void Main (string[] args)
		{


			bool close = false;
			while (!close) {
				Console.WriteLine ("choose method");
				Console.WriteLine ("1.listen");
				Console.WriteLine ("2.connect");
				Console.WriteLine ("0.close");
				Console.Write ("please enter the number:");
				string read = Console.ReadLine ();
				switch (read.Trim ()) {
					case "1":
						ListenCommand ();
						break;
					case "2":
						ConnectCommand ();
						break;
					case "0":
						close = true;
						break;
					default:
						Console.Write ("unknow number");
						break;
				}
			}
			Console.Write ("bye bye");


		}

		private static void ConnectCommand ()
		{
			Socket socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			try {
				int port = 3818;
				string host = null;
				string endpoint = null;
				if (ConfigurationManager.AppSettings ["connect"] != null) {
					endpoint = ConfigurationManager.AppSettings ["connect"];
				}
				if (string.IsNullOrEmpty (endpoint)) {
					Console.Write ("please enter the endpoint:");
					endpoint = Console.ReadLine ();
				}
				if (!string.IsNullOrEmpty (endpoint)) {
					int index = endpoint.IndexOf (':');
					if (index > 0) {
						host = endpoint.Substring (0, index);
						port = Convert.ToInt32 (endpoint.Substring (index + 1));
					}
					else {
						host = endpoint;
					}
				}
				else {
					throw new Exception ("endpoint is not exists");
				}

				socket.Connect (host, port);
				Console.WriteLine ("connect sucess");
				socket.BeginReceive (_buffer, 0, _buffer.Length, SocketFlags.None,
					new AsyncCallback (ReceiveSocketInputCallback), socket);
			}
			catch (Exception e) {
				Console.WriteLine (e.Message);
				return;
			}
			_state = true;
			while (true) {
				try {
					string data = Console.ReadLine ();
					if (data == "quit") {
						break;
					}
					byte[] databuf = Encoding.UTF8.GetBytes (data);
					byte[] totalbuf = new byte[databuf.Length + 4];
					byte[] lenbuf = BitConverter.GetBytes (databuf.Length);
					lenbuf.CopyTo (totalbuf, 0);
					databuf.CopyTo (totalbuf, 4);
					socket.Send (totalbuf);
				}
				catch (SocketException se) {
					Console.WriteLine (se.Message);
					break;
				}
				catch (Exception e) {
					Console.WriteLine (e.Message);
				}
			}
			_state = false;
			socket.Close ();
			Console.WriteLine ("connect finish");
			Console.ReadLine ();
		}

		private static void ListenCommand ()
		{
			Socket socket = null;
			Socket listen = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			try {
				int port = 3818;
				string ip = null;
				string endpoint = null;
				if (ConfigurationManager.AppSettings ["listen"] != null) {
					endpoint = ConfigurationManager.AppSettings ["listen"];
				}
				if (string.IsNullOrEmpty (endpoint)) {
					Console.Write ("please enter the endpoint:");
					endpoint = Console.ReadLine ();
				}
				if (!string.IsNullOrEmpty (endpoint)) {
					int index = endpoint.IndexOf (':');
					if (index > 0) {
						ip = endpoint.Substring (0, index);
						port = Convert.ToInt32 (endpoint.Substring (index + 1));
					}
					else {
						ip = endpoint;
					}
				}

				IPAddress ipaddress = null;
				if (!string.IsNullOrEmpty (ip)) {
					ipaddress = IPAddress.Parse (ip);
				}
				else {
					ipaddress = IPAddress.Any;
				}

				IPEndPoint ipe = new IPEndPoint (ipaddress, port);


				listen.Bind (ipe);
				listen.Listen (5);
				Console.WriteLine ("listen start");
				socket = listen.Accept ();
				Console.WriteLine ("accept socket " + socket.LocalEndPoint);
				socket.BeginReceive (_buffer, 0, _buffer.Length, SocketFlags.None,
					new AsyncCallback (ReceiveSocketInputCallback), socket);

			}
			catch (Exception e) {
				Console.WriteLine (e.Message);
				if (socket != null) {
					socket.Close ();
				}
				listen.Close ();
				return;
			}
			_state = true;
			while (true) {
				try {
					string data = Console.ReadLine ();
					if (data == "quit") {
						break;
					}
					byte[] databuf = Encoding.UTF8.GetBytes (data);
					byte[] totalbuf = new byte[databuf.Length + 4];
					byte[] lenbuf = BitConverter.GetBytes (databuf.Length);
					lenbuf.CopyTo (totalbuf, 0);
					databuf.CopyTo (totalbuf, 4);
					socket.Send (totalbuf);
				}
				catch (SocketException se) {
					Console.WriteLine (se.Message);
					break;
				}
				catch (Exception e) {
					Console.WriteLine (e);
				}
			}
			_state = false;
			socket.Close ();
			listen.Close ();
			Console.WriteLine ("listen finish");
			Console.ReadLine ();
		}



		static private void ReceiveSocketInputCallback (IAsyncResult ar)
		{
			Socket socket = (Socket)ar.AsyncState;//获得调用时传递的StateObject对象
			try {
				int len = socket.EndReceive (ar);
				if (len > 0) {//判断是否接受到了新信息
					_stream.Write (_buffer, 0, len);
					byte[] lenbuf = new byte[4];
					while (true) {
						if (_dataLength == -1) {
							if (_stream.Length > 4) {
								_stream.Read (lenbuf, 0, 4);
								_dataLength = BitConverter.ToInt32 (lenbuf, 0);
							}
						}

						if (_dataLength > 0 && _stream.Length >= _dataLength) {
							byte[] strbuf = new byte[_dataLength];
							_stream.Read (strbuf, 0, _dataLength);
							string data = Encoding.UTF8.GetString (strbuf);
							Console.WriteLine (data);
							_dataLength = -1;
						}
						else if (_dataLength == 0) {
							Console.WriteLine ();
							_dataLength = -1;
						}
						else {
							break;
						}
					}
					socket.BeginReceive (_buffer, 0, _buffer.Length, SocketFlags.None,
						new AsyncCallback (ReceiveSocketInputCallback), socket);
				}
				else {
					socket.Close ();
					Console.WriteLine ("socket.Close");
				}
			}
			catch (Exception e) {
				if (_state) {
					Console.WriteLine ("socket.error");
					Console.WriteLine (e);
				}
			}
		}
	}
}
