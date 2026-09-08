using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Utilities;

namespace KewlKommon.Modules.Standard
{
	[HelpInfo("thanks", "For expressing appreciation.", HelpPriorities.Thanks)]
	public class ThanksModule : KewlModule
	{
		[SlashCommand("thanks", "Express gratitude to the bot.", false, RunMode.Async)]
		public async Task Thanks()
		{
			using (responseHandler)
			{
				try
				{
					ConsoleUtil.WriteLine("User '" + Context.User.Username + "' is very polite. I like them.");
					await SendResponse(true, "No problem, " + Context.User.Mention + "!");
					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'thanks' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}