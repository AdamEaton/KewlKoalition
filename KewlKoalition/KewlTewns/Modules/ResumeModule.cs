using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;

namespace KewlTewns.Modules
{
	[HelpInfo("resume", "For continuing previously suspended playback.", HelpPriorities.Resume)]
	public class ResumeModule : KewlTewnsModule
	{
		[SlashCommand("resume", "Resume playback that has been suspended with **/pause**.", false, RunMode.Async)]
		public async Task Resume()
		{
			using (responseHandler)
			{
				await Resume(responseHandler, ParsedContextInfo.ParseContext(Context));
			}
		}

		public static async Task Resume(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool includeResponse = true)
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

				if (!controller.GetQueuedTracks(1).Any())
				{
					ConsoleUtil.WriteLine("Ignoring 'resume' command due to lack of queue.");
					await responseHandler.SendResponse(false, contextInfo.mention + "It doesn't seem like there's anything to resume.");
					return;
				}
				if (!controller.paused)
				{
					ConsoleUtil.WriteLine("Ignoring 'resume' command because not paused.");
					await responseHandler.SendResponse(false, contextInfo.mention + "That really only works if something's paused.");
					return;
				}

				controller.paused = false;
				ConsoleUtil.WriteLine("Resumed queue by request of user '" + contextInfo.username + "'.");
				if (!includeResponse && responseHandler is IResponseAcknowledger acknowledger)
					await acknowledger.SendAcknowledgement();
				else
				{
					if (controller.isPlaying)
						await responseHandler.SendResponse(includeResponse, contextInfo.mention + "Alright, nevermind then.");
					else
						await responseHandler.SendResponse(includeResponse, contextInfo.mention + "Let's see, where were we?");
				}
				return;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'resume' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return;
			}
		}
	}
}