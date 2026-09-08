using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KewlKommon.Context;
using KewlKommon.Extensions;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlBot.Core;
using KewlBot.Extensions;
using KewlBot.Utilities;

namespace KewlBot.Modules
{
	[HelpInfo("arrest", "For keeping users in line. (Admins only)", HelpPriorities.Arrest)]
	public class ArrestModule : KewlBotModule
	{
		static bool HasJurisdiction(SocketRole role)
		{
			if (role.IsEveryone)
				return false;
			if (role.IsManaged)
				return false;

			var botRole = role.Guild.GetSpecialRole(KewlUtil.BotRoleKey);
			if (botRole != null && role.Position > botRole.Position)
				return false;

			return true;
		}

		[SlashCommand("arrest", "Force a user into jail. (Admins only)", false, RunMode.Async)]
		public async Task Arrest
			(
				[Summary("suspect", "The user to arrest.")]
				SocketGuildUser suspect
			)
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context, Requirement.Admin);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					if (suspect == null)
					{
						ConsoleUtil.WriteLine("Couldn't find suspect.");
						await SendResponse(false, contextInfo.mention + " You can't fool me - I know there's no _real_ '" + suspect?.DisplayName + "'.");
						return;
					}
					else
					{
						ConsoleUtil.WriteLine("Found suspect '" + suspect.Username + "'!");
					}

					if (suspect.IsAdmin())
					{
						ConsoleUtil.WriteLine("Can't arrest server admin '" + suspect.Username + "'.");
						await SendResponse(false, contextInfo.mention + " What do I look like? A _god_?!");
						return;
					}
					if (suspect.IsBot())
					{
						ConsoleUtil.WriteLine("Can't arrest bot '" + suspect.Username + "'.");
						await SendResponse(false, contextInfo.mention + " We bots stick up for each other.");
						return;
					}
					if (suspect.IsConvict())
					{
						ConsoleUtil.WriteLine("Can't arrest an existing convict.");
						await SendResponse(false, contextInfo.mention + " Suspect '" + suspect.DisplayName + "' is already in custody.");
						return;
					}

					await DoArrest(contextInfo, suspect);
					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while arresting user '" + suspect + "':", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}

		async Task DoArrest(ParsedContextInfo contextInfo, SocketGuildUser user)
		{
			var userID = DiscordUtil.ExtractUserID(user.Mention);
			await SendResponse(true, contextInfo.mention + " Arresting '" + user.Mention + "'.");

			if (contextInfo.guild?.GetSpecialRole(BotUtil.ConvictRoleKey) is not SocketRole role)
				return;

			ConsoleUtil.WriteLine("Writing roles to file.");
			var roles = new List<IRole>();
			var filename = Path.Combine("Inmates", "Inmate " + userID + ".txt");
			if (Path.GetDirectoryName(filename) is string directory)
				Directory.CreateDirectory(directory);
			using (var outStream = new FileStream(filename, FileMode.OpenOrCreate))
			using (var writer = new StreamWriter(outStream))
			{
				foreach (var r in user.Roles.Where(HasJurisdiction))
				{
					ConsoleUtil.WriteLine("Role: " + r.Name);
					roles.Add(r);
					writer.WriteLine(r.Id);
				}
			}

			ConsoleUtil.WriteLine("Removing active roles.");
			await user.RemoveRolesAsync(roles);

			ConsoleUtil.WriteLine("Adding convict role.");
			await user.AddRoleAsync(role);

			ConsoleUtil.WriteLine("Checking connected voice status.");
			if (user.VoiceChannel != null)
			{
				if (!Program.Config.GetGuildConfigBase(Context.Guild.Id).TryGetSpecialChannel(BotUtil.JailChannelKey, out var channel))
					ConsoleUtil.WriteLine("Jail channel not found - disconnecting from voice.");
				else
					ConsoleUtil.WriteLine("Moving to jail.");

				await user.ModifyAsync(x => x.Channel = channel as SocketVoiceChannel);
			}

			ConsoleUtil.WriteLine("Finished with arrest.");
			return;
		}
	}
}