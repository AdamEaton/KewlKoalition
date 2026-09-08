using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KewlKommon.Context;
using KewlKommon.Extensions;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlBot.Extensions;
using KewlBot.Utilities;

namespace KewlBot.Modules
{
	[HelpInfo("release", "For the rehabilitated. (Admins only)", HelpPriorities.Release)]
	public class ReleaseModule : KewlBotModule
	{
		[SlashCommand("release", "Reinstate a user's access to non-jail channels. (Admins only)", false, RunMode.Async)]
		public async Task Release
			(
				[Summary("inmate", "The user to release.")]
				SocketGuildUser inmate
			)
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context, Requirement.Admin);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					if (inmate == null)
					{
						ConsoleUtil.WriteLine("Couldn't find inmate.");
						await SendResponse(false, contextInfo.mention + " You can't fool me - I know there's no _real_ '" + inmate + "'.");
						return;
					}
					else
					{
						ConsoleUtil.WriteLine("Found inmate '" + inmate.DisplayName + "'!");
					}

					if (inmate.IsAdmin())
					{
						ConsoleUtil.WriteLine("Can't release server admin '" + inmate.DisplayName + "'.");
						await SendResponse(false, contextInfo.mention + " What do I look like? A _god_?!");
						return;
					}
					if (inmate.IsBot())
					{
						ConsoleUtil.WriteLine("Can't release bot '" + inmate.DisplayName + "'.");
						await SendResponse(false, contextInfo.mention + " We bots stick up for each other.");
						return;
					}
					if (!inmate.IsConvict())
					{
						ConsoleUtil.WriteLine("Can't release a free user.");
						await SendResponse(false, contextInfo.mention + " User '" + inmate.DisplayName + "' is not in custody.");
						return;
					}

					await DoRelease(contextInfo, inmate);
					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while releasing user '" + inmate.Username + "':", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}

		async Task DoRelease(ParsedContextInfo contextInfo, SocketGuildUser user)
		{
			var userID = DiscordUtil.ExtractUserID(user.Mention);
			string filename = Path.Combine("Inmates", "Inmate " + userID + ".txt");
			if (!File.Exists(filename))
			{
				ConsoleUtil.WriteLine("Can't find record for inmate '" + user.Username + "'.", ConsoleColor.Red);
				await SendResponse(false, contextInfo.mention + " I have no record of that user's arrest.");
				return;
			}

			await SendResponse(true, contextInfo.mention + " Releasing '" + user.Mention + "'.");

			ConsoleUtil.WriteLine("Reading roles from file.");
			var roles = new List<IRole>();
			using (var inStream = new FileStream(filename, FileMode.Open))
			using (var reader = new StreamReader(inStream))
			{
				string? line;
				do
				{
					line = reader.ReadLine();

					foreach (var role in user.Guild.Roles)
					{
						if (role.Id.ToString() == line)
						{
							ConsoleUtil.WriteLine("Role: " + role.Name);
							roles.Add(role);
							break;
						}
					}
				} while (!string.IsNullOrEmpty(line));
			}
			File.Delete(filename);

			ConsoleUtil.WriteLine("Adding previous roles.");
			await user.AddRolesAsync(roles);

			ConsoleUtil.WriteLine("Removing convict role.");
			await user.RemoveRoleAsync(user.Guild.GetSpecialRole(BotUtil.ConvictRoleKey));

			ConsoleUtil.WriteLine("Finished with release.");
			return;
		}
	}
}