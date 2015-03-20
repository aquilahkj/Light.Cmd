using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace Light.Cmd
{
	static class WorkTaskManager
	{
		static Dictionary<Timer, WorkTask> Dict = new Dictionary<Timer, WorkTask> ();

		public static void AddWorkTask (WorkTask task)
		{
			if (task == null) {
				return;
			}
			Timer timer = new Timer (task.Command.Timespan * 1000);
			Dict [timer] = task;
			timer.AutoReset = false;
			timer.Elapsed += new ElapsedEventHandler (timer_Elapsed);
			timer.Start ();
		}

		static void timer_Elapsed (object sender, ElapsedEventArgs e)
		{
			Timer timer = sender as Timer;
			if (timer == null) {
				return;
			}
			if (Dict.ContainsKey (timer)) {
				WorkTask task = Dict [timer];
				Dict.Remove (timer);
				if (task != null) {
					try {
						task.Handler (task.Command, task.Callback);
					}
					catch (Exception exception) {
						if (task.Callback != null) {
							try {
								task.Callback (task.Command, CallbackType.ERROR, exception.Message);
							}
							catch {

							}
						}
					}
				}
			}
			timer.Stop ();
			timer.Close ();
		}
	}
}
