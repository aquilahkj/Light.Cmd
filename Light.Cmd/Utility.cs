using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Light.Cmd
{
	public static class Utility
	{
		public static Dictionary<string, string> ParseCommandLine (string commandLine)
		{
			return ParseCommandLine (commandLine, true);
		}

		public static Dictionary<string, string> ParseCommandLine (string commandLine, bool keyLower)
		{
			Dictionary<string, string> dict = new Dictionary<string, string> ();
			string[] array = Split (commandLine);
			foreach (string item in array) {
				int index = item.IndexOf ('=');
				if (index > 0) {
					string key = item.Substring (0, index).Trim ();
					if (keyLower) {
						key = key.ToLower ();
					}
					dict [key] = item.Substring (index + 1).Trim ();
				}
				else {
					string key = item.Trim ();
					if (keyLower) {
						key = key.ToLower ();
					}
					dict [key] = null;
				}
			}
			return dict;
		}

		public static string[] Split (string content, params char[] sign)
		{
			List<string> list = new List<string> ();
			if (!string.IsNullOrEmpty (content)) {
				Dictionary<char, char> signs = new Dictionary<char, char> ();
				if (sign == null || sign.Length == 0) {
					signs.Add (' ', ' ');
				}
				else {
					foreach (char c in sign) {
						signs [c] = c;
					}
				}
				int len = content.Length;
				char[] array = content.ToCharArray ();
				StringBuilder sb = new StringBuilder ();
				for (int i = 0; i < len; i++) {
					if (array [i] == '\\') {
						if (i + 1 < len) {
							char ch = array [i + 1];
							if (ch == '\\' && signs.ContainsKey ('\\')) {
								i++;
								if (sb.Length > 0) {
									list.Add (sb.ToString ());
									sb = new StringBuilder ();
								}
								continue;
							}
							if (signs.ContainsKey (ch) || ch == '\\') {
								i++;
							}
							sb.Append (array [i]);
						}
						else if (i + 1 == len) {
							if (!signs.ContainsKey ('\\')) {
								sb.Append (array [i]);
							}
						}
					}
					else if (signs.ContainsKey (array [i])) {
						if (sb.Length > 0) {
							list.Add (sb.ToString ());
							sb = new StringBuilder ();
						}
					}
					else {
						sb.Append (array [i]);
					}
				}
				if (sb.Length > 0) {
					list.Add (sb.ToString ());
				}
			}
			return list.ToArray ();
		}

		public static string GetCmdName ()
		{
			string cmdName = null;

			switch (Environment.OSVersion.Platform) {
				case PlatformID.MacOSX:
				case PlatformID.Unix:
					cmdName = "sh";
					break;
				default:
					cmdName = "cmd";
					break;

			}
			return cmdName;
		}

		static char[] hexChars = "0123456789ABCDEF".ToCharArray ();

		public static string UrlPathEncode (string value, Encoding encoding)
		{
			if (String.IsNullOrEmpty (value)) {
				return value;
			}
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}

			MemoryStream result = new MemoryStream ();
			foreach (char c in value) {
				if (c < 33 || c > 126) {
					byte[] bIn = encoding.GetBytes (c.ToString ());
					for (int i = 0; i < bIn.Length; i++) {
						result.WriteByte ((byte)'%');
						int idx = ((int)bIn [i]) >> 4;
						result.WriteByte ((byte)hexChars [idx]);
						idx = ((int)bIn [i]) & 0x0F;
						result.WriteByte ((byte)hexChars [idx]);
					}
				}
				else if (c == ' ') {
					result.WriteByte ((byte)'%');
					result.WriteByte ((byte)'2');
					result.WriteByte ((byte)'0');
				}
				else {
					result.WriteByte ((byte)c);
				}
			}
			return Encoding.ASCII.GetString (result.ToArray ());
		}
	}
}
