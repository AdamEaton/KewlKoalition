using System;
using KewlKommon.Components;
using KewlKommon.Context;

namespace KewlKommon.Utilities
{
	public static class KewlUtil
	{
		public const string AdminRoleKey = "admin";
		public const string BotRoleKey = "bot";

		public static IPerGuildSingleton? GetSingletonForGuild(Type type, ulong guildId)
		{
			return typeof(KewlUtil)
				.GetMethod(nameof(GetSingletonForGuild), 1, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, [typeof(ulong)])
				?.MakeGenericMethod(type)
				.Invoke(null, [guildId]) as IPerGuildSingleton;
		}
		public static T GetSingletonForGuild<T>(ulong guildId)
			where T : PerGuildSingleton<T>, new()
		{
			return PerGuildSingleton<T>.Get(guildId);
		}

		public const int MajorVersion = 3;
		public const int MinorVersion = 0;

		public const int MaxMessageLength = 2000;

		public static string? GetStandardContextReply(ParsedContextInfo contextInfo)
		{
			switch (contextInfo.errorCode)
			{
				case ParsedContextInfo.ErrorCode.GuildNotFound:
					return contextInfo.mention + "I can't help you - I'm lost!";
				case ParsedContextInfo.ErrorCode.UserNotFound:
					return contextInfo.mention + "Who even are you?";
				case ParsedContextInfo.ErrorCode.UserNotAuthorized:
					return contextInfo.mention + "You're not my mom!";
				case ParsedContextInfo.ErrorCode.UserNotConnected:
					return contextInfo.mention + "Join a voice room first.";
			}
			return null;
		}
		public static string? GetStandardContextLog(ParsedContextInfo contextInfo)
		{
			var username = contextInfo.user == null ? "[Unknown]" : contextInfo.user.Username;

			switch (contextInfo.errorCode)
			{
				case ParsedContextInfo.ErrorCode.GuildNotFound:
					return "Guild not found.";
				case ParsedContextInfo.ErrorCode.UserNotFound:
					return "User '" + username + "' could not be found in server.";
				case ParsedContextInfo.ErrorCode.UserNotAuthorized:
					return "User '" + username + "' does not have authority to control the bot.";
				case ParsedContextInfo.ErrorCode.UserNotConnected:
					return "User is not in a voice channel.";
			}
			return null;
		}
	}
}