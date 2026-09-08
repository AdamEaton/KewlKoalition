using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Extensions;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Extensions;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	[HelpInfoGroup("play", "For playing music.", HelpPriorities.Play)]
	public class PlayModule : KewlTewnsModule
	{
		public enum Mode
		{
			[Hide]
			Unspecified,
			Random,
			Sequential,
			LeastPlayed,
			LeastRecent,
		}
		
		[SlashCommand("song", "Play an individual song by Youtube URL.", false, RunMode.Async)]
		public async Task Song
			(
				[Summary("url", "The URL of the YouTube song to play.")]
				string url,
				[Summary("play-next", "Enable to bypass queue and play as soon as possible. (Default: False)")]
				bool playNext = false
			)
		{
			using (responseHandler)
			{
				await Song(responseHandler, ParsedContextInfo.ParseContext(Context), YoutubeUtil.URLToSongID(url), playNext);
			}
		}
		public static async Task<bool> Song(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? id, bool playNext = false, bool includeResponse = true)
		{
			string? url = null;

			try
			{
				var commands = CommandQueue.Get(contextInfo.guild);
				var controller = PlaybackController.Get(contextInfo.guild);

				using (var handle = new CommandHandle(commands))
				{
					url = YoutubeUtil.SongIDToURL(id);

					if (includeResponse)
						await responseHandler.PerformStandardAcknowledgement(true);

					if (!await handle.WaitToValidate())
						return false;

					if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Reply))
						return false;

					await responseHandler.SendReply(contextInfo.mention + "Preparing...");

					if (!await handle.WaitToExecute())
						return false;

					var downloadInfo = await YoutubeUtil.DownloadOrFindVideo(url);
					if (!await responseHandler.PerformStandardDownloadResponse(contextInfo, downloadInfo, true))
						return false;

					if (playNext)
						controller.PushToQueue(downloadInfo.path);
					else
						controller.AddToQueue(downloadInfo.path);
					await controller.JoinAsync(contextInfo.voiceChannel);

					ConsoleUtil.WriteLine("Added video with ID '" + downloadInfo.id + "' to queue.");
					await responseHandler.SendReply(contextInfo.mention + "Added your request to queue!\n" + url);
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while playing song with URL '" + url + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}

			return true;
		}

		[SlashCommand("list", "Play a YouTube playlist by URL.", false, RunMode.Async)]
		public async Task List
			(
				[Summary("url", "The URL of the YouTube playlist to play.")]
				string url,
				[Summary("shuffle", "Enable to randomize the order of songs in the list. (Default: False)")]
				bool shuffle = false
			)
		{
			using (responseHandler)
			{
				await List(responseHandler, ParsedContextInfo.ParseContext(Context), YoutubeUtil.URLToListID(url), shuffle);
			}
		}
		public static async Task<bool> List(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? id, bool shuffle, bool includeResponse = true)
		{
			string? url = null;

			try
			{
				var commands = CommandQueue.Get(contextInfo.guild);
				var controller = PlaybackController.Get(contextInfo.guild);

				using (var handle = new CommandHandle(commands))
				{
					url = YoutubeUtil.ListIDToURL(id);

					if (includeResponse)
						await responseHandler.PerformStandardAcknowledgement(true);

					if (!await handle.WaitToValidate())
						return false;

					if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Reply))
						return false;

					if (!await handle.WaitToExecute())
						return false;

					ConsoleUtil.WriteLine("Adding playlist with ID '" + id + "' to queue.");
					await responseHandler.SendReply(contextInfo.mention + "Queueing playlist...\n" + url);

					foreach (var vid in await YoutubeUtil.GetPlaylistLinks(url, shuffle))
					{
						if (KewlProgram.Bot.shuttingDown)
							break;

						using (new KewlProgram.ShutDownLock(KewlProgram.Bot))
						{
							try
							{
								YoutubeUtil.DownloadedVideoInfo downloadInfo = await YoutubeUtil.DownloadOrFindVideo(vid);
								if (!await responseHandler.PerformStandardDownloadResponse(contextInfo, downloadInfo, false))
									continue;

								if (handle.cancelled)
									return true;

								controller.AddToQueue(downloadInfo.path);
								await controller.JoinAsync(contextInfo.voiceChannel);

								ConsoleUtil.WriteLine("Added video with ID '" + downloadInfo.id + "' to queue.");
							}
							catch (Exception ex)
							{
								ConsoleUtil.WriteLine("Error while loading song from URL '" + vid + "':", ConsoleColor.Red);
								ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
								continue;
							}
						}
					}

					ConsoleUtil.WriteLine("Finished queueing playlist with ID '" + id + "'.");
					await responseHandler.SendReply(contextInfo.mention + "Your request has been fully queued!\n" + url);
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while queueing playlist with URL '" + url + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}

			return true;
		}
		
		[SlashCommand("sheet", "Play a Google Sheets playlist by URL.", false, RunMode.Async)]
		public async Task Sheet
			(
				[Summary("url", "The URL of the Google Sheets playlist to play.")]
				string url,
				[Summary("sort-mode", "The sorting mode to use when queueing tracks. (Default: Random)")]
				Mode mode = Mode.Random
			)
		{
			using (responseHandler)
			{
				await Sheet(responseHandler, ParsedContextInfo.ParseContext(Context), SheetsService.URLToSheetID(url), mode);
			}
		}
		public static async Task<bool> Sheet(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string? id, Mode mode, bool includeResponse = true)
		{
			string? url = null;

			try
			{
				var commands = CommandQueue.Get(contextInfo.guild);
				var controller = PlaybackController.Get(contextInfo.guild);
				var sheets = SheetsService.Get(contextInfo.guild);

				using (var handle = new CommandHandle(commands))
				{
					url = SheetsService.SheetIDToURL(id);

					if (includeResponse)
						await responseHandler.PerformStandardAcknowledgement(true);

					if (!await handle.WaitToValidate())
						return false;

					if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Reply))
						return false;

					if (!await handle.WaitToExecute())
						return false;

					ConsoleUtil.WriteLine("Adding playlist in spreadsheet with ID '" + id + "' to queue.");
					await responseHandler.SendReply(contextInfo.mention + "Queueing playlist...\n" + url);

					var songList = SheetsService.ExtractPlaylistFromSheet(url);
					if (songList == null)
					{
						ConsoleUtil.WriteLine("Could not read spreadsheet with ID '" + id + "'.");
						await responseHandler.SendReply(contextInfo.mention + "I'm having trouble reading that sheet. Are you sure I'm allowed?");
						return false;
					}

					var random = new Random();
					switch (mode)
					{
						case Mode.Random:
							songList = [.. songList.Shuffled()];
							break;
						case Mode.Sequential:
							break;
						case Mode.LeastRecent:
							songList = [.. songList.OrderBy(x => x.playTime).ThenBy(x => random.Next())];
							break;
						case Mode.LeastPlayed:
							songList = [.. songList.OrderBy(x => x.playCount).ThenBy(x => random.Next())];
							break;
					}

					sheets.cachedChannel = contextInfo.voiceChannel;
					var joinFlag = true;
					foreach (var songData in songList)
					{
						if (KewlProgram.Bot.shuttingDown)
							break;

						using (new KewlProgram.ShutDownLock(KewlProgram.Bot))
						{
							try
							{
								YoutubeUtil.DownloadedVideoInfo downloadInfo = await YoutubeUtil.DownloadOrFindVideo(songData.Url);
								if (!await responseHandler.PerformStandardDownloadResponse(contextInfo, downloadInfo, false))
								{
									songData.AddInfo(SongData.Info.Broken);
									continue;
								}
								else
								{
									songData.RemoveInfo(SongData.Info.Broken);
								}

								if (handle.cancelled)
									return true;

								sheets.AddSongData(songData);
								controller.AddToQueue(downloadInfo.path);

								if (joinFlag)
								{
									if (sheets.ShouldPlaySong(songData.id))
									{
										await controller.JoinAsync(contextInfo.voiceChannel);
										joinFlag = false;
										sheets.cachedChannel = null;
									}
								}

								ConsoleUtil.WriteLine("Added video with ID '" + downloadInfo.id + "' to queue.");
							}
							catch (Exception ex)
							{
								if (songData == null)
									continue;

								ConsoleUtil.WriteLine("Error while loading song with ID '" + songData.id + "':", ConsoleColor.Red);
								ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
								continue;
							}
						}
					}

					ConsoleUtil.WriteLine("Finished queueing playsheet with ID '" + id + "'.");
					await responseHandler.SendReply(contextInfo.mention + "Your request has been fully queued!\n" + url);
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while loading playsheet with URL '" + url + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}

			return true;
		}
	}
}