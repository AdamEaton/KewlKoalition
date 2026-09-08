using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KewlKommon.Core;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlStreams.Core;
using KewlStreams.Twitch;

namespace KewlStreams.Modules
{
	[HelpInfo("live", "For getting information about community Twitch streamers.", HelpPriorities.Live)]
	public class LiveModule : KewlStreamsModule
	{
		[SlashCommand("live", "Display a list of community Twitch streamers who are currently live.", false, RunMode.Async)]
		public async Task Live()
		{
			using (responseHandler)
			{
				try
				{
					ConsoleUtil.WriteLine("Showing live channels info.");

					var embed = new EmbedBuilder
					{
						Title = "Who's Online",
						Description = "Don't forget to leave a follow!"
					};
					embed.WithColor(KewlProgram.Bot.themeColor)
						.WithCurrentTimestamp();

					await DeferResponse(true);

					var guildConfig = Program.Bot.config.GetGuildConfig(Context.Guild.Id);

					foreach (var stream in TwitchManager.GetActiveBroadcasters())
					{
						if (!guildConfig.streams.Contains(stream))
							continue;

						var info = await TwitchManager.GetTwitchInfo(stream);

						embed.AddField(info.name + " is playing " + info.game, info.title + "\n" + info.Url);
					}

					if (embed.Fields.Count <= 0)
					{
						await SetDeferredResponse("Looks like no one is online currently...");
					}
					else
					{
						await SetDeferredResponse(embed: embed.Build());
					}
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'live' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				}
			}
		}
	}
}