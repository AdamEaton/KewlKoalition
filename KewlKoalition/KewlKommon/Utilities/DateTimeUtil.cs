using System;

namespace KewlKommon.Utilities
{
	public static class DateTimeUtil
	{
		public static string SerializeDateTime(DateTime date)
		{
			string output = "";
			output += date.Year.ToString("0000");
			output += "-";
			output += date.Month.ToString("00");
			output += "-";
			output += date.Day.ToString("00");
			output += " ";
			output += date.Hour.ToString("00");
			output += ":";
			output += date.Minute.ToString("00");
			output += ":";
			output += date.Second.ToString("00");
			return output;
		}
		public static DateTime DeserializeDateTime(string? date)
		{
			try
			{
				if (string.IsNullOrEmpty(date))
					return DateTime.FromBinary(0);

				var split = date.Split(' ');
				var day = split[0].Split('-');
				var time = split[1].Split(':');

				return new DateTime(int.Parse(day[0]), int.Parse(day[1]), int.Parse(day[2]), int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]));
			}
			catch (Exception)
			{
				return DateTime.FromBinary(0);
			}
		}
	}
}