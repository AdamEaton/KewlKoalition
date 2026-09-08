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
	[HelpInfo("pause", "For suspending playback.", HelpPriorities.Pause)]
	public class PauseModule : KewlTewnsModule
	{
		[SlashCommand("pause", "Suspend playback at the end of the current song. Use **/resume** to continue.", false, RunMode.Async)]
		public async Task Pause()
		{
			using (responseHandler)
			{
				await Pause(responseHandler, ParsedContextInfo.ParseContext(Context));
			}
		}

		public static async Task Pause(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool includeResponse = false)
		{
			try
			{
				var controller = PlaybackController.Get(contextInfo.guild);

				if (!await responseHandler.PerformStandardContextResponse(contextInfo))
					return;

				if (controller == null || contextInfo.voiceChannel != controller.currentChannel)
				{
					ConsoleUtil.WriteLine("Bot is not in user's voice channel.");
					await responseHandler.SendResponse(false, contextInfo.mention + "I'm not even in your room!");
					return;
				}

				if (controller == null || !controller.GetQueuedTracks(1).Any() && string.IsNullOrEmpty(controller.currentTrack))
				{
					ConsoleUtil.WriteLine("Ignoring 'pause' command due to lack of queue.");
					await responseHandler.SendResponse(false, contextInfo.mention + "It doesn't seem like there's anything to pause.");
					return;
				}
				if (controller.paused)
				{
					ConsoleUtil.WriteLine("Ignoring 'pause' command because already paused.");
					await responseHandler.SendResponse(false, contextInfo.mention + "I'm already hanging on!");
					return;
				}

				controller.paused = true;
				ConsoleUtil.WriteLine("Paused queue by request of user '" + contextInfo.username + "'.");

				if (!includeResponse && responseHandler is IResponseAcknowledger acknowledger)
					await acknowledger.SendAcknowledgement();
				else
					await responseHandler.SendResponse(includeResponse, contextInfo.mention + "I'm hanging on!");
				return;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'pause' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return;
			}
		}
	}
}