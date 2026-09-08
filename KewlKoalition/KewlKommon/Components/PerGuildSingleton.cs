using System;
using System.Collections.Generic;
using Discord.WebSocket;
using KewlKommon.Core;

namespace KewlKommon.Components
{
	public interface IPerGuildSingleton
	{
	}
	public class PerGuildSingleton<T> : IPerGuildSingleton
		where T : PerGuildSingleton<T>, new()
	{
		static Dictionary<ulong, T> _Instances = [];
		public static IEnumerable<T> Instances { get { return _Instances.Values; } }
		public static T Get(ulong guildId)
		{
			if (!_Instances.TryGetValue(guildId, out var output))
			{
				output = new T();
				output.guildId = guildId;
				_Instances[guildId] = output;
			}
			return output;
		}
		public static T Get(SocketGuild? guild)
		{
			ArgumentNullException.ThrowIfNull(guild);

			return Get(guild.Id);
		}

		public TOther Get<TOther>() where TOther : PerGuildSingleton<TOther>, new()
		{
			return PerGuildSingleton<TOther>.Get(guildId);
		}

		public ulong guildId { get; private set; }
		public SocketGuild? guild { get { return KewlProgram.Bot.GetGuild(guildId); } }
	}
	public interface IPerGuildMonitor : IPerGuildSingleton
	{
		void Monitor();
	}
	public abstract class PerGuildMonitor<T> : PerGuildSingleton<T>, IPerGuildMonitor
		where T : PerGuildMonitor<T>, new()
	{
		public abstract void Monitor();
	}
}