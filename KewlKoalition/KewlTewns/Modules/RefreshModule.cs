using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;

namespace KewlTewns.Modules
{
	[HelpInfo("refresh", "For reconnecting the bot if something goes wrong.", HelpPriorities.Refresh)]
	public class RefreshModule : KewlTewnsModule
	{
		[SlashCommand("refresh", "Force the bot to reconnect (useful if the audio stream unexpectedly stops).", false, RunMode.Async)]
		public async Task Refresh()
		{
			using (responseHandler)
			{
				await Refresh(responseHandler, ParsedContextInfo.ParseContext(Context));
			}
		}

		public static async Task Refresh(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool includeResponse = false)
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

				ConsoleUtil.WriteLine("Refreshing audio stream by request of user '" + contextInfo.username + "'.");
				if (!includeResponse && responseHandler is IResponseAcknowledger acknowledger)
					await acknowledger.SendAcknowledgement();
				else
					await responseHandler.SendResponse(true, contextInfo.mention + "Refreshing audio connection.");
				await controller.Refresh();
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