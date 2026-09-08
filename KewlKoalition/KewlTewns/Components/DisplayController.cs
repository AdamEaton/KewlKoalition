using System;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using KewlKommon.Components;
using KewlKommon.Core;
using KewlKommon.Extensions;
using KewlKommon.Interaction;
using KewlKommon.Modules.Standard;
using KewlKommon.Utilities;
using KewlTewns.Modules;
using KewlTewns.Utilities;

namespace KewlTewns.Components
{
	public class DisplayController : PerGuildMonitor<DisplayController>, IRegisteredListener
	{
		const string PlayingPrefix = "\U0001F3B5 : ";	// Musical note
		const string PausedPrefix = "\u23F8 : ";		// Pause button
		const string EmptyPrefix = "\u274C : ";			// Cross mark

		const string PausedStatus = "[Paused]";
		const string EmptyStatus = TewnsUtil.EmptyDisplayString;

		const string ResumeButtonName = "Resume";
		const string PauseButtonName = "Pause";
		const string SkipButtonName = "Skip";
		const string RefreshButtonName = "Refresh";
		const string LeaveButtonName = "Leave";

		const string LoopOnName = "LoopingOn";
		const string LoopOffName = "LoopingOff";
		const string PersonalizeOnName = "PersonalizationOn";
		const string PersonalizeOffName = "PersonalizationOff";

		const string EndorseButtonName = "Endorse";
		const string VetoButtonName = "Veto";
		const string RenounceButtonName = "Renounce";
		const string AllowButtonName = "Allow";
		const string FlagButtonName = "Flag";

		const string HelpButtonName = "Help";
		const string CommandButtonName = "Commands";

		bool isDirty = false;
		bool queuePaused = false;

		RestUserMessage? display = null;
		string? currentName = null;
		string? currentUrl = null;

		public static void AddListeners()
		{
			EventUtil.AddListener<PlayedSongDataEvent>(OnPlayedSongData);
			EventUtil.AddListener<PlayedUnknownSongEvent>(OnPlayedUnknownSong);
			EventUtil.AddListener<PlaybackController.PausedQueueEvent>(OnPausedQueue);
			EventUtil.AddListener<PlaybackController.ClearedQueueEvent>(OnClearedQueue);
			EventUtil.AddListener<PlaybackController.PlaybackOptionsUpdatedEvent>(OnPlaybackOptionsUpdated);
			EventUtil.AddListener<ButtonClickedEvent>(OnButtonClicked);
		}
		public static void RemoveListeners()
		{
			EventUtil.RemoveListener<PlayedSongDataEvent>(OnPlayedSongData);
			EventUtil.RemoveListener<PlayedUnknownSongEvent>(OnPlayedUnknownSong);
			EventUtil.RemoveListener<PlaybackController.PausedQueueEvent>(OnPausedQueue);
			EventUtil.RemoveListener<PlaybackController.ClearedQueueEvent>(OnClearedQueue);
			EventUtil.RemoveListener<PlaybackController.PlaybackOptionsUpdatedEvent>(OnPlaybackOptionsUpdated);
			EventUtil.RemoveListener<ButtonClickedEvent>(OnButtonClicked);
		}
		static void OnPlayedSongData(PlayedSongDataEvent e)
		{
			if (e.guild == null)
				return;

			var instance = Get(e.guild);
			if (instance == null)
				return;

			if (e.songData == null)
				instance.SetStatus(null, null);
			else
				instance.SetStatus(e.songData.DisplayInfo, e.songData.Url);
		}
		static void OnPlayedUnknownSong(PlayedUnknownSongEvent e)
		{
			if (e.guild == null)
				return;

			var instance = Get(e.guild);
			if (instance == null)
				return;

			instance.SetStatus(YoutubeUtil.GetSongTitle(e.id), YoutubeUtil.SongIDToURL(e.id));
		}
		static void OnPausedQueue(PlaybackController.PausedQueueEvent e)
		{
			if (e.guild == null)
				return;

			var instance = Get(e.guild);
			if (instance == null)
				return;

			instance.queuePaused = e.paused;
			instance.isDirty = true;
		}
		static void OnClearedQueue(PlaybackController.ClearedQueueEvent e)
		{
			if (e.guild == null)
				return;

			var instance = Get(e.guild);
			if (instance == null)
				return;

			instance.SetStatus(null, null);
		}
		static void OnPlaybackOptionsUpdated(PlaybackController.PlaybackOptionsUpdatedEvent e)
		{
			if (e.guild == null)
				return;

			var instance = Get(e.guild);
			if (instance == null)
				return;

			instance.isDirty = true;
		}
		static void OnButtonClicked(ButtonClickedEvent e)
		{
			RespondToButtonClick(e);
		}

		static async void RespondToButtonClick(ButtonClickedEvent e)
		{
			var componentName = e.contextInfo.context.Data.CustomId.ExtractComponentName();

			switch (componentName)
			{
				case ResumeButtonName:
				case PauseButtonName:
				case SkipButtonName:
				case RefreshButtonName:
				case LeaveButtonName:
				case LoopOnName:
				case LoopOffName:
				case PersonalizeOnName:
				case PersonalizeOffName:
				case EndorseButtonName:
				case VetoButtonName:
				case RenounceButtonName:
				case AllowButtonName:
				case FlagButtonName:
				case HelpButtonName:
				case CommandButtonName:
					break;
				default:
					return;
			}

			using (var responseHandler = e.contextInfo.context.GetResponseHandler())
			{
				if (!await responseHandler.PerformStandardContextResponse(e.contextInfo))
					return;

				switch (componentName)
				{
					case ResumeButtonName:
						await ResumeModule.Resume(responseHandler, e.contextInfo, false);
						break;
					case PauseButtonName:
						await PauseModule.Pause(responseHandler, e.contextInfo, false);
						break;
					case SkipButtonName:
						await SkipModule.Skip(responseHandler, e.contextInfo, false);
						break;
					case RefreshButtonName:
						await RefreshModule.Refresh(responseHandler, e.contextInfo, false);
						break;
					case LeaveButtonName:
						await LeaveModule.Leave(responseHandler, e.contextInfo, false);
						break;

					case LoopOnName:
						await SequenceModule.Loop(responseHandler, e.contextInfo, false, false);
						break;
					case LoopOffName:
						await SequenceModule.Loop(responseHandler, e.contextInfo, true, false);
						break;
					case PersonalizeOnName:
						await SequenceModule.Personalize(responseHandler, e.contextInfo, false, false);
						break;
					case PersonalizeOffName:
						await SequenceModule.Personalize(responseHandler, e.contextInfo, true, false);
						break;

					case EndorseButtonName:
						await SongModule.Endorse(responseHandler, e.contextInfo, null, true);
						break;
					case RenounceButtonName:
						await SongModule.Renounce(responseHandler, e.contextInfo, null, false);
						break;
					case VetoButtonName:
						await SongModule.Veto(responseHandler, e.contextInfo, null, false);
						break;
					case AllowButtonName:
						await SongModule.Allow(responseHandler, e.contextInfo, null, false);
						break;
					case FlagButtonName:
						await SongModule.Flag(responseHandler, e.contextInfo, null, false);
						break;

					case HelpButtonName:
						await HelpModule.Help(responseHandler, e.contextInfo);
						break;
					case CommandButtonName:
						await InfoModule.Commands(responseHandler, e.contextInfo);
						break;
				}
			}
		}

		public async Task CreateDisplay(SocketTextChannel? channel)
		{
			if (channel == null)
				return;

			await RemoveDisplay();
			display = await channel.SendMessageAsync(embed: GetStatusEmbed(), components: GetStatusComponents());
		}
		public async Task RemoveDisplay()
		{
			if (display == null)
				return;

			try
			{
				await display.DeleteAsync();
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while removing DisplayController:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
			}
		}
		public async Task UpdateDisplay()
		{
			if (display is null)
				return;

			await display.ModifyAsync(x =>
			{
				x.Embed = GetStatusEmbed();
				x.Components = GetStatusComponents();
			});
			await Task.Delay(1000);
		}

		public override async void Monitor()
		{
			using (new KewlProgram.ShutDownLock(KewlProgram.Bot))
			{
				try
				{
					while (!KewlProgram.Bot.shuttingDown)
					{
						try
						{
							await Task.Delay(100);

							if (!isDirty)
								continue;
							if (display == null)
								continue;

							isDirty = false;
							await UpdateDisplay();
						}
						catch (Exception ex)
						{
							ConsoleUtil.WriteLine("Error while updating DisplayController:", ConsoleColor.Red);
							ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
						}
					}
				}
				finally
				{
					await RemoveDisplay();
				}
			}
		}

		public void SetStatus(string? name, string? url)
		{
			currentName = name;
			currentUrl = url;
			isDirty = true;
		}

		Embed GetStatusEmbed()
		{
			var embed = new EmbedBuilder
			{
				Title = "Now Playing",
				Description = null,
			};

			if (queuePaused)
			{
				embed.Title = embed.Title;
				embed.AddField(PausedStatus, PausedPrefix + "Use `/resume` to resume playback.");
			}
			else if (string.IsNullOrEmpty(currentUrl))
			{
				embed.Title = embed.Title;
				embed.AddField(EmptyStatus, EmptyPrefix + "Use `/play` to queue some tewns.");
			}
			else if (string.IsNullOrEmpty(currentName))
			{
				var sheets = Get<SheetsService>();
				if (sheets != null)
				{
					embed.Title = embed.Title;
					embed.AddField(sheets.GetDisplayInfo(YoutubeUtil.URLToSongID(currentUrl)), PlayingPrefix + currentUrl);
				}
			}
			else
			{
				embed.Title = embed.Title;
				embed.AddField(currentName, PlayingPrefix + currentUrl);
			}

			return embed.WithColor(KewlProgram.Bot.themeColor).Build();
		}
		MessageComponent GetStatusComponents()
		{
			var controller = Get<PlaybackController>();
			var components = new ComponentBuilder();

			if (queuePaused)
			{
				AddRows(
					GetControlsRow(false, true, false, false, true),
					GetOptionsRow(),
					GetHelpRow());
			}
			else if (string.IsNullOrEmpty(currentUrl))
			{
				AddRows(
					GetOptionsRow(),
					GetHelpRow());
			}
			else
			{
				AddRows(
					GetControlsRow(true, false, true, true, true),
					GetOptionsRow(),
					GetSheetsRow(),
					GetHelpRow());
			}

			return components.Build();

			void AddRows(params ActionRowBuilder[] rows)
			{
				components.WithRows(rows);
			}
			ActionRowBuilder GetControlsRow(bool includePause, bool includeResume, bool includeSkip, bool includeRefresh, bool includeLeave)
			{
				var output = new ActionRowBuilder();
				if (includePause)
					output.WithButton(PauseButtonName.Nicify(), PauseButtonName.MakeComponentId(), ButtonStyle.Primary);
				if (includeResume)
					output.WithButton(ResumeButtonName.Nicify(), ResumeButtonName.MakeComponentId(), ButtonStyle.Success);
				if (includeSkip)
					output.WithButton(SkipButtonName.Nicify(), SkipButtonName.MakeComponentId(), ButtonStyle.Secondary);
				if (includeRefresh)
					output.WithButton(RefreshButtonName.Nicify(), RefreshButtonName.MakeComponentId(), ButtonStyle.Secondary);
				if (includeLeave)
					output.WithButton(LeaveButtonName.Nicify(), LeaveButtonName.MakeComponentId(), ButtonStyle.Danger);
				return output;
			}
			ActionRowBuilder GetOptionsRow()
			{
				string loopName = controller.LoopEnabled() ? LoopOnName : LoopOffName;
				string personalizeName = controller.PersonalizeEnabled() ? PersonalizeOnName : PersonalizeOffName;

				return new ActionRowBuilder()
					.WithButton(personalizeName.Nicify(), personalizeName.MakeComponentId(), GetToggleStyle(controller.PersonalizeEnabled()))
					.WithButton(loopName.Nicify(), loopName.MakeComponentId(), GetToggleStyle(controller.LoopEnabled()));
			}
			ActionRowBuilder GetSheetsRow()
			{
				return new ActionRowBuilder()
					.WithButton(EndorseButtonName.Nicify(), EndorseButtonName.MakeComponentId(), ButtonStyle.Success)
					.WithButton(RenounceButtonName.Nicify(), RenounceButtonName.MakeComponentId(), ButtonStyle.Secondary)
					.WithButton(VetoButtonName.Nicify(), VetoButtonName.MakeComponentId(), ButtonStyle.Danger)
					.WithButton(AllowButtonName.Nicify(), AllowButtonName.MakeComponentId(), ButtonStyle.Secondary)
					.WithButton(FlagButtonName.Nicify(), FlagButtonName.MakeComponentId(), ButtonStyle.Danger);
			}
			ActionRowBuilder GetHelpRow()
			{
				return new ActionRowBuilder()
					.WithButton(HelpButtonName.Nicify(), HelpButtonName.MakeComponentId(), ButtonStyle.Secondary)
					.WithButton(CommandButtonName.Nicify(), CommandButtonName.MakeComponentId(), ButtonStyle.Secondary);
			}
			ButtonStyle GetToggleStyle(bool isOn)
			{
				return isOn ? ButtonStyle.Primary : ButtonStyle.Secondary;
			}
		}
	}
}