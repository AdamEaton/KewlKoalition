using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Interaction;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Extensions;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	[HelpInfoGroup("search", "For searching YouTube for content.", HelpPriorities.Search)]
	public class SearchModule : KewlTewnsModule
	{
		[SlashCommand("song", "Play the first song found by searching the specified query.", false, RunMode.Async)]
		public async Task Song
			(
				[Summary("search-query", "The query to search for.")]
				string query,
				[Summary("play-next", "Enable to bypass queue and play as soon as possible. (Default: False)")]
				bool playNext = false
			)
		{
			using (responseHandler)
			{
				await Song(responseHandler, ParsedContextInfo.ParseContext(Context), query, playNext);
			}
		}
		public static async Task<bool> Song(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string query, bool playNext = false, bool includeResponse = true)
		{
			var commands = CommandQueue.Get(contextInfo.guild);
			var controller = PlaybackController.Get(contextInfo.guild);

			if (includeResponse)
				await responseHandler.PerformStandardAcknowledgement(true);

			try
			{
				using (var handle = new CommandHandle(commands))
				{

					if (!await handle.WaitToValidate())
						return false;

					if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Reply))
						return false;

					if (!await handle.WaitToExecute())
						return false;

					await responseHandler.SendReply(contextInfo.mention + "Preparing...");

					var url = await YoutubeUtil.GetSongURLFromSearch(query);
					if (string.IsNullOrEmpty(url))
					{
						ConsoleUtil.WriteLine("Search query '" + query + "' found no results.");
						await responseHandler.SendReply(contextInfo.mention + "Search '" + query + "' returned no results...");
						return false;
					}

					var downloadInfo = await YoutubeUtil.DownloadOrFindVideo(url);
					if (!await responseHandler.PerformStandardDownloadResponse(contextInfo, downloadInfo, false))
					{
						await responseHandler.SendReply(contextInfo.mention + "I found this, but I'm having trouble playing it...\n" + url);
						return false;
					}

					if (playNext)
						controller.PushToQueue(downloadInfo.path);
					else
						controller.AddToQueue(downloadInfo.path);
					await controller.JoinAsync(contextInfo.voiceChannel);

					ConsoleUtil.WriteLine("Added video with ID '" + downloadInfo.id + "' to queue.");
					await responseHandler.SendReply(contextInfo.mention + "I found this!\n" + url);
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while searching for query '" + query + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}

			return true;
		}
		[SlashCommand("list", "Play the first YouTube playlist found by searching the specified query.", false, RunMode.Async)]
		public async Task List
			(
				[Summary("search-query", "The query to search for.")]
				string query,
				[Summary("shuffle", "Enable to randomize the order of songs in the list. (Default: False)")]
				bool shuffle = false
			)
		{
			using (responseHandler)
			{
				await List(responseHandler, ParsedContextInfo.ParseContext(Context), query, shuffle);
			}
		}
		public static async Task<bool> List(IResponseHandler responseHandler, ParsedContextInfo contextInfo, string query, bool shuffle, bool includeResponse = true)
		{
			var commands = CommandQueue.Get(contextInfo.guild);
			var controller = PlaybackController.Get(contextInfo.guild);

			if (includeResponse)
				await responseHandler.PerformStandardAcknowledgement(true);

			try
			{
				using (var handle = new CommandHandle(commands))
				{

					if (!await handle.WaitToValidate())
						return false;

					if (!await responseHandler.PerformStandardContextResponse(contextInfo, ResponseMode.Reply))
						return false;

					if (!await handle.WaitToExecute())
						return false;

					var url = await YoutubeUtil.GetListURLFromSearch(query);
					if (string.IsNullOrEmpty(url))
					{
						ConsoleUtil.WriteLine("Search query '" + query + "' found no results.");
						await responseHandler.SendReply(contextInfo.mention + "Search '" + query + "' returned no results...");
						return false;
					}

					var id = YoutubeUtil.URLToListID(url);

					ConsoleUtil.WriteLine("Adding playlist with ID '" + id + "' to queue.");
					await responseHandler.SendReply(contextInfo.mention + "Queueing playlist...\n" + url);

					foreach (var vid in await YoutubeUtil.GetPlaylistLinks(url, shuffle))
					{
						if (KewlProgram.Bot.shuttingDown)
							break;

						using (new KewlProgram.ShutDownLock(KewlProgram.Bot))
						{
							YoutubeUtil.DownloadedVideoInfo? downloadInfo = null;
							try
							{
								downloadInfo = await YoutubeUtil.DownloadOrFindVideo(vid);
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
					return true;
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while searching for query '" + query + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}
		}
	}
}