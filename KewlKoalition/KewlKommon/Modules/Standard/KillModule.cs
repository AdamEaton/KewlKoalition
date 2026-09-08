using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Utilities;

namespace KewlKommon.Modules.Standard
{
	[HelpInfo("kill", "For shutting down the bot. (Admins only)", HelpPriorities.Kill)]
	public class KillModule : KewlModule
	{
		[SlashCommand("kill", "Force the bot to log out and shut down. (Admins only)", false, RunMode.Async)]
		public async Task Kill()
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context, Requirement.Admin);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					ConsoleUtil.WriteLine("Shutting down by request of user '" + Context.User.Username + "'.");
					await SendResponse(true, Context.User.Mention + " Rip me.");

					KewlProgram.Bot.ShutDown();
					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'kill' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}