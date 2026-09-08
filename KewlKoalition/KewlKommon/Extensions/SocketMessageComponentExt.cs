using System.Collections.Generic;
using Discord.WebSocket;
using KewlKommon.Interaction;

namespace KewlKommon.Extensions
{
	public static class SocketMessageComponentExt
	{
		static Dictionary<SocketMessageComponent, MessageComponentResponseHandler> ButtonHandlers = [];
		public static void RegisterHandler(MessageComponentResponseHandler responseHandler)
		{
			if (responseHandler == null)
				return;

			ButtonHandlers[responseHandler.target] = responseHandler;
		}
		public static void DisposeHandler(MessageComponentResponseHandler responseHandler)
		{
			if (responseHandler == null)
				return;

			ButtonHandlers.Remove(responseHandler.target);
		}

		public static SocketTextChannel? GetChannel(this SocketMessageComponent component)
		{
			return component.Channel as SocketTextChannel;
		}
		public static SocketGuild? GetGuild(this SocketMessageComponent component)
		{
			return component.GetChannel()?.Guild;
		}
		public static SocketGuildUser? GetUser(this SocketMessageComponent component)
		{
			return component.GetGuild()?.GetUser(component.User.Id);
		}
		public static string GetCustomId(this SocketMessageComponent component)
		{
			return component.Data.CustomId;
		}

		public static bool GetInfo(this SocketMessageComponent component, out SocketTextChannel? channel, out SocketGuild? guild, out SocketGuildUser? user)
		{
			channel = null;
			guild = null;
			user = null;

			try
			{
				channel = component.GetChannel();
				guild = channel?.Guild;
				user = guild?.GetUser(component.User.Id);
				return user != null;
			}
			catch { return false; }
		}

		public static MessageComponentResponseHandler GetResponseHandler(this SocketMessageComponent component)
		{
			if (!ButtonHandlers.TryGetValue(component, out var output))
			{
				output = new MessageComponentResponseHandler(component);
				return output;
			}
			return output;
		}
	}
}