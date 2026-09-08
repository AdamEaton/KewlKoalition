using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using Discord.WebSocket;
using KewlKommon.Core;
using KewlKommon.Utilities;
using KewlStreams.Core;
using KewlStreams.Utilities;

namespace KewlStreams.Twitch
{
	public static class Announcer
	{
		static readonly TimeSpan RateLimitPeriod = TimeSpan.FromMinutes(10);

		static HashSet<ulong> inProgress = [];

		public static void Init()
		{
			EventUtil.AddListener<StreamStartedEvent>(OnStreamStarted);
		}

		static void OnStreamStarted(StreamStartedEvent e)
		{
			HandleStreamStarted(e.channelId);
		}

		static async void HandleStreamStarted(ulong channelId)
		{
			if (!inProgress.Add(channelId))
				return;

			try
			{
				Directory.CreateDirectory("Timestamps");
				string path = Path.Combine("Timestamps", channelId + ".txt");

				if (File.Exists(path))
				{
					try
					{
						using (var inStream = new FileStream(path, FileMode.Open))
						using (var reader = new StreamReader(inStream))
						{
							var date = DateTimeUtil.DeserializeDateTime(reader.ReadLine());
							if (DateTime.Now - date < RateLimitPeriod)
							{
								ConsoleUtil.WriteLine("Already announced channel with ID '" + channelId + "' within the rate limit period.");
								return;
							}
						}
					}
					catch (Exception ex)
					{
						ConsoleUtil.WriteLine("Error while checking last announcement time for channel with ID '" + channelId + "':", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					}
				}

				var info = await TwitchManager.GetTwitchInfo(channelId);
				
				var embed = new EmbedBuilder
				{
					Title = info.name + " is streaming " + info.game + "!",
					Description = info.title
				};
				embed.WithUrl(info.Url)
					.WithImageUrl(info.logo)
					.WithColor(KewlProgram.Bot.themeColor)
					.WithCurrentTimestamp();

				ConsoleUtil.WriteLine("Announcing channel '" + info.name + "'.");


				foreach (var identifier in Program.Bot.config.guildIds ?? [])
				{
					try
					{
						var guildConfig = Program.Bot.config.GetGuildConfig(identifier);

						if (!guildConfig.TryGetSpecialChannel(StreamsUtil.AnnouncementsChannelKey, out var channel) || channel is not SocketTextChannel textChannel)
						{
							ConsoleUtil.WriteLine("Could not find appropriate announcements channel in server '" + identifier + "'.", ConsoleColor.Yellow);
							continue;
						}

						await textChannel.SendMessageAsync(embed: embed.Build());
					}
					catch (Exception ex)
					{
						ConsoleUtil.WriteLine("Error while announcing channel in server '" + identifier + "':", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					}
				}

				try
				{
					using (var outStream = new FileStream(path, FileMode.OpenOrCreate))
					using (var writer = new StreamWriter(outStream))
					{
						writer.WriteLine(DateTimeUtil.SerializeDateTime(DateTime.Now));
					}
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while recording announcement time for channel with ID '" + channelId + "':", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				}
			}
			finally
			{
				inProgress.Remove(channelId);
			}
		}
	}
}