using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using KewlKommon.Utilities;
using KewlKommon.Core;
using KewlKommon.Components;
using KewlTewns.Utilities;

namespace KewlTewns.Components
{
	public class SheetsService : PerGuildSingleton<SheetsService>, IRegisteredListener
	{
		static Google.Apis.Sheets.v4.SheetsService? Service;
		static string[] SheetsScopes = [Google.Apis.Sheets.v4.SheetsService.Scope.Spreadsheets];
		public static bool Initialized { get; private set; } = false;

		public const string UnknownSongDisplay = "[Broken Link]";

		const string SheetName = "Sheet1";
		const string RequestersColumn = "D";
		const string VetoersColumn = "E";
		const string PlayedColumn = "F";
		const string LastPlayTimeColumn = "G";
		const string LengthColumn = "H";
		const string InfoColumn = "I";

		public ConcurrentDictionary<string, ConcurrentDictionary<string, SongData>> enqueuedSongs = new ConcurrentDictionary<string, ConcurrentDictionary<string, SongData>>();
		public SocketVoiceChannel? cachedChannel = null;
		public SocketVoiceChannel? currentChannel
		{
			get
			{
				if (cachedChannel != null)
					return cachedChannel;
				return PlaybackController.Get(guild)?.currentChannel;
			}
		}

		public static void Initialize()
		{
			UserCredential credential;

			using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.FromStream(stream).Secrets,
					SheetsScopes,
					"user",
					CancellationToken.None,
					new FileDataStore(credPath, true)).Result;
				ConsoleUtil.WriteLine("Google Sheets credential file saved to: " + credPath);
			}

			Service = new Google.Apis.Sheets.v4.SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "Kewl Tewns",
			});

			Initialized = true;
		}
		
		public static void AddListeners()
		{
			EventUtil.AddListener<PlaybackController.PlayedSongEvent>(OnPlayedSong);
			EventUtil.AddListener<PlaybackController.ClearedQueueEvent>(OnClearedQueue);
		}
		public static void RemoveListeners()
		{
			EventUtil.RemoveListener<PlaybackController.PlayedSongEvent>(OnPlayedSong);
			EventUtil.RemoveListener<PlaybackController.ClearedQueueEvent>(OnClearedQueue);
		}
		static void OnPlayedSong(PlaybackController.PlayedSongEvent e)
		{
			if (e.guild == null)
				return;

			var sheets = Get(e.guild);
			var controller = PlaybackController.Get(e.guild);

			if (sheets == null)
				return;
			if (controller == null)
				return;

			sheets.RecordLength(e.id);

			if (!sheets.ShouldPlaySong(e.id))
			{
				controller.Skip();
				return;
			}

			if (sheets.TryGetSongData(e.id, out var data) && data != null && !string.IsNullOrEmpty(data.id))
			{
				if ((controller?.currentChannel?.Category?.Name ?? string.Empty).Contains("test", StringComparison.InvariantCultureIgnoreCase))
					sheets.IncrementPlays(data.id);

				EventUtil.Dispatch(new PlayedSongDataEvent(e.guild, data));
			}
			else
			{
				EventUtil.Dispatch(new PlayedUnknownSongEvent(e.guild, e.id));
			}
		}
		static void OnClearedQueue(PlaybackController.ClearedQueueEvent e)
		{
			if (e.guild == null)
				return;

			var sheets = Get(e.guild);
			if (sheets == null)
				return;

			sheets.enqueuedSongs.Clear();
		}
		
		public void AddSongData(SongData data)
		{
			if (string.IsNullOrEmpty(data.id) || string.IsNullOrEmpty(data.sheetID))
				return;

			var generalData = enqueuedSongs.GetOrAdd(data.id, new ConcurrentDictionary<string, SongData>());
			generalData.TryAdd(data.sheetID, data);
		}
		public bool TryGetSongData(string? id, out SongData? data)
		{
			data = GetSongData(id);
			return data != null;
		}
		SongData? GetSongData(string? id)
		{
			if (string.IsNullOrEmpty(id))
				return null;

			if (enqueuedSongs.TryGetValue(id, out var generalData))
				return generalData.Values.FirstOrDefault();
			return null;
		}

		public static string? URLToSheetID(string? url)
		{
			string tag = "spreadsheets/d/";

			if (string.IsNullOrEmpty(url))
				return null;

			var index = url.IndexOf(tag);
			if (index < 0)
				return null;

			string output = url[(index + tag.Length)..];
			index = output.IndexOf('/');
			if (index < 0)
				return output;

			return output[..index];
		}
		public static string? SheetIDToURL(string? id)
		{
			if (string.IsNullOrEmpty(id))
				return null;

			return "http://docs.google.com/spreadsheets/d/" + id + "/";
		}

		public static string SerializeTime(float time)
		{
			string output = "";
			output += ((int)Math.Floor(time / 60)).ToString("00");
			output += ":";
			output += ((int)Math.Floor(time % 60)).ToString("00");
			return output;
		}
		public static float DeserializeTime(string? time)
		{
			try
			{
				if (string.IsNullOrEmpty(time))
					return 0;

				var split = time.Split(':');

				return int.Parse(split[0]) * 60 + int.Parse(split[1]);
			}
			catch (Exception)
			{
				return 0;
			}
		}

		public static List<SongData>? ExtractPlaylistFromSheet(string? sheetURL)
		{
			try
			{
				List<SongData> output = [];

				if (Service == null)
					return output;
				if (!Initialized)
					return output;

				var id = URLToSheetID(sheetURL);
				if (string.IsNullOrEmpty(id))
					return output;

				var request = Service.Spreadsheets.Values.Get(id, SheetName);
				ValueRange result = request.Execute();

				foreach (var row in result.Values.Skip(1))
				{
					output.Add(new SongData(
						id,
						TryGetRowData(row, 0),
						YoutubeUtil.URLToSongID(TryGetRowData(row, 1)),
						TryGetRowData(row, 2),
						[.. ((TryGetRowData(row, 3) ?? "").Split(',') ?? []).Where(x => !string.IsNullOrWhiteSpace(x))],
						[.. ((TryGetRowData(row, 4) ?? "").Split(',') ?? []).Where(x => !string.IsNullOrWhiteSpace(x))],
						ParseIntOrDefault(TryGetRowData(row, 5) ?? "0"),
						DateTimeUtil.DeserializeDateTime(TryGetRowData(row, 6)),
						DeserializeTime(TryGetRowData(row, 7)),
						[.. ((TryGetRowData(row, 8) ?? "").Split(',') ?? []).Where(x => !string.IsNullOrWhiteSpace(x))]
						));
				}

				return output;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error loading spreadsheet from URL '" + sheetURL + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return null;
			}
		}
		static string? TryGetRowData(IList<object> row, int index)
		{
			if (row.Count <= index)
				return string.Empty;

			return row[index].ToString();
		}
		static int ParseIntOrDefault(string data)
		{
			try
			{
				return int.Parse(data);
			}
			catch { return 0; }
		}

		public void IncrementPlays(string id)
		{
			if (enqueuedSongs.TryGetValue(id, out var generalData))
				foreach (var songData in generalData.Values)
					IncrementPlays(songData);
		}
		static void IncrementPlays(SongData songData)
		{
			if (Service == null)
				return;

			songData.playCount++;
			songData.playTime = DateTime.Now;

			if (!TryGetRowIndex(songData.sheetID, songData.id, out var rowIndex))
				return;

			List<IList<object>> data =
			[
				[
					songData.playCount.ToString(),
					DateTimeUtil.SerializeDateTime(songData.playTime)
				]
			];

			var valueRange = new ValueRange();
			valueRange.Values = data;

			var request = Service.Spreadsheets.Values.Update(valueRange, songData.sheetID, SheetName + "!" + PlayedColumn + rowIndex + ":" + LastPlayTimeColumn + rowIndex);
			request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
			request.Execute();
		}

		public async void RecordLength(string id)
		{
			using (new KewlProgram.ShutDownLock(KewlProgram.Bot))
			{
				try
				{
					if (enqueuedSongs.TryGetValue(id, out var generalData))
					{
						var info = await YoutubeUtil.GetSongLength(YoutubeUtil.IDToPath(id));
						foreach (var songData in generalData.Values)
							RecordLength(songData, (float)info.TotalSeconds);
					}
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while recording song length for song with ID '" + id + "':", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				}
			}
		}
		static void RecordLength(SongData songData, float length)
		{
			if (Service == null)
				return;
			if (!TryGetRowIndex(songData.sheetID, songData.id, out var rowIndex))
				return;

			List<IList<object>> data =
			[
				[
					SerializeTime(length)
				]
			];

			var valueRange = new ValueRange();
			valueRange.Values = data;

			var request = Service.Spreadsheets.Values.Update(valueRange, songData.sheetID, SheetName + "!" + LengthColumn + rowIndex);
			request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
			request.Execute();
		}
		
		public static void SaveRequesters(SongData songData)
		{
			if (Service == null)
				return;
			if (!TryGetRowIndex(songData.sheetID, songData.id, out var rowIndex))
				return;

			List<IList<object>> data =
			[
				[
					string.Join(",", songData.requesters)
				]
			];

			var valueRange = new ValueRange();
			valueRange.Values = data;

			var request = Service.Spreadsheets.Values.Update(valueRange, songData.sheetID, SheetName + "!" + RequestersColumn + rowIndex);
			request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
			request.Execute();
		}
		public static void SaveVetoers(SongData songData)
		{
			if (Service == null)
				return;
			if (!TryGetRowIndex(songData.sheetID, songData.id, out var rowIndex))
				return;

			List<IList<object>> data =
			[
				[
					string.Join(",", songData.vetoers)
				]
			];

			var valueRange = new ValueRange();
			valueRange.Values = data;

			var request = Service.Spreadsheets.Values.Update(valueRange, songData.sheetID, SheetName + "!" + VetoersColumn + rowIndex);
			request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
			request.Execute();
		}
		public static void SaveInfo(SongData songData)
		{
			if (Service == null)
				return;
			if (!TryGetRowIndex(songData.sheetID, songData.id, out var rowIndex))
				return;

			List<IList<object>> data =
			[
				[
					string.Join(",", songData.info)
				]
			];
			
			var valueRange = new ValueRange();
			valueRange.Values = data;

			var request = Service.Spreadsheets.Values.Update(valueRange, songData.sheetID, SheetName + "!" + InfoColumn + rowIndex);
			request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
			request.Execute();
		}

		static bool TryGetRowIndex(string? sheetID, string? songID, out int rowIndex)
		{
			if (Service == null || string.IsNullOrEmpty(sheetID) || string.IsNullOrEmpty(songID))
			{
				rowIndex = -1;
				return false;
			}	

			var request = Service.Spreadsheets.Values.Get(sheetID, SheetName);
			ValueRange result = request.Execute();

			for (int i = 1; i < result.Values.Count; i++)
				if (songID == YoutubeUtil.URLToSongID(TryGetRowData(result.Values[i], 1)))
				{
					rowIndex = i + 1;
					return true;
				}

			rowIndex = -1;
			return false;
		}
	}
	public class SongData
	{
		public string? sheetID;
		public string? title;
		public string? id;
		public string? source;
		public List<string> requesters;
		public List<string> vetoers;
		public int playCount;
		public DateTime playTime;
		public float length;
		public List<string> info;

		public string? Url { get { return YoutubeUtil.SongIDToURL(id); } }
		public string DisplayInfo { get { return source + " -- " + title; } }

		public SongData(string? sheetID, string? title, string? id, string? source, List<string> requesters, List<string> vetoers, int playCount, DateTime playTime, float length, List<string> info)
		{
			this.sheetID = sheetID;
			this.title = title;
			this.id = id;
			this.source = source;
			this.requesters = requesters;
			this.requesters.RemoveAll(string.IsNullOrWhiteSpace);
			this.vetoers = vetoers;
			this.vetoers.RemoveAll(string.IsNullOrWhiteSpace);
			this.playCount = playCount;
			this.playTime = playTime;
			this.length = length;
			this.info = info;
			this.info.RemoveAll(string.IsNullOrWhiteSpace);
		}
		
		public void AddRequester(string requester)
		{
			if (requesters.Contains(requester))
				return;

			RemoveVetoer(requester);

			requesters.Add(requester);
			requesters.RemoveAll(string.IsNullOrWhiteSpace);
			SheetsService.SaveRequesters(this);
		}
		public void RemoveRequester(string requester)
		{
			requesters.RemoveAll(x => requester.Contains(x, StringComparison.InvariantCultureIgnoreCase));
			SheetsService.SaveRequesters(this);
		}
		public void AddVetoer(string veto)
		{
			if (vetoers.Contains(veto))
				return;

			RemoveRequester(veto);

			vetoers.Add(veto);
			vetoers.RemoveAll(string.IsNullOrWhiteSpace);
			SheetsService.SaveVetoers(this);
		}
		public void RemoveVetoer(string veto)
		{
			vetoers.RemoveAll(x => veto.Contains(x, StringComparison.InvariantCultureIgnoreCase));
			SheetsService.SaveVetoers(this);
		}
		public void AddInfo(string info)
		{
			if (this.info.Contains(info))
				return;

			this.info.Add(info);
			this.info.RemoveAll(string.IsNullOrWhiteSpace);
			SheetsService.SaveInfo(this);
		}
		public void RemoveInfo(string info)
		{
			if (!this.info.Contains(info))
				return;

			this.info.Remove(info);
			SheetsService.SaveInfo(this);
		}

		public static class Info
		{
			public const string Broken = "Broken";
			public const string Flagged = "Flagged";
		}
	}

	public class PlayedSongDataEvent : EventInstance
	{
		public SocketGuild guild;
		public SongData songData;

		public PlayedSongDataEvent(SocketGuild guild, SongData songData)
		{
			this.guild = guild;
			this.songData = songData;
		}
	}
	public class PlayedUnknownSongEvent : EventInstance
	{
		public SocketGuild guild;
		public string id;

		public PlayedUnknownSongEvent(SocketGuild guild, string id)
		{
			this.guild = guild;
			this.id = id;
		}
	}
	public static class SheetsServiceExt
	{
		public static string GetDisplayInfo(this SheetsService? sheets, string? id)
		{
			if (string.IsNullOrEmpty(id))
				return SheetsService.UnknownSongDisplay;

			string output = YoutubeUtil.GetSongTitle(id) ?? SheetsService.UnknownSongDisplay;
			if (!SheetsService.Initialized)
				return output;
			if (sheets == null)
				return output;

			if (sheets.enqueuedSongs.TryGetValue(id, out var generalData))
			{
				var songData = generalData.Values.FirstOrDefault();
				if (songData != null)
					return songData.DisplayInfo;
			}

			return output;
		}
		public static bool ShouldPlaySong(this SheetsService? sheets, string? id)
		{
			if (sheets == null)
				return true;
			if (string.IsNullOrEmpty(id))
				return false;

			if (!sheets.Get<PlaybackController>().PersonalizeEnabled())
				return true;

			if (!sheets.enqueuedSongs.TryGetValue(id, out var generalData))
				return true;
			else if (sheets.currentChannel != null)
				return sheets.currentChannel.ConnectedUsers.Any(x => generalData.Values.Any(y => y.requesters.Any(z => x.Username.Contains(z, StringComparison.InvariantCultureIgnoreCase))))
					&& !sheets.currentChannel.ConnectedUsers.Any(x => generalData.Values.Any(y => y.vetoers.Any(z => x.Username.Contains(z, StringComparison.InvariantCultureIgnoreCase))));

			return false;
		}
	}
}