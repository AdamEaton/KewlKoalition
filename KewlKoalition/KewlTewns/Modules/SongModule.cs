using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	[HelpInfoGroup("song", "For marking the current Google Sheets playlist song(s).")]
	public class SongModule : KewlTewnsModule
	{
		[SlashCommand("endorse", "Request that the bot play the specified (or current) song when you are present.", false, RunMode.Async)]
		public async Task Endorse
			(
				[Summary("url", "The YouTube URL of the song to endorse (omit to use current playing song).")]
				string? url = null
			)
		{
			using (responseHandler)
			{
				await Endorse(responseHandler, ParsedContextInfo.ParseContext(Context, Requirement.None), null, true);
			}
		}
		public static async Task Endorse(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? url, bool includeResponse)
		{
			using (responseHandler)
			{
				var info = await UpdateInfo("endorse", responseHandler, contextInfo, url, includeResponse, CanApply, DoApply);
				switch (info.errorCode)
				{
					case UpdateInfoResult.ErrorCode.None:
						ConsoleUtil.WriteLine("Song endorse process complete!");
						await responseHandler.SetResponse(includeResponse, contextInfo.mention + "You're now marked as a requester on this song!\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
					case UpdateInfoResult.ErrorCode.Denied:
						break;
					case UpdateInfoResult.ErrorCode.NotPlaying:
						ConsoleUtil.WriteLine("No track playing to endorse.");
						await responseHandler.SetResponse(false, contextInfo.mention + "I'm not playing anything for you to endorse.");
						break;
					case UpdateInfoResult.ErrorCode.NoSongData:
						ConsoleUtil.WriteLine("Song is not from a playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "This track isn't from an active Google Sheets playlist. Only songs from currently playing Google Sheets playlists can be endorsed.\n"
							+ url);
						break;
					case UpdateInfoResult.ErrorCode.NoUserData:
						ConsoleUtil.WriteLine("User '" + contextInfo.username + "' is not represented in any current playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "You can't endorse a song unless you are already listed as a requester or vetoer in an active Google Sheets playlist. Contact the sheet owner to be set up.");
						break;
					case UpdateInfoResult.ErrorCode.OperationRedundant:
						await responseHandler.SetResponse(false, contextInfo.mention + "You're already endorsing this song!\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
				}
			}

			bool CanApply(SongData songData)
			{
				return !songData.requesters.Any(x => contextInfo.username.Contains(x, StringComparison.InvariantCultureIgnoreCase));
			}
			void DoApply(SongData songData, string identifier)
			{
				songData.AddRequester(identifier);
			}
		}
		[SlashCommand("renounce", "Take back a request to play the specified (or current) song when you are present.", false, RunMode.Async)]
		public async Task Renounce
			(
				[Summary("url", "The YouTube URL of the song to renounce (omit to use current playing song).")]
				string? url = null
			)
		{
			using (responseHandler)
			{
				await Renounce(responseHandler, ParsedContextInfo.ParseContext(Context, Requirement.None), null, true);
			}
		}
		public static async Task Renounce(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? url, bool includeResponse)
		{
			using (responseHandler)
			{
				var info = await UpdateInfo("renounce", responseHandler, contextInfo, url, includeResponse, CanApply, DoApply);
				switch (info.errorCode)
				{
					case UpdateInfoResult.ErrorCode.None:
						ConsoleUtil.WriteLine("Song renounce process complete!");
						await responseHandler.SetResponse(includeResponse, contextInfo.mention + "You're no longer marked as a requester on this song.\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
					case UpdateInfoResult.ErrorCode.Denied:
						break;
					case UpdateInfoResult.ErrorCode.NotPlaying:
						ConsoleUtil.WriteLine("No track playing to endorse.");
						await responseHandler.SetResponse(false, contextInfo.mention + "I'm not playing anything for you to renounce.");
						break;
					case UpdateInfoResult.ErrorCode.NoSongData:
						ConsoleUtil.WriteLine("Song is not from a playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "This track isn't from an active Google Sheets playlist. Only songs from currently playing Google Sheets playlists can have endorsements.\n"
							+ url);
						break;
					case UpdateInfoResult.ErrorCode.NoUserData:
						ConsoleUtil.WriteLine("User '" + contextInfo.username + "' is not represented in any current playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "You can't have endorsed a song unless you are already listed as a requester or vetoer in an active Google Sheets playlist. Contact the sheet owner to be set up.");
						break;
					case UpdateInfoResult.ErrorCode.OperationRedundant:
						await responseHandler.SetResponse(false, contextInfo.mention + "You're not endorsing this song!\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
				}
			}

			bool CanApply(SongData songData)
			{
				return songData.requesters.Any(x => contextInfo.username.Contains(x, StringComparison.InvariantCultureIgnoreCase));
			}
			void DoApply(SongData songData, string identifier)
			{
				songData.RemoveRequester(identifier);
			}
		}
		[SlashCommand("veto", "Request that the bot avoid the specified (or current) song when you are present.", false, RunMode.Async)]
		public async Task Veto
			(
				[Summary("url", "The YouTube URL of the song to veto (omit to use current playing song).")]
				string? url = null
			)
		{
			using (responseHandler)
			{
				await Veto(responseHandler, ParsedContextInfo.ParseContext(Context, Requirement.None), null, true);
			}
		}
		public static async Task Veto(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? url, bool includeResponse)
		{
			using (responseHandler)
			{
				var info = await UpdateInfo("veto", responseHandler, contextInfo, url, includeResponse, CanApply, DoApply);
				switch (info.errorCode)
				{
					case UpdateInfoResult.ErrorCode.None:
						ConsoleUtil.WriteLine("Song veto process complete!");
						await responseHandler.SetResponse(includeResponse, contextInfo.mention + "You're now marked as a vetoer on this song!\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
					case UpdateInfoResult.ErrorCode.Denied:
						break;
					case UpdateInfoResult.ErrorCode.NotPlaying:
						ConsoleUtil.WriteLine("No track playing to veto.");
						await responseHandler.SetResponse(false, contextInfo.mention + "I'm not playing anything for you to veto.");
						break;
					case UpdateInfoResult.ErrorCode.NoSongData:
						ConsoleUtil.WriteLine("Song is not from a playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "This track isn't from an active Google Sheets playlist. Only songs from currently playing Google Sheets playlists can be vetoed.\n"
							+ url);
						break;
					case UpdateInfoResult.ErrorCode.NoUserData:
						ConsoleUtil.WriteLine("User '" + contextInfo.username + "' is not represented in any current playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "You can't veto a song unless you are already listed as a requester or vetoer in an active Google Sheets playlist. Contact the sheet owner to be set up.");
						break;
					case UpdateInfoResult.ErrorCode.OperationRedundant:
						await responseHandler.SetResponse(false, contextInfo.mention + "You're already vetoing this song!\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
				}
			}

			bool CanApply(SongData songData)
			{
				return !songData.vetoers.Any(x => contextInfo.username.Contains(x, StringComparison.InvariantCultureIgnoreCase));
			}
			void DoApply(SongData songData, string identifier)
			{
				songData.AddVetoer(identifier);
			}
		}
		[SlashCommand("allow", "Take back a request to avoid the specified (or current) song when you are present.", false, RunMode.Async)]
		public async Task Allow
			(
				[Summary("url", "The YouTube URL of the song to allow (omit to use current playing song).")]
				string? url = null
			)
		{
			using (responseHandler)
			{
				await Allow(responseHandler, ParsedContextInfo.ParseContext(Context, Requirement.None), null, true);
			}
		}
		public static async Task Allow(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? url, bool includeResponse)
		{
			using (responseHandler)
			{
				var info = await UpdateInfo("allow", responseHandler, contextInfo, url, includeResponse, CanApply, DoApply);
				switch (info.errorCode)
				{
					case UpdateInfoResult.ErrorCode.None:
						ConsoleUtil.WriteLine("Song allow process complete!");
						await responseHandler.SetResponse(includeResponse, contextInfo.mention + "You're no longer marked as a vetoer on this song.\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
					case UpdateInfoResult.ErrorCode.Denied:
						break;
					case UpdateInfoResult.ErrorCode.NotPlaying:
						ConsoleUtil.WriteLine("No track playing to allow.");
						await responseHandler.SetResponse(false, contextInfo.mention + "I'm not playing anything for you to allow.");
						break;
					case UpdateInfoResult.ErrorCode.NoSongData:
						ConsoleUtil.WriteLine("Song is not from a playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "This track isn't from an active Google Sheets playlist. Only songs from currently playing Google Sheets playlists can have vetoes.\n"
							+ url);
						break;
					case UpdateInfoResult.ErrorCode.NoUserData:
						ConsoleUtil.WriteLine("User '" + contextInfo.username + "' is not represented in any current playsheet.");
						await responseHandler.SetResponse(false, contextInfo.mention + "You can't have vetoed a song unless you are already listed as a requester or vetoer in an active Google Sheets playlist. Contact the sheet owner to be set up.");
						break;
					case UpdateInfoResult.ErrorCode.OperationRedundant:
						await responseHandler.SetResponse(false, contextInfo.mention + "You're not vetoing this song!\n"
							+ TewnsUtil.GetSongDisplayString(contextInfo.guild, info.id));
						break;
				}
			}

			bool CanApply(SongData songData)
			{
				return songData.vetoers.Any(x => contextInfo.username.Contains(x, StringComparison.InvariantCultureIgnoreCase));
			}
			void DoApply(SongData songData, string identifier)
			{
				songData.RemoveVetoer(identifier);
			}
		}
		[SlashCommand("flag", "Mark the specified (or current) song for review.", false, RunMode.Async)]
		public async Task Flag
			(
				[Summary("url", "The YouTube URL of the song to flag (omit to use current playing song).")]
				string? url = null
			)
		{
			using (responseHandler)
			{
				await Flag(responseHandler, ParsedContextInfo.ParseContext(Context, Requirement.None), null, true);
			}
		}
		public static async Task Flag(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? url, bool includeResponse)
		{
			using (responseHandler)
			{
				try
				{
					var controller = PlaybackController.Get(contextInfo.guild);

					if (!await responseHandler.PerformStandardContextResponse(contextInfo))
						return;

					string? id = null;
					if (string.IsNullOrWhiteSpace(url))
					{
						if (!controller.isPlaying)
						{
							ConsoleUtil.WriteLine("No track playing to flag.");
							await responseHandler.SendResponse(false, contextInfo.mention + "I'm not playing anything for you to flag.");
							return;
						}

						id = controller.currentTrack;
					}
					else
					{
						id = YoutubeUtil.URLToSongID(url);
					}

					var sheets = SheetsService.Get(contextInfo.guild);
					if (string.IsNullOrWhiteSpace(id) || !sheets.enqueuedSongs.TryGetValue(id, out var generalData))
					{
						ConsoleUtil.WriteLine("Song is not from a playsheet.");
						await responseHandler.SendResponse(false, contextInfo.mention + "This track isn't from an active Google Sheets playlist. Only songs from currently playing Google Sheets playlists can be flagged.\n"
							+ YoutubeUtil.SongIDToURL(id));
						return;
					}

					await responseHandler.DeferResponse(includeResponse);

					foreach (var sheetSpecificData in generalData)
						sheetSpecificData.Value.AddInfo(SongData.Info.Flagged);

					ConsoleUtil.WriteLine("Song with ID '" + id + "' flagged for review by user '" + contextInfo.username + "'");
					await responseHandler.SetDeferredResponse(contextInfo.mention + "Flagged this song for review.\n"
						+ TewnsUtil.GetSongDisplayString(contextInfo.guild, id));
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while flagging song:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}

		class UpdateInfoResult
		{
			public ErrorCode errorCode { get; private set; }
			public string? id { get; private set; }

			public UpdateInfoResult(ErrorCode errorCode, string? id)
			{
				this.errorCode = errorCode;
				this.id = id;
			}

			public enum ErrorCode
			{
				None,
				Denied,
				NotPlaying,
				NoSongData,
				NoUserData,
				OperationRedundant,
				Unknown,
			}
		}
		static async Task<UpdateInfoResult> UpdateInfo
			(
				string command,
				IResponseHandler responseHandler,
				ParsedContextInfo contextInfo,
				string? url,
				bool includeResponse,
				Predicate<SongData> canApply,
				Action<SongData, string> doApply
			)
		{
			try
			{
				var controller = PlaybackController.Get(contextInfo.guild);

				if (!await responseHandler.PerformStandardContextResponse(contextInfo))
					return new UpdateInfoResult(UpdateInfoResult.ErrorCode.Denied, null);

				string? id = null;
				if (string.IsNullOrWhiteSpace(url))
				{
					if (!controller.isPlaying)
						return new UpdateInfoResult(UpdateInfoResult.ErrorCode.NotPlaying, id);

					id = controller.currentTrack;
				}
				else
				{
					id = YoutubeUtil.URLToSongID(url);
				}

				var sheets = SheetsService.Get(contextInfo.guild);

				if (string.IsNullOrWhiteSpace(id) || !sheets.enqueuedSongs.TryGetValue(id, out var generalData))
					return new UpdateInfoResult(UpdateInfoResult.ErrorCode.NoSongData, id);

				await responseHandler.DeferResponse(includeResponse);

				var identifier = sheets.enqueuedSongs
					.SelectMany(songKVP => songKVP.Value)
					.SelectMany(sheetSongKVP => sheetSongKVP.Value.requesters.Concat(sheetSongKVP.Value.vetoers))
					.FirstOrDefault(userIdentifier => contextInfo.username.Contains(userIdentifier, StringComparison.InvariantCultureIgnoreCase));
				if (string.IsNullOrEmpty(identifier))
					return new UpdateInfoResult(UpdateInfoResult.ErrorCode.NoUserData, id);

				int oldSheets = 0;
				int newSheets = 0;
				foreach (var sheetData in generalData.Values)
				{
					if (!canApply(sheetData))
					{
						ConsoleUtil.WriteLine("Command '" + command + "' is not applicable for user '" + contextInfo.username + "' for song with ID '" + id + "' in sheet with ID '" + sheetData.sheetID + "'.");
						oldSheets++;
						continue;
					}

					newSheets++;

					doApply(sheetData, identifier);
					ConsoleUtil.WriteLine("Applied command '" + command + "' for user '" + contextInfo.username + "' for song with ID '" + id + "' under tag '" + identifier + "' in sheet with ID '" + sheetData.sheetID + ".");
				}

				if (newSheets > 0)
					return new UpdateInfoResult(UpdateInfoResult.ErrorCode.None, id);
				else if (oldSheets > 0)
					return new UpdateInfoResult(UpdateInfoResult.ErrorCode.OperationRedundant, id);
				else
					return new UpdateInfoResult(UpdateInfoResult.ErrorCode.NoSongData, id);
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing '" + command + "' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return new UpdateInfoResult(UpdateInfoResult.ErrorCode.Unknown, null);
			}
		}
	}
}
