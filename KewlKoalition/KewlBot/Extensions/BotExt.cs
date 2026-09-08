using Discord.WebSocket;
using KewlKommon.Extensions;
using KewlBot.Utilities;

namespace KewlBot.Extensions
{
	public static class BotExt
	{
		public static bool IsConvict(this SocketGuildUser? user)
		{
			return user?.HasSpecialRole(BotUtil.ConvictRoleKey) ?? false;
		}
	}
}
