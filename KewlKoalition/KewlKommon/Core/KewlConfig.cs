using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using KewlKommon.Extensions;

namespace KewlKommon.Core
{
	public abstract class KewlConfig
	{
		public const string ConfigsPath = "Configs";
		public const string Extension = ".json";
		public const string DefaultName = "config";

		public static string FormatPath(string path)
		{
			path = path.Trim();
			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			while (path.StartsWith(Path.DirectorySeparatorChar))
				path = path[1..];
			if (!path.StartsWith(ConfigsPath, StringComparison.InvariantCultureIgnoreCase))
				path = Path.Combine(ConfigsPath, path);
			if (!path.EndsWith(Extension, StringComparison.InvariantCultureIgnoreCase))
				path += Extension;

			return path;
		}

		public string? path = null;

		public string? discordToken { get; private set; } = null;

		public abstract KewlGuildConfig GetGuildConfigBase(ulong guildId);
		public abstract IEnumerable<ulong> guildIds { get; }

		protected abstract KewlGuildConfig CreateGuildConfig();
		protected abstract void CacheGuildConfig(KewlGuildConfig config);

		public static string FixFriendlyJson(string[] lines)
		{
			return string.Join("",
				lines
					.Select(x => x.Trim())
					.Where(x => !x.StartsWith("//"))
					.ToArray())
				.Replace(",}", "}")
				.Replace(",]", "]");
		}
		public bool Load(string? path)
		{
			this.path = path;
			if (!Read(path, out var node))
				return false;

			return Extract(node);
		}
		static bool Read(string? path, [NotNullWhen(true)] out JsonNode? node)
		{
			node = null;
			try
			{
				if (string.IsNullOrEmpty(path))
					return false;

				path = FormatPath(path);

				node = JsonNode.Parse(FixFriendlyJson(File.ReadAllLines(path)));
				return node != null;
			}
			catch { }

			return false;
		}

		protected virtual bool Extract(JsonNode? node)
		{
			try
			{
				if (!node.TryGetArray("servers", out var servers))
					return false;
				foreach (var server in servers)
				{
					var config = CreateGuildConfig();
					if (config.Extract(server))
						CacheGuildConfig(config);
				}

				if (!node.TryGetObject("discord", out var discord))
					return false;
				if (!discord.TryGetString("token", out var token))
					return false;

				discordToken = token;
				return true;
			}
			catch { return false; }
		}

		protected virtual bool OverwriteStringValue(string path, string key, string value)
		{
			try
			{
				path = FormatPath(path);
				File.WriteAllText(path, Regex.Replace(
					File.ReadAllText(path),
					"(\\s*\"" + Regex.Escape(key) + "\"\\s*:\\s*\")[^\"]*(\")",
					m => m.Groups[1].Value + value + m.Groups[2].Value,
					RegexOptions.IgnoreCase));
				return true;
			}
			catch { return false; }
		}
	}
	public class KewlConfig<TGuildConfig> : KewlConfig
		where TGuildConfig : KewlGuildConfig, new()
	{
		protected Dictionary<ulong, TGuildConfig> _guilds = [];

		public override KewlGuildConfig GetGuildConfigBase(ulong guildId)
		{
			return GetGuildConfig(guildId);
		}
		public TGuildConfig GetGuildConfig(ulong guildId)
		{
			return _guilds[guildId];
		}
		protected override KewlGuildConfig CreateGuildConfig()
		{
			return new TGuildConfig();
		}
		protected override void CacheGuildConfig(KewlGuildConfig config)
		{
			if (config is TGuildConfig guildConfig)
				_guilds[config.id] = guildConfig;
		}

		public override IEnumerable<ulong> guildIds
		{
			get
			{
				return _guilds.Keys;
			}
		}
	}

	public class KewlGuildConfig
	{
		public ulong id { get; private set; }
		public SocketGuild GetGuild()
		{
			return KewlProgram.Bot.GetGuild(id) ?? throw new InvalidOperationException("Guild with ID '" + id + "' not found!");
		}

		Dictionary<string, ulong> specialRoleIds = new Dictionary<string, ulong>(StringComparer.InvariantCultureIgnoreCase);
		public ulong GetSpecialRoleId(string roleKey)
		{
			return specialRoleIds.TryGetValue(roleKey, out var roleId) ? roleId : 0UL;
		}
		public bool TryGetSpecialRole(string roleKey, [NotNullWhen(true)] out SocketRole? role)
		{
			role = GetGuild().Roles.FirstOrDefault(x => x.Id == GetSpecialRoleId(roleKey));
			return role != null;
		}

		Dictionary<string, ulong> specialChannelIds = new Dictionary<string, ulong>(StringComparer.InvariantCultureIgnoreCase);
		public ulong GetSpecialChannelId(string channelKey)
		{
			return specialChannelIds.TryGetValue(channelKey, out var channelId) ? channelId : 0UL;
		}
		public bool TryGetSpecialChannel(string channelKey, [NotNullWhen(true)] out SocketChannel? channel)
		{
			channel = GetGuild().Channels.FirstOrDefault(x => x.Id == GetSpecialChannelId(channelKey));
			return channel != null;
		}

		public virtual bool Extract(JsonNode? node)
		{
			if (!node.TryGetValue("id", out var id))
				return false;
			this.id = id.GetValue<ulong>();

			if (node.TryGetObject("roles", out var roles))
				specialRoleIds = ConvertToDictionary(roles);
			if (node.TryGetObject("channels", out var channels))
				specialChannelIds = ConvertToDictionary(channels);

			return true;

			static Dictionary<string, ulong> ConvertToDictionary(JsonObject obj)
			{
				var output = new Dictionary<string, ulong>(StringComparer.InvariantCultureIgnoreCase);
				foreach (var kvp in obj)
				{
					try
					{
						if (string.IsNullOrEmpty(kvp.Key))
							continue;
						if (kvp.Value == null)
							continue;
						output[kvp.Key] = kvp.Value.GetValue<ulong>();
					}
					catch { }
				}
				return output;
			}
		}
	}
}