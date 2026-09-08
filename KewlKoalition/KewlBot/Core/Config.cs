using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Discord.WebSocket;
using KewlKommon.Core;
using KewlKommon.Extensions;

namespace KewlBot.Core
{
	public class GuildConfig : KewlGuildConfig
	{
		IList<ulong> _tempChannelExceptions = [];
		public IReadOnlyList<ulong> tempChannelExceptions
		{
			get
			{
				return _tempChannelExceptions.AsReadOnly();
			}
		}
		public bool IsTempChannelException(SocketGuildChannel channel)
		{
			return tempChannelExceptions.Contains(channel.Id);
		}

		public override bool Extract(JsonNode? node)
		{
			try
			{
				if (!base.Extract(node))
					return false;

				if (node.TryGetArray("tempExceptions", out var tempExceptions))
					_tempChannelExceptions = [.. tempExceptions.Select(
					x =>
					{
						try
						{
							return x?.GetValue<ulong>();
						}
						catch { }
						return null;
					}).OfType<ulong>()];
			}
			catch
			{
				return false;
			}

			return true;
		}
	}
}
