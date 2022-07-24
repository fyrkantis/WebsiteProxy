namespace WebsiteProxy
{
	public static class ClockTimer
	{
		public static DateTime? NextTime(TimeSpan[] times)
		{
			return NextTime(times, DateTime.Now);
		}
		public static DateTime? NextTime(TimeSpan[] times, DateTime now)
		{
			if (times.Length <= 0)
			{
				return null;
			}
			foreach (TimeSpan timeOfDay in new TimeSpan[] { now.TimeOfDay, now.TimeOfDay - TimeSpan.FromDays(1)})
			{
				TimeSpan? closestTime = null;
				foreach (TimeSpan time in times)
				{
					if (time > timeOfDay)
					{
						TimeSpan timeUntil = time - timeOfDay;
						if (closestTime == null || timeUntil < closestTime)
						{
							closestTime = timeUntil;
						}
					}
				}
				if (closestTime != null)
				{
					return now + closestTime;
				}
			}
			return null;
		}

		// https://stackoverflow.com/a/18611442/13347795.
		public static void DoAtTime(DateTime time, Action action)
		{
			DoAfterDelay(time - DateTime.Now, action);
		}
		public static void DoAfterDelay(TimeSpan delay, Action action)
		{
			Timer timer = new Timer((x) =>
			{
				action();
			}, null, delay, Timeout.InfiniteTimeSpan);
		}
		public static void DoAfterDelay(long delay, Action action)
		{
			Timer timer = new Timer((x) =>
			{
				action();
			}, null, delay, Timeout.Infinite);
		}
	}
}
