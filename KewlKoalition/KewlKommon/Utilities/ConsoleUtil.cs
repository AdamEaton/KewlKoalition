using System;

namespace KewlKommon.Utilities
{
	public static class ConsoleUtil
	{
		public static void WriteLine(object? message, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
		{
			using (new Color(foregroundColor ?? Console.ForegroundColor, backgroundColor ?? Console.BackgroundColor))
			{
				var messageString = message?.ToString();
				if (!string.IsNullOrEmpty(messageString))
				{
					if (messageString.Contains("GatewayReconnectException"))
						return;
					if (messageString.Contains("The remote party closed the WebSocket connection without completing the close handshake."))
						return;
					if (messageString.Contains("TaskCanceledException"))
						return;
				}

				Console.WriteLine(DateTimeUtil.SerializeDateTime(DateTime.Now) + "| " + messageString);
			}
		}
		public static string? ReadLine(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
		{
			using (new Color(foregroundColor ?? Console.ForegroundColor, backgroundColor ?? Console.BackgroundColor))
			{
				Console.Write(DateTimeUtil.SerializeDateTime(DateTime.Now) + "| ");
				return Console.ReadLine();
			}
		}

		public class Color : IDisposable
		{
			ConsoleColor foregroundColor;
			ConsoleColor backgroundColor;

			public Color(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
			{
				this.foregroundColor = Console.ForegroundColor;
				this.backgroundColor = Console.BackgroundColor;

				Console.ForegroundColor = foregroundColor ?? Console.ForegroundColor;
				Console.BackgroundColor = backgroundColor ?? Console.BackgroundColor;
			}
			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Console.ForegroundColor = foregroundColor;
				Console.BackgroundColor = backgroundColor;
			}
		}
	}
}