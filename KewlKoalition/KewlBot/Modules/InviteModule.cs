using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Modules;
using KewlKommon.Utilities;

namespace KewlBot.Modules
{
	[HelpInfo("invite", "For bringing visitors in temporarily.", HelpPriorities.Invite)]
	public class InviteModule : KewlBotModule
	{
		[SlashCommand("invite", "Generate a temporary invite to the server.", false, RunMode.Async)]
		public async Task Invite()
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					var channel = contextInfo.guild?.TextChannels?.FirstOrDefault(x => x.Name.Equals("all", StringComparison.InvariantCultureIgnoreCase));
					if (channel == null)
					{
						ConsoleUtil.WriteLine("Couldn't find 'all' channel to create invite.", ConsoleColor.Red);
						await SendResponse(false, contextInfo.mention + " I think I need a crafting table or something to do that. Ask the admin to make me one.");
						return;
					}

					await DeferResponse(false);

					var invite = await channel.CreateInviteAsync(86400, 1, true, true);
					if (invite == null)
					{
						ConsoleUtil.WriteLine("Can't create invite.", ConsoleColor.Red);
						await SetDeferredResponse(contextInfo.mention + " Really not feelin' up to it right now, sorry.");
						return;
					}

					ConsoleUtil.WriteLine("Creating invite for user '" + contextInfo.username + "'.");
					await SetDeferredResponse(contextInfo.mention
						+ " Here's your link! It's good for one use within the next 24 hours.\n"
						+ "`" + invite.Url + "`\n"
						+ "\n"
						+ "Don't visit the link yourself! Copy it and send it to your guest!"
						);

					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'invite' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}