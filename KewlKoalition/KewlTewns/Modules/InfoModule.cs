using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KewlKommon.Components;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Extensions;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	[HelpInfoGroup("info", "For displaying playback and environment information.", HelpPriorities.Info)]
	public class InfoModule : KewlTewnsModule, IRegisteredListener
	{
		const int MaxHistoryLength = 10;
		const int MaxEmbedsCount = 25;

		const string EmptyCommandName = "None";
		const string CommandMenuName = "Command";
		const string RunButtonName = "Run";

		const string EmptyDescription = "[No Description]";

		static Dictionary<SocketGuild, Queue<string>> SongsPlayed = [];
		static Dictionary<ulong, string> SelectedCommands = [];

		public static void AddListeners()
		{
			EventUtil.AddListener<PlaybackController.PlayedSongEvent>(OnPlayedSong);
			EventUtil.AddListener<ButtonClickedEvent>(OnButtonClicked);
			EventUtil.AddListener<MenuSelectedEvent>(OnMenuSelected);
		}
		public static void RemoveListeners()
		{
			EventUtil.RemoveListener<PlaybackController.PlayedSongEvent>(OnPlayedSong);
			EventUtil.RemoveListener<ButtonClickedEvent>(OnButtonClicked);
			EventUtil.RemoveListener<MenuSelectedEvent>(OnMenuSelected);
		}
		static void OnPlayedSong(PlaybackController.PlayedSongEvent e)
		{
			if (e.guild == null)
				return;

			if (!SongsPlayed.TryGetValue(e.guild, out var history))
			{
				history = new Queue<string>();
				SongsPlayed[e.guild] = history;
			}
			if (history.Count > 0 && history.Last() == e.id)
				return;

			history.Enqueue(e.id);
			while (history.Count > MaxHistoryLength)
				history.Dequeue();
		}

		static async void OnButtonClicked(ButtonClickedEvent e)
		{
			var componentName = e.contextInfo.context.Data.CustomId.ExtractComponentName();

			switch (componentName)
			{
				case RunButtonName:
					break;
				default:
					return;
			}

			using (var responseHandler = e.contextInfo.context.GetResponseHandler())
			{
				var mention = e.contextInfo.user == null ? "" : e.contextInfo.user.Mention + " ";

				switch (componentName)
				{
					case RunButtonName:
						if (!SelectedCommands.TryGetValue(e.contextInfo.context.Message.Id, out var command))
							await responseHandler.SendResponse(false, mention + "No command selected.");
						else
							await RunModule.Run(responseHandler, e.contextInfo, command);
						break;
				}
			}
		}
		static async void OnMenuSelected(MenuSelectedEvent e)
		{
			using (var responseHandler = e.contextInfo.context.GetResponseHandler())
			{
				switch (e.contextInfo.context.Data.CustomId.ExtractComponentName())
				{
					case CommandMenuName:
						var messageId = e.contextInfo.context.Message.Id;

						if (e.contextInfo.context.Data.Values == null || e.contextInfo.context.Data.Values.Count < 1)
						{
							SelectedCommands.Remove(messageId);
							break;
						}

						var command = e.contextInfo.context.Data.Values.FirstOrDefault();
						if (string.IsNullOrEmpty(command))
						{
							SelectedCommands.Remove(messageId);
							break;
						}

						SelectedCommands[messageId] = command;
						break;
				}
				await responseHandler.SendAcknowledgement();
			}
		}

		[SlashCommand("controller", "Display a message with playback info and controls.", false, RunMode.Async)]
		public async Task Controller()
		{
			using (responseHandler)
			{
				try
				{
					var controller = DisplayController.Get(Context.Guild);

					ConsoleUtil.WriteLine("Displaying controller.");
					await SendResponse(true, Context.User.Mention + " Creating controller...");

					if (controller != null)
						await controller.CreateDisplay(Context.Channel as SocketTextChannel);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'queue' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
		[SlashCommand("queue", "Display the next few tracks in queue.", false, RunMode.Async)]
		public async Task Queue()
		{
			using (responseHandler)
			{
				try
				{
					var controller = PlaybackController.Get(Context.Guild);
					var sheets = SheetsService.Get(Context.Guild);

					ConsoleUtil.WriteLine("Displaying queue.");

					if (controller == null || !controller.isPlaying && !controller.GetQueuedTracks(1).Any())
					{
						await SendResponse(false, Context.User.Mention + " It doesn't seem like anything is playing...");
						return;
					}

					await DeferResponse(true);

					var embed = new EmbedBuilder
					{
						Title = "Now Playing",
						Description = null,
					};

					int count = 9;
					if (controller.isPlaying && sheets != null)
						embed.AddField(sheets.GetDisplayInfo(controller.currentTrack), YoutubeUtil.SongIDToURL(controller.currentTrack));
					else
						count = 10;

					bool comingUp = true;
					foreach (var track in controller.GetQueuedTracks(count))
					{
						if (comingUp)
						{
							comingUp = false;
						}

						embed.AddField(sheets.GetDisplayInfo(track), YoutubeUtil.SongIDToURL(track));
					}

					float listSize = embed.Fields.Count;
					float playingSize = controller.GetQueueLength(false);
					float totalSize = controller.GetQueueLength(true);

					await SetDeferredResponse(embed: embed
						.WithDescription("Next " + listSize + " of " + playingSize + " playing tracks (" + totalSize + " total):")
						.WithColor(KewlProgram.Bot.themeColor)
						.Build());
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'queue' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
		[SlashCommand("previous", "Display the previous few tracks that were played.", false, RunMode.Async)]
		public async Task Previous()
		{
			using (responseHandler)
			{
				try
				{
					var sheets = SheetsService.Get(Context.Guild);

					ConsoleUtil.WriteLine("Displaying previously played songs.");

					if (!SongsPlayed.TryGetValue(Context.Guild, out var history) || history.Count <= 0)
					{
						await SendResponse(false, Context.User.Mention + " It looks like nothing's been played...");
						return;
					}

					await DeferResponse(true);

					var embed = new EmbedBuilder
					{
						Title = "Playback History",
						Description = null,
					};
					foreach (var song in history.Reverse())
					{
						embed.AddField(sheets.GetDisplayInfo(song), YoutubeUtil.SongIDToURL(song));
					}

					await SetDeferredResponse(embed: embed.WithColor(KewlProgram.Bot.themeColor).Build());
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'previous' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
		[SlashCommand("commands", "Display a list of commands available to use with **/run**.", false, RunMode.Async)]
		public async Task Commands()
		{
			using (responseHandler)
			{
				var contextInfo = ParsedContextInfo.ParseContext(Context, Requirement.None);
				if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Response))
					return;

				await Commands(responseHandler, contextInfo);
			}
		}
		public static async Task Commands(IResponseHandler responseHandler, ParsedContextInfo contextInfo)
		{
			using (responseHandler)
			{
				try
				{
					string[]? commands = null;

					try
					{
						commands = [.. RunModule.GetCommands(contextInfo.guild)];
					}
					catch { }

					if (commands == null || commands.Length <= 0)
					{
						await responseHandler.SendResponse(false, contextInfo.mention + "Seems there are no custom commands available.");
					}
					else if (commands.Length <= MaxEmbedsCount)
					{
						var embed = new EmbedBuilder()
						{
							Title = "Available Custom Commands",
							Description = "Use **/run** with the command's name to execute.",
						};

						var menu = new SelectMenuBuilder()
							.WithCustomId(CommandMenuName.MakeComponentId())
							.AddOption(EmptyCommandName, EmptyCommandName);
						foreach (var command in commands)
						{
							var description = RunModule.GetDescription(contextInfo.guild, command);
							if (string.IsNullOrEmpty(description))
								description = EmptyDescription;
							embed.AddField(command, description);
							menu.AddOption(command, command, description);
							menu.MaxValues = 1;
						}

						var components = new ComponentBuilder();
						components.AddRow
							(
								new ActionRowBuilder
								{
									Components = [menu]
								}
							);
						components.AddRow
							(
								new ActionRowBuilder().WithButton(RunButtonName.Nicify(), RunButtonName.MakeComponentId(), ButtonStyle.Primary)
							);

						await responseHandler.SendResponse(false, embed: embed.Build(), components: components.Build());
					}
					else
					{
						await responseHandler.SendResponse(false, "Available custom commands:\n" + string.Join("\n", commands));
					}
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'commands' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				}
			}
		}
	}
}