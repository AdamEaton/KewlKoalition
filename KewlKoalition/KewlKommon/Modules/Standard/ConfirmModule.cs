using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Utilities;

namespace KewlKommon.Modules.Standard
{
	[HelpInfo("confirm", "For testing the bot or supporting your arguments.", HelpPriorities.Confirm)]
	public class ConfirmModule : KewlModule
	{
		[SlashCommand("confirm", "Output a confirmation message that the bot is still running.", false, RunMode.Async)]
		public async Task Confirm
			(
				[Summary("message", "The text of the message to repeat (omit to receive a generic response).")]
				string? message = null
			)
		{
			using (responseHandler)
			{
				try
				{
					string reply;

					if (string.IsNullOrWhiteSpace(message))
						reply = Context.User.Mention + " Still ticking!";
					else
						reply = "Just like " + Context.User.Mention + " said, " + message;

					if (reply.Length > 2000)
					{
						await SendResponse(false, Context.User.Mention + " I can't be expected to repeat _all of that_!");
						return;
					}

					ConsoleUtil.WriteLine("Sending confirmation message by request of '" + Context.User.Username + "'.");
					await SendResponse(true, reply);
					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'confirm' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}