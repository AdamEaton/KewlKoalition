using Discord.WebSocket;
using KewlKommon.Utilities;
using KewlTewns.Components;

namespace KewlTewns.Utilities
{
	public static class TewnsUtil
	{
		public const string DjRoleKey = "dj";

		public const string EmptyDisplayString = "[Nothing]";

		public static string GetSongDisplayString(SocketGuild? guild, string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return EmptyDisplayString;

			string output = YoutubeUtil.SongIDToURL(id) ?? EmptyDisplayString;
			if (SheetsService.Get(guild).TryGetSongData(id, out var songData) && songData != null)
				output = songData.DisplayInfo + "\n" + output;
			return output;
		}
	}
}