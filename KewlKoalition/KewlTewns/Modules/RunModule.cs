using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Core;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	[HelpInfo("run", "For executing custom commands crafted by the bot admin.", HelpPriorities.Run)]
	public class RunModule : KewlTewnsModule
	{
		public static readonly string CommandsPath = "Commands";

		public static IEnumerable<string> GetCommands(SocketGuild? guild)
		{
			if (guild == null)
				return [];

			return Program.Bot.config.GetGuildConfig(guild.Id).customCommands.Select(x => x.name);
		}

		[SlashCommand("run", "Execute the custom command with the specified name.", false, RunMode.Async)]
		public async Task Run
			(
				[Summary("command", "The name of the command to run.")]
				string command
			)
		{
			using (responseHandler)
			{
				await Run(responseHandler, ParsedContextInfo.ParseContext(Context), command);
			}
		}

		public static async Task<bool> Run(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? commandName, bool includeResponse = true)
		{
			try
			{
				if (!await responseHandler.PerformStandardContextResponse(contextInfo))
					return false;

				if (contextInfo.guild is not SocketGuild guild)
					return false;

				var guildConfig = Program.Bot.config.GetGuildConfig(guild.Id);
				if (string.IsNullOrEmpty(commandName) || !guildConfig.TryGetCustomCommand(commandName, out var command))
				{
					ConsoleUtil.WriteLine("Unrecognized command name '" + commandName + "'.");
					await responseHandler.SendResponse(false, contextInfo.mention + "'" + commandName + "' is not a recognized command name. Use **/commands** for a list of registered command names.");
					return false;
				}

				if (includeResponse)
					await responseHandler.SendResponse(true, contextInfo.mention + "Executing command '" + commandName + "'...");

				foreach (var line in command.body)
					await RunLine(responseHandler, contextInfo, line);
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while running custom command '" + commandName + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}

			return true;
		}
		public static string? GetDescription(SocketGuild? guild, string commandName)
		{
			try
			{
				if (guild == null)
					return null;

				var guildConfig = Program.Bot.config.GetGuildConfig(guild.Id);
				return guildConfig.TryGetCustomCommand(commandName, out var command) ? command.description : null;
			}
			catch { return null; }
		}
		static async Task RunLine(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? line)
		{
			if (string.IsNullOrEmpty(line))
				return;

			string? head;
			string? body;
			var index = line.IndexOf(' ');
			if (index >= 0)
			{
				head = line[..index];
				body = line[(index + 1)..];
			}
			else
			{
				head = line;
				body = null;
			}
			
			ConsoleUtil.WriteLine("Running line '" + line + "'");

			bool result = true;
			switch (head.ToLowerInvariant())
			{
				case "run":
					result = await Run(responseHandler, contextInfo, body, false);
					break;
				default:
					DoRunLine(responseHandler, contextInfo, line.Split());
					break;
			}

			if (!result)
			{
				ConsoleUtil.WriteLine("Error in command at line:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(line, ConsoleColor.Red);
			}
		}
		static async void DoRunLine(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string[] args)
		{
			if (args.Length <= 0 || string.IsNullOrEmpty(args[0]))
				return;

			bool result = false;
			switch (args[0].ToLowerInvariant())
			{
				case "play":
					{
						if (args.Length <= 2 || string.IsNullOrEmpty(args[1]) || string.IsNullOrEmpty(args[2]))
							break;

						switch (args[1].ToLowerInvariant())
						{
							case "song":
								{
									result = await PlayModule.Song(responseHandler, contextInfo, YoutubeUtil.URLToSongID(args[2]), GetBoolArg(args, 3), false);
								}
								break;
							case "list":
								{
									result = await PlayModule.List(responseHandler, contextInfo, YoutubeUtil.URLToListID(args[2]), GetBoolArg(args, 3), false);
								}
								break;
							case "sheet":
								{
									result = await PlayModule.Sheet(responseHandler, contextInfo, SheetsService.URLToSheetID(args[2]), GetModeArg(args, 3), false);
								}
								break;
							default:
								break;
						}
					}
					break;
				case "sequence":
					{
						if (args.Length <= 1 || string.IsNullOrEmpty(args[1]))
							break;

						switch (args[1].ToLowerInvariant())
						{
							case "loop":
								{
									if (args.Length <= 2 || string.IsNullOrEmpty(args[2]))
										break;

									result = await SequenceModule.Loop(responseHandler, contextInfo, GetBoolArg(args, 2), false);
								}
								break;
							case "personalize":
								{
									if (args.Length <= 2 || string.IsNullOrEmpty(args[2]))
										break;

									result = await SequenceModule.Personalize(responseHandler, contextInfo, GetBoolArg(args, 2), false);
								}
								break;
							case "reshuffle":
								{
									result = await SequenceModule.Reshuffle(responseHandler, contextInfo, false);
								}
								break;
						}
					}
					break;
			}

			if (!result)
			{
				ConsoleUtil.WriteLine("Error in command at line:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(string.Join(" ", args), ConsoleColor.Red);
			}
		}

		static bool GetBoolArg(string[] args, int index)
		{
			if (args == null || args.Length <= index)
				return false;

			return args[index].Equals("true", StringComparison.InvariantCultureIgnoreCase);
		}
		static PlayModule.Mode GetModeArg(string[] args, int index)
		{
			if (args == null || args.Length <= index)
				return PlayModule.Mode.Unspecified;

			try
			{
				return EnumUtil.GetValues<PlayModule.Mode>().First(x => x.ToString().Equals(args[index], StringComparison.InvariantCultureIgnoreCase));
			}
			catch
			{
				return PlayModule.Mode.Unspecified;
			}
		}
	}
}