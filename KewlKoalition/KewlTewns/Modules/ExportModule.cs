using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	[HelpInfo("export", "For saving offline playlists. (Admins only)", HelpPriorities.Export)]
	public class ExportModule : KewlTewnsModule
	{
		[SlashCommand("export", "Export a local copy of the specified Google Sheets playlist. (Admins only)", false, RunMode.Async)]
		public async Task Export
			(
				[Summary("url", "The URL to the Google Sheets playlist.")]
				string url,
				[Summary("requesters", "A space-separated list of requesters to personalize to. (Default: All)")]
				string? requesters = null
			)
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context, Requirement.Admin);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					var id = SheetsService.URLToSheetID(url);
					if (string.IsNullOrEmpty(id))
					{
						ConsoleUtil.WriteLine("Cannot parse URL as Google Sheets playlist: " + url);
						await SendResponse(false, Context.User.Mention + " Is this even a spreadsheet?\n" + url);
						return;
					}

					string[]? r = null;
					if (!string.IsNullOrWhiteSpace(requesters))
					{
						r = [.. requesters.Trim().Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))];
						if (r.Length == 0)
						{
							r = null;
						}
					}

					ConsoleUtil.WriteLine("Exporting songs...");
					await PerformStandardAcknowledgement(true);

					var now = DateTime.Now;
					string path = Path.Combine("Exports",
						now.Year.ToString("0000") + "-" + now.Month.ToString("00") + "-" + now.Day.ToString("00") + " "
						+ now.Hour.ToString("00") + "-" + now.Minute.ToString("00") + "-" + now.Second.ToString("00"));
					foreach (var c in Path.GetInvalidPathChars())
						path = path.Replace(c, '_');
					Directory.CreateDirectory(path);

					var usedFilenames = new HashSet<string>();
					foreach (var songData in SheetsService.ExtractPlaylistFromSheet(url) ?? [])
					{
						if (KewlProgram.Bot.shuttingDown)
							return;

						using (new KewlProgram.ShutDownLock(KewlProgram.Bot))
						{
							try
							{
								if (r != null
									&& (!songData.requesters.Any(x => r.Any(y => y.Equals(x, StringComparison.InvariantCultureIgnoreCase)))
									|| songData.vetoers.Any(x => r.Any(y => y.Equals(x, StringComparison.InvariantCultureIgnoreCase)))))
									continue;

								var downloadInfo = await YoutubeUtil.DownloadOrFindVideo(songData.Url);
								if (!await PerformStandardDownloadResponse(contextInfo, downloadInfo, false))
								{
									songData.AddInfo(SongData.Info.Broken);
									continue;
								}
								else
								{
									songData.RemoveInfo(SongData.Info.Broken);
								}

								var filename = GetFilename(path, songData);

								if (File.Exists(filename))
								{
									usedFilenames.Add(filename.ToLowerInvariant());
									File.Move(filename, GetFilename(path, songData, GetLowestUniqueIndex(path, songData)));
								}
								if (usedFilenames.Contains(filename.ToLowerInvariant()))
								{
									filename = GetFilename(path, songData, GetLowestUniqueIndex(path, songData));
								}

								await FFmpegUtil.CopyFileWithMetadata(downloadInfo.path, filename,
									 ("title", songData.title),
									 ("artist", "Kewl Tewns"),
									 ("album", songData.source),
									 ("comment", songData.Url));

								ConsoleUtil.WriteLine("Successfully copied file '" + downloadInfo.path + "' to '" + filename + "'.");
							}
							catch (Exception ex)
							{
								if (songData == null)
									continue;

								ConsoleUtil.WriteLine("Error while copying song with ID '" + songData.id + "':\n" + ex);
								continue;
							}
						}
					}

					ConsoleUtil.WriteLine("Finished exporting playsheet with ID '" + id + "' to path '" + path + "'.");
					await SendReply(Context.User.Mention + " Your request has been exported!\n" + url);

					var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					if (string.IsNullOrEmpty(rootPath))
						return;
					var openPath = Path.Combine(rootPath, path);
					Process.Start("explorer.exe", openPath);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while exporting playlist with URL '" + url + "':", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}

		static int GetLowestUniqueIndex(string folderPath, SongData songData)
		{
			int i;
			for (i = 1; File.Exists(GetFilename(folderPath, songData, i)); i++) ;
			return i;
		}
		static string GetFilename(string folderPath, SongData songData)
		{
			var filename = songData.source + " -- " + songData.title + ".mp3";
			foreach (var c in Path.GetInvalidFileNameChars())
				filename = filename.Replace(c, '_');

			return Path.Combine(folderPath, filename);
		}
		static string GetFilename(string folderPath, SongData songData, int index)
		{
			var filename = songData.source + " -- " + songData.title + " (" + index + ").mp3";
			foreach (var c in Path.GetInvalidFileNameChars())
				filename = filename.Replace(c, '_');

			return Path.Combine(folderPath, filename);
		}
	}
}