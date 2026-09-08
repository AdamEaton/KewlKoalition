using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using VideoLibrary;
using YoutubeExplode;
using KewlKommon.Context;
using KewlKommon.Extensions;
using KewlKommon.Utilities;

namespace KewlTewns.Utilities
{
	public static class YoutubeUtil
	{
		static readonly string VideoIDTag = "v=";
		static readonly string AltVideoIDTag = ".be/";
		static readonly string ListIDTag = "list=";
		static readonly string PathIDTag = "Tewns" + Path.DirectorySeparatorChar;

		const char UpperIndicator = ')';
		const char LowerIndicator = '(';

		static readonly YoutubeClient ytClient = new YoutubeClient();
		
		public static string? GetStandardVideoReply(ParsedContextInfo contextInfo, DownloadedVideoInfo downloadInfo)
		{
			switch (downloadInfo.errorCode)
			{
				case DownloadedVideoInfo.ErrorCode.CannotParseLink:
					return contextInfo.mention + "Is this even a Youtube video?\n" + SongIDToURL(downloadInfo.id);
				case DownloadedVideoInfo.ErrorCode.DownloadFailed:
					return contextInfo.mention + "This video doesn't seem to be working.\n" + SongIDToURL(downloadInfo.id);
				case DownloadedVideoInfo.ErrorCode.NoLocalFile:
					return contextInfo.mention + "I thought I had that one ready, but I seem to have lost it.\n" + SongIDToURL(downloadInfo.id);
				case DownloadedVideoInfo.ErrorCode.ConversionFailed:
					return contextInfo.mention + "I can't seem to get the audio for this one.\n" + SongIDToURL(downloadInfo.id);
			}
			return null;
		}
		public static string? GetStandardVideoLog(DownloadedVideoInfo downloadInfo)
		{
			switch (downloadInfo.errorCode)
			{
				case DownloadedVideoInfo.ErrorCode.CannotParseLink:
					return "Cannot parse URL as Youtube link: " + downloadInfo.url;
				case DownloadedVideoInfo.ErrorCode.DownloadFailed:
					return "Could not download video at URL: " + SongIDToURL(downloadInfo.id);
				case DownloadedVideoInfo.ErrorCode.NoLocalFile:
					return "Could not find local media for ID '" + downloadInfo.id + "'.";
				case DownloadedVideoInfo.ErrorCode.ConversionFailed:
					return "Could not convert video with ID '" + downloadInfo.id + "'.";
			}
			return null;
		}
		
		public static string AddCaseInfoToID(string id)
		{
			string output = "";

			for (int i = 0; i < id.Length; i++)
			{
				if (char.IsUpper(id[i]))
					output += UpperIndicator;
				else if (char.IsLower(id[i]))
					output += LowerIndicator;
			}

			return id + output;
		}

		public static string? PathToID(string? path)
		{
			path = URLToID(path, PathIDTag);

			if (string.IsNullOrEmpty(path))
				return null;

			var index = path.IndexOf(".mp3");
			if (index >= 0)
				path = path[..index];

			path = path.Replace(UpperIndicator.ToString(), "");
			path = path.Replace(LowerIndicator.ToString(), "");

			return path;
		}
		public static string? IDToPath(string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;

			return PathIDTag + AddCaseInfoToID(id) + ".mp3";
		}
		public static string? URLToSongID(string? url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return null;

			return URLToID(url, VideoIDTag) ?? URLToID(url, AltVideoIDTag);
		}
		public static string? SongIDToURL(string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;

			return "http://www.youtube.com/watch?v=" + id;
		}
		public static string? URLToListID(string? url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return null;

			return URLToID(url, ListIDTag);
		}
		public static string? ListIDToURL(string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;

			return "http://www.youtube.com/playlist?list=" + id;
		}
		static string? URLToID(string? url, string tag)
		{
			if (string.IsNullOrEmpty(url))
				return null;

			var index = url.IndexOf(tag);
			if (index < 0)
				return null;

			string output = url[(index + tag.Length)..];

			index = output.IndexOf('&');
			if (index < 0)
			{
				index = output.IndexOf('/');
				if (index < 0)
					return output;
			}
			
			return output[..index];
		}

		public static string? GetSongTitle(string id)
		{
			try
			{
				var youtube = YouTube.Default;
				var video = youtube.GetVideo(SongIDToURL(id));
				return video.Title;
			}
			catch { return null; }
		}

		public static async Task<TimeSpan> GetSongLength(string? path)
		{
			try
			{
				if (string.IsNullOrEmpty(path))
					return TimeSpan.Zero;

				return (await FFmpeg.GetMediaInfo(path)).Duration;
			}
			catch { return TimeSpan.Zero; }
		}

		public static async Task<DownloadedVideoInfo> DownloadOrFindVideo(string? url)
		{
			var output = new DownloadedVideoInfo();
			output.url = url;
			
			var id = URLToSongID(url);
			if (string.IsNullOrEmpty(id))
			{
				output.errorCode = DownloadedVideoInfo.ErrorCode.CannotParseLink;
				return output;
			}
			output.id = id;

			id = AddCaseInfoToID(id);

			if (!Directory.Exists("Tewns"))
				Directory.CreateDirectory("Tewns");
			
			if (Directory.GetFiles("Tewns", id + ".*").Length <= 0)
			{
				ConsoleUtil.WriteLine("Downloading video with ID '" + output.id + "'...");

				var youtube = YouTube.Default;
				try
				{
					var video = youtube.GetAllVideos(url).OrderByDescending(x => x.AudioBitrate).First();
					var bytes = await video.GetBytesAsync();
					File.WriteAllBytes(Path.Combine("Tewns", id + ".mp4"), bytes);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while downloading video with ID '" + output.id + "':", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					output.errorCode = DownloadedVideoInfo.ErrorCode.DownloadFailed;
					return output;
				}
			}
			
			var path = Directory.GetFiles("Tewns", id + ".*").FirstOrDefault();
			if (string.IsNullOrEmpty(path))
			{
				output.errorCode = DownloadedVideoInfo.ErrorCode.NoLocalFile;
				return output;
			}
			
			if (!path.EndsWith("mp3"))
			{
				ConsoleUtil.WriteLine("Converting video with ID '" + output.id + "' to mp3...");
				var newPath = Path.ChangeExtension(path, "mp3");

				var conversion = await FFmpeg.Conversions.FromSnippet.Convert(path, newPath);
				var task = conversion
					.AddParameter("-vn")
					.AddParameter("-filter:a dynaudnorm")
					.Start();
				await task;

				File.Delete(path);
				if (task.Status != TaskStatus.RanToCompletion)
				{
					output.errorCode = DownloadedVideoInfo.ErrorCode.ConversionFailed;
					return output;
				}

				path = newPath;
			}

			output.errorCode = DownloadedVideoInfo.ErrorCode.None;
			output.path = path;
			return output;
		}

		public static async Task<string?> GetSongURLFromSearch(string query)
		{
			try
			{
				var results = ytClient.Search.GetVideosAsync(query);
				return (await results.FirstOrDefaultAsync(x => x.Duration < TimeSpan.FromMinutes(10)))?.Url;
			}
			catch { return null; }
		}
		public static async Task<string?> GetListURLFromSearch(string query)
		{
			try
			{
				var results = ytClient.Search.GetPlaylistsAsync(query);
				return (await results.FirstOrDefaultAsync())?.Url;
			}
			catch { return null; }
		}

		public static async Task<List<string>> GetPlaylistLinks(string? url, bool shuffle = false)
		{
			var id = URLToListID(url);
			if (string.IsNullOrWhiteSpace(id))
				return [];

			var asyncList = ytClient.Playlists.GetVideosAsync(id);
			if (asyncList == null)
				return [];

			var list = await asyncList.ToListAsync();
			if (list == null)
				return [];

			if (shuffle)
				return [.. list.Select(x => x.Url).Shuffled()];
			return [.. list.Select(x => x.Url)];
		}

		public class DownloadedVideoInfo
		{
			public ErrorCode errorCode = ErrorCode.None;
			public string? url = null;
			public string? id = null;
			public string? path = null;

			public enum ErrorCode
			{
				None,
				CannotParseLink,
				DownloadFailed,
				NoLocalFile,
				ConversionFailed,
			}
		}
	}
}