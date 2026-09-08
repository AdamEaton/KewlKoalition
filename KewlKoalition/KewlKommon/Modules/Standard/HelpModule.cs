using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Interaction;
using KewlKommon.Utilities;

namespace KewlKommon.Modules.Standard
{
	[HelpInfo("help", "For getting information on how to use the different bot commands.", HelpPriorities.Help)]
	public class HelpModule : KewlModule
	{
		static IHelpInfoAttribute? GetHelpInfo(Type type)
		{
			return type.GetCustomAttributes<Attribute>()
				.OfType<IHelpInfoAttribute>()
				.FirstOrDefault();
		}

		[SlashCommand("help", "Display general information or details about built-in bot commands.", false, RunMode.Async)]
		public async Task Help
			(
				[Summary("command", "The command to learn details about (omit for general info).")]
				string? command = null
			)
		{
			using (responseHandler)
			{
				var contextInfo = ParsedContextInfo.ParseContext(Context, Requirement.None);
				if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Response))
					return;

				if (string.IsNullOrWhiteSpace(command))
					await Help(responseHandler, contextInfo);
				else
					await Help(responseHandler, contextInfo, command);
			}
		}

		public static async Task Help(IResponseHandler responseHandler, ParsedContextInfo contextInfo)
		{
			try
			{
				var embed = new EmbedBuilder
				{
					Title = "Command List",
					Description = "Use '**/help `commandName`**' to learn more.",
				};

				foreach (var info in AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(x => x
						.GetTypes()
						.Where(x => x.IsSubclassOf(typeof(KewlModule)))
						.Select(GetHelpInfo)
						.OfType<IHelpInfoAttribute>())
					.OrderByDescending(x => x.Priority)
					.ThenBy(x => x.Name))
				{
					embed.AddField(info.Name, info.Description);
				}

				await responseHandler.SendResponse(false, embed: embed.WithColor(KewlProgram.Bot.themeColor).Build());
				return;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'help' command:" + ex, ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return;
			}
		}
		public static async Task Help(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string command)
		{
			try
			{
				if (string.IsNullOrEmpty(command))
					return;

				var embed = new EmbedBuilder
				{
					Title = command[0].ToString().ToUpper() + command[1..] + " Command",
				};

				bool success = false;

				foreach (var module in AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(x => x
						.GetTypes()
						.Where(x => x.IsSubclassOf(typeof(KewlModule)))))
				{
					var moduleInfo = GetHelpInfo(module);
					if (moduleInfo == null)
						continue;

					if (!moduleInfo.Name.Equals(command, StringComparison.InvariantCultureIgnoreCase))
						continue;

					embed.WithDescription(moduleInfo.Description);

					foreach (var c in module.GetMethods())
					{
						var commandAtt = c.GetCustomAttribute<SlashCommandAttribute>();
						if (commandAtt == null)
							continue;

						if (commandAtt.Description.Contains("error", StringComparison.InvariantCultureIgnoreCase))
							continue;

						success = true;

						var title = commandAtt.Name.ToLowerInvariant();
						if (moduleInfo is HelpInfoGroupAttribute group)
							title = group.Name.ToLowerInvariant() + " " + title;
						var body = commandAtt.Description;

						foreach (var param in c.GetParameters())
						{
							title += " `" + param.Name + "`";

							var paramAtt = param.GetCustomAttribute<SummaryAttribute>();
							if (paramAtt == null)
								continue;

							body += "\n`" + paramAtt.Name + "`: " + paramAtt.Description;
						}
						title = "**" + title + "**";
						embed.AddField(title, body);
					}
				}

				if (success)
					await responseHandler.SendResponse(false, embed: embed.WithColor(KewlProgram.Bot.themeColor).Build());
				else
					await responseHandler.SendResponse(false, "'" + command + "' isn't a recognized built-in command. Try using **/help** on its own for a list of commands.");
				return;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'help' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return;
			}
		}
	}
}