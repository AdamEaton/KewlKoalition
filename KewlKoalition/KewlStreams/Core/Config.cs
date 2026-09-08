using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using KewlKommon.Core;
using KewlKommon.Extensions;

namespace KewlStreams.Core
{
	public class Config : KewlConfig<GuildConfig>
	{
		public static string GetChannelFile(string identifier)
		{
			return Path.Combine("Data", identifier + ".txt");
		}

		public string? twitchClientId { get; private set; } = null;
		public string? twitchClientSecret { get; private set; } = null;
		public string? twitchRedirectUri { get; private set; } = null;

		public string? botAccessToken { get; private set; } = null;
		public string? botRefreshToken { get; private set; } = null;

		protected override bool Extract(JsonNode? node)
		{
			try
			{
				if (!base.Extract(node))
					return false;

				if (!node.TryGetObject("twitch", out var twitch))
					return false;
				if (!twitch.TryGetString("clientId", out var clientId))
					return false;
				if (!twitch.TryGetString("clientSecret", out var clientSecret))
					return false;
				if (!twitch.TryGetString("redirectUri", out var redirectUri))
					return false;

				twitchClientId = clientId;
				twitchClientSecret = clientSecret;
				twitchRedirectUri = redirectUri;

				botAccessToken = twitch.TryGetString("accessToken", out var accessToken) ? accessToken : null;
				botRefreshToken = twitch.TryGetString("refreshToken", out var refreshToken) ? refreshToken : null;

				return true;
			}
			catch { return false; }
		}

		public static IEnumerable<ulong> GetAllBroadcasters()
		{
			return Program.Config.guildIds.Distinct();
		}

		public bool Refresh(string accessToken)
		{
			try
			{
				botAccessToken = accessToken;

				if (!string.IsNullOrEmpty(path))
					return OverwriteStringValue(path, "accessToken", accessToken);

				return true;
			}
			catch { return false; }
		}
		public bool Reauthorize((string accessToken, string refreshToken) tokens)
		{
			try
			{
				botRefreshToken = tokens.refreshToken;
				botAccessToken = tokens.accessToken;

				if (!string.IsNullOrEmpty(path))
					return OverwriteStringValue(path, "accessToken", tokens.accessToken)
						&& OverwriteStringValue(path, "refreshToken", tokens.refreshToken);

				return true;
			}
			catch { return false; }
		}
	}
	public class GuildConfig : KewlGuildConfig
	{
		List<ulong> _streams = [];
		public IReadOnlyList<ulong> streams
		{
			get
			{
				return _streams.AsReadOnly();
			}
		}

		public override bool Extract(JsonNode? node)
		{
			if (!base.Extract(node))
				return false;

			_streams.Clear();
			if (node.TryGetArray("streams", out var streams))
				_streams = [.. streams.Select(JsonNodeExt.AsUlong)];

			return true;
		}
}

public class StreamInfo
	{
		public string name;
		public string title;
		public string game;
		public DateTime startTime;
		public string logo;
		
		public string Url { get { return "http://www.twitch.tv/" + name; } }

		public StreamInfo(string name, string title, string game, DateTime startTime, string logo)
		{
			this.name = name;
			this.title = title;
			this.game = game;
			this.startTime = startTime;
			this.logo = logo;
		}
	}
}