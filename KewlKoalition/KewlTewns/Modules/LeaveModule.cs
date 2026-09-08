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
	[HelpInfo("leave", "For dismissing the bot.", HelpPriorities.Leave)]
	public class LeaveModule : KewlTewnsModule
	{
		[SlashCommand("leave", "Make the bot leave the room.", false, RunMode.Async)]
		public async Task Leave()
		{
			using (responseHandler)
			{
				await Leave(responseHandler, ParsedContextInfo.ParseContext(Context));
			}
		}

		public static async Task Leave(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool includeResponse = false)
		{
			try
			{
				var controller = PlaybackController.Get(contextInfo.guild);
				var commands = CommandQueue.Get(contextInfo.guild);

				if (!await responseHandler.PerformStandardContextResponse(contextInfo))
					return;

				if (contextInfo.voiceChannel != controller.currentChannel && commands.allCommandsCancelled)
				{
					ConsoleUtil.WriteLine("Bot is not in user's voice channel.");
					await responseHandler.SendResponse(false, contextInfo.mention + "I'm not even in your room!");
					return;
				}

				bool hidden = false;
				if (responseHandler is IResponseAcknowledger acknowledger)
				{
					if (!includeResponse)
					{
						hidden = true;
						await acknowledger.SendAcknowledgement();
					}
				}
				if (!hidden)
					await responseHandler.DeferResponse(true);

				commands.CancelCurrentActions();
				await controller.LeaveAsync();
				ConsoleUtil.WriteLine("Left room by request of user '" + contextInfo.username + "'.");
				if (!hidden)
					await responseHandler.SetDeferredResponse(contextInfo.mention + "... and _awaaaaay_ I go!");
				return;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'leave' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return;
			}
		}
	}
}