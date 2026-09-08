using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	[HelpInfo("skip", "For skipping songs during playback.", HelpPriorities.Skip)]
	public class SkipModule : KewlTewnsModule
	{
		[SlashCommand("skip", "Skip the currently playing track.", false, RunMode.Async)]
		public async Task Skip()
		{
			using (responseHandler)
			{
				await Skip(responseHandler, ParsedContextInfo.ParseContext(Context));
			}
		}

		public static async Task Skip(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool includeResponse = false)
		{
			try
			{
				var controller = PlaybackController.Get(contextInfo.guild);

				if (!await responseHandler.PerformStandardContextResponse(contextInfo))
					return;

				if (contextInfo.voiceChannel != controller.currentChannel)
				{
					ConsoleUtil.WriteLine("Bot is not in user's voice channel.");
					await responseHandler.SendResponse(false, contextInfo.mention + "I'm not even in your room!");
					return;
				}
				if (string.IsNullOrEmpty(controller.currentTrack))
				{
					ConsoleUtil.WriteLine("Skip command ignored.");
					await responseHandler.SendResponse(false, contextInfo.mention + "I can't skip what I'm not playing...");
					return;
				}

				ConsoleUtil.WriteLine("Skipped song with ID '" + controller.currentTrack + "' by request of user '" + contextInfo.username + "'.");
				if (!includeResponse && responseHandler is IResponseAcknowledger acknowledger)
					await acknowledger.SendAcknowledgement();
				else
					await responseHandler.SendResponse(true, contextInfo.mention + "Skipped song\n"
						+ TewnsUtil.GetSongDisplayString(contextInfo.guild, controller.currentTrack));
				controller.Skip();
				return;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'skip' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return;
			}
		}
	}
}