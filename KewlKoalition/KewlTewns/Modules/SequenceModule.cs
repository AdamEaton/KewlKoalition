using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Context;

namespace KewlTewns.Modules
{
	[HelpInfoGroup("sequence", "For adjusting playback options.", HelpPriorities.Sequence)]
	public class SequenceModule : KewlTewnsModule
	{
		[SlashCommand("loop", "Adjust queue looping settings.", false, RunMode.Async)]
		public async Task Loop
			(
				[Summary("enable", "Enable to restart the queue when exhausted. (Default: False)")]
				bool enable
			)
		{
			using (responseHandler)
			{
				await Loop(responseHandler, ParsedContextInfo.ParseContext(Context, TewnsRequirement.Dj), enable);
			}
		}
		public static async Task<bool> Loop(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool enable, bool includeResponse = true)
		{
			try
			{
				var controller = PlaybackController.Get(contextInfo.guild);

				if (!await responseHandler.PerformStandardContextResponse(contextInfo, includeResponse ? ResponseMode.Response : ResponseMode.Reply))
					return false;

				if (controller.loop == enable)
				{
					await responseHandler.SendResponse(false, contextInfo.mention + "Looping is already " + (enable ? "en" : "dis") + "abled.");
					return false;
				}

				controller.loop = enable;
				ConsoleUtil.WriteLine("Set loop mode to " + enable);
				if (includeResponse)
					await responseHandler.SendResponse(true, contextInfo.mention + "Looping has been " + (enable ? "en" : "dis") + "abled.");
				else if (responseHandler is IResponseAcknowledger acknowledger)
					await acknowledger.SendAcknowledgement();
				else
					await responseHandler.SendReply(contextInfo.mention + "Looping has been " + (enable ? "en" : "dis") + "abled.");
				return true;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'loop' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}
		}

		[SlashCommand("personalize", "Adjust whether playback should consider the current audience.", false, RunMode.Async)]
		public async Task Personalize
			(
				[Summary("enable", "Enable to exclude songs not endorsed by any present user. (Default: True)")]
				bool enable
			)
		{
			using (responseHandler)
			{
				await Personalize(responseHandler, ParsedContextInfo.ParseContext(Context, TewnsRequirement.Dj), enable);
			}
		}
		public static async Task<bool> Personalize(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool enable, bool includeResponse = true)
		{
			try
			{
				var controller = PlaybackController.Get(contextInfo.guild);

				if (!await responseHandler.PerformStandardContextResponse(contextInfo))
					return false;

				if (controller.personalize == enable)
				{
					await responseHandler.SendResponse(false, contextInfo.mention + "Personalization is already " + (enable ? "en" : "dis") + "abled.");
					return false;
				}

				controller.personalize = enable;
				ConsoleUtil.WriteLine("Set personalization mode to " + enable);
				if (includeResponse)
					await responseHandler.SendResponse(true, contextInfo.mention + "Personalization has been " + (enable ? "en" : "dis") + "abled.");
				else if (responseHandler is IResponseAcknowledger acknowledger)
					await acknowledger.SendAcknowledgement();
				else
					await responseHandler.SendReply(contextInfo.mention + "Personalization has been " + (enable ? "en" : "dis") + "abled.");
				return true;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'personalize' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}
		}

		[SlashCommand("reshuffle", "Reshuffle the current queue.", false, RunMode.Async)]
		public async Task Reshuffle()
		{
			using (responseHandler)
			{
				await Reshuffle(responseHandler, ParsedContextInfo.ParseContext(Context));
			}
		}
		public static async Task<bool> Reshuffle(IResponseHandler responseHandler, ParsedContextInfo contextInfo, bool includeResponse = true)
		{
			try
			{
				var commands = CommandQueue.Get(contextInfo.guild);
				var controller = PlaybackController.Get(contextInfo.guild);

				using (var handle = new CommandHandle(commands))
				{

					if (includeResponse)
						await responseHandler.PerformStandardAcknowledgement(true);

					if (!await handle.WaitToValidate())
						return false;

					if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Reply))
						return false;

					if (!await handle.WaitToExecute())
						return false;

					if (contextInfo.voiceChannel != controller.currentChannel)
					{
						ConsoleUtil.WriteLine("Bot is not in user's voice channel.");
						await responseHandler.SendReply(contextInfo.mention + "I'm not even in your room!");
						return false;
					}

					controller.ShuffleQueue();
					ConsoleUtil.WriteLine("Reshuffled queue by request of user '" + contextInfo.username + "'.");
					await responseHandler.SendReply(contextInfo.mention + "CHAAAAANGE PLACES!!");
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'reshuffle' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}

			return true;
		}
	}
}
