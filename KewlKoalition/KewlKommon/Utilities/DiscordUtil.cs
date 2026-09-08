using System.Linq;

namespace KewlKommon.Utilities
{
	public static class DiscordUtil
	{
		public static string ExtractUserID(string userMention)
		{
			return new string([.. userMention.Where(char.IsDigit)]);
		}
	}
}