using System.Linq;
using Discord.WebSocket;
using KewlKommon.Core;
using KewlKommon.Utilities;

namespace KewlKommon.Extensions
{
	public static class KewlExt
	{
		public static bool IsSpecialRole(this SocketRole? role, string roleKey)
		{
			try
			{
				if (role == null)
					return false;

				return role.Id == KewlProgram.Bot.configBase.GetGuildConfigBase(role.Guild.Id).GetSpecialRoleId(roleKey);
			}
			catch { }

			return false;
		}
		public static SocketRole? GetSpecialRole(this SocketGuild guild, string roleKey)
		{
			try
			{
				return guild.Roles.FirstOrDefault(x => x.IsSpecialRole(roleKey));
			}
			catch
			{
				return null;
			}
		}
		public static bool HasSpecialRole(this SocketGuildUser user, string roleKey)
		{
			try
			{
				return user.Roles.Any(x => x.IsSpecialRole(roleKey));
			}
			catch
			{
				return false;
			}
		}

		public static bool IsAdmin(this SocketGuildUser? user)
		{
			return user?.HasSpecialRole(KewlUtil.AdminRoleKey) ?? false;
		}
		public static bool IsBot(this SocketGuildUser? user)
		{
			return user?.HasSpecialRole(KewlUtil.BotRoleKey) ?? false;
		}
	}
}
