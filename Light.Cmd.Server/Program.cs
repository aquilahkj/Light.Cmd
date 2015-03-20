using System;
using System.Threading;

namespace Light.Cmd.Server
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			bool debug = false;
			if (args.Length > 0 && args [0] == "debug") {
				debug = true;
			}
			ServerManager.StartCmdService ("conf", debug);

			while (true) {
				Thread.Sleep (1000 * 60);
			}
		}
	}
}
