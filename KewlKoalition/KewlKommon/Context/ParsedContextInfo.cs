using System;
using System.Linq;
using Discord.WebSocket;
using Discord.Interactions;
using KewlKommon.Core;
using KewlKommon.Extensions;

namespace KewlKommon.Context
{
	public abstract class ParsedContextInfo
	{
		public static ParsedInteractionContextInfo ParseContext(SocketInteractionContext context)
		{
			return ParseContext(context, KewlProgram.Bot.defaultContextRequirements);
		}
		public static ParsedInteractionContextInfo ParseContext(SocketInteractionContext context, params Requirement[] requirements)
		{
			var output = new ParsedInteractionContextInfo(context);
			output.errorCode = ErrorCode.None;

			output.guild = context.Guild;
			if (!context.User.MutualGuilds.Contains(output.guild) && !KewlProgram.Bot.configBase.guildIds.Contains(output.guild.Id))
			{
				output.errorCode = ErrorCode.GuildNotFound;
				return output;
			}

			output.user = output.guild.GetUser(context.User.Id);
			output.voiceChannel = output.user?.VoiceChannel;
			if (output.user == null)
			{
				output.errorCode = ErrorCode.UserNotFound;
				return output;
			}

			foreach (var requirement in requirements)
			{
				output.errorCode = requirement.Check(output);
				if (output.errorCode != ErrorCode.None)
					return output;
			}

			return output;
		}
		public static ParsedMessageComponentContextInfo ParseContext(SocketMessageComponent context, params Requirement[] requirements)
		{
			var output = new ParsedMessageComponentContextInfo(context);
			output.errorCode = ErrorCode.None;

			output.guild = context.GetGuild();
			if (output.guild == null || !context.User.MutualGuilds.Contains(output.guild) && !KewlProgram.Bot.configBase.guildIds.Contains(output.guild.Id))
			{
				output.errorCode = ErrorCode.GuildNotFound;
				return output;
			}

			output.user = output.guild.GetUser(context.User.Id);
			output.voiceChannel = output.user?.VoiceChannel;
			if (output.user == null)
			{
				output.errorCode = ErrorCode.UserNotFound;
				return output;
			}

			foreach (var requirement in requirements)
			{
				output.errorCode = requirement.Check(output);
				if (output.errorCode != ErrorCode.None)
					return output;
			}

			return output;
		}

		public ErrorCode errorCode = ErrorCode.None;
		public SocketGuild? guild = null;
		public SocketGuildUser? user = null;
		public SocketVoiceChannel? voiceChannel = null;

		public string mention { get { return user != null ? user.Mention + " " : ""; } }
		public string username { get { return user != null ? user.Username : "[Unknown]"; } }

		public enum ErrorCode
		{
			None,
			GuildNotFound,
			UserNotFound,
			UserNotAuthorized,
			UserNotConnected,
		}
	}
	public abstract class ParsedContextInfo<T> : ParsedContextInfo
	{
		public T context { get; private set; }

		public ParsedContextInfo(T context)
		{
			this.context = context;
		}
	}
	public class ParsedInteractionContextInfo : ParsedContextInfo<SocketInteractionContext>
	{
		public ParsedInteractionContextInfo(SocketInteractionContext context)
			: base(context)
		{

		}
	}
	public class ParsedMessageComponentContextInfo : ParsedContextInfo<SocketMessageComponent>
	{
		public ParsedMessageComponentContextInfo(SocketMessageComponent context)
			: base(context)
		{

		}
	}
}
