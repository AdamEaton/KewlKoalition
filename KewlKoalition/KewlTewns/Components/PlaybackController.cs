using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;
using Discord.WebSocket;
using KewlKommon.Components;
using KewlKommon.Core;
using KewlKommon.Extensions;
using KewlKommon.Utilities;
using KewlTewns.Core;
using KewlTewns.Utilities;

namespace KewlTewns.Components
{
	public class PlaybackController : PerGuildMonitor<PlaybackController>
	{
		static readonly TimeSpan MaxIntermissionLength = TimeSpan.FromSeconds(10);

		public IAudioClient? audioClient { get; private set; }
		AudioOutStream? audioStream { get; set; }
		public SocketVoiceChannel? currentChannel { get; private set; }
		public string? currentTrack { get; private set; }

		readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
		readonly ConcurrentQueue<string> loopQueue = new ConcurrentQueue<string>();

		public bool isPlaying { get; private set; }
		bool _paused;
		public bool paused
		{
			get { return _paused; }
			set
			{
				if (paused == value)
					return;

				_paused = value;

				if (_paused && PushToQueue(YoutubeUtil.IDToPath(currentTrack)))
					Skip();
			}
		}
		bool lastAnnouncedPauseStatus = false;
		bool _loop = false;
		public bool loop
		{
			get { return _loop; }
			set
			{
				if (loop == value)
					return;

				_loop = value;
				EventUtil.Dispatch(new PlaybackOptionsUpdatedEvent(guild));
			}
		}
		bool _personalize = true;
		public bool personalize
		{
			get { return _personalize; }
			set
			{
				if (_personalize == value)
					return;

				_personalize = value;
				EventUtil.Dispatch(new PlaybackOptionsUpdatedEvent(guild));
			}
		}

		bool skipFlag = false;

		enum SuspendState
		{
			Active,
			Suspended,
			Inactive,
			Reinstated,
		}
		SuspendState currentSuspendState;

		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		void GenerateNewToken()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			cancellationTokenSource = new CancellationTokenSource();
		}

		public override async void Monitor()
		{
			using (new KewlProgram.ShutDownLock(KewlProgram.Bot))
			{
				while (!KewlProgram.Bot.shuttingDown)
				{
					try
					{
						if (Program.Bot == null)
							continue;

						switch (currentSuspendState)
						{
							case SuspendState.Active:
								break;
							case SuspendState.Suspended:
								var channel = currentChannel;
								var track = currentTrack;

								await HandleCleanUp();
								currentChannel = channel;
								currentTrack = track;
								currentSuspendState = SuspendState.Inactive;
								continue;
							case SuspendState.Inactive:
								continue;
							case SuspendState.Reinstated:
								currentSuspendState = SuspendState.Active;
								break;
						}

						if (currentChannel != null && Program.Bot.GetSelfUser(currentChannel.Guild).VoiceChannel != currentChannel)
						{
							var track = currentTrack;
							var channel = currentChannel;
							if (!string.IsNullOrEmpty(track))
							{
								PushToQueue(YoutubeUtil.IDToPath(track));
								currentTrack = null;
							}
							GenerateNewToken();
							try
							{
								var commands = CommandQueue.Get(guild);
								if (commands != null && !commands.allCommandsCancelled)
									await JoinAsync(channel);
							}
							catch (Exception ex)
							{
								ConsoleUtil.WriteLine("Error while reconnecting:", ConsoleColor.Red);
								ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
								continue;
							}
						}

						if (audioClient == null || currentChannel == null)
							continue;

						if (currentChannel != null && !currentChannel.ConnectedUsers.Any(x => x != Program.Bot.GetSelfUser(currentChannel.Guild)))
						{
							Get<CommandQueue>().CancelCurrentActions();
							await LeaveAsync();
							continue;
						}

						if (isPlaying && currentChannel != null)
						{
							if (Program.Bot.GetSelfUser(currentChannel.Guild).VoiceChannel == null)
								await JoinAsync(currentChannel);

							if (skipFlag)
							{
								skipFlag = false;
								GenerateNewToken();
							}
							continue;
						}
						else
						{
							skipFlag = false;
						}

						if (paused)
						{
							if (lastAnnouncedPauseStatus == false)
							{
								EventUtil.Dispatch(new PausedQueueEvent(guild, true));
								lastAnnouncedPauseStatus = true;
							}
							continue;
						}
						else
						{
							if (lastAnnouncedPauseStatus == true)
							{
								EventUtil.Dispatch(new PausedQueueEvent(guild, false));
								lastAnnouncedPauseStatus = false;
							}
						}

						if (queue.TryDequeue(out var next))
						{
							while (!Get<SheetsService>().ShouldPlaySong(YoutubeUtil.PathToID(next)))
							{
								if (!queue.TryDequeue(out next))
								{
									next = null;
									break;
								}
							}

							if (string.IsNullOrEmpty(next))
								continue;

							loopQueue.Enqueue(next);
							Stream(next);
							continue;
						}
						else if (loop && !loopQueue.IsEmpty)
						{
							while (loopQueue.TryDequeue(out next))
							{
								queue.Enqueue(next);
							}

							if (queue.TryDequeue(out next))
							{
								loopQueue.Enqueue(next);
								Stream(next);
							}
							continue;
						}
						else
						{
							await LeaveAsync();
							continue;
						}
					}
					catch (Exception ex)
					{
						ConsoleUtil.WriteLine("Error while monitoring PlaybackController:", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					}
					finally
					{
						await Task.Delay(100);
					}
				}
			}
		}

		async void Stream(string path)
		{
			isPlaying = true;

			try
			{
				ConsoleUtil.WriteLine("Playing song file at path '" + path + "'.");
				currentTrack = YoutubeUtil.PathToID(path);

				if (!string.IsNullOrEmpty(currentTrack))
					EventUtil.Dispatch(new PlayedSongEvent(guild, currentTrack));

				var token = cancellationTokenSource.Token;

				using (var ffmpeg = FFmpegUtil.CreateStream(path))
				using (var output = ffmpeg?.StandardOutput.BaseStream)
				{
					try
					{
						if (output != null && audioStream != null)
							await output.CopyToAsync(audioStream, 81920, token);
					}
					catch (OperationCanceledException) { }
					catch (ObjectDisposedException) { }
					catch (Exception ex)
					{
						ConsoleUtil.WriteLine("Error while copying song at path '" + path + "' to AudioStream:", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					}
					finally
					{
						Task doStream = audioStream != null ? audioStream.FlushAsync(token) : Task.CompletedTask;
						Task checkTimeout = TrackStreamTimeout(path, token);

						var completedTask = await Task.WhenAny(doStream, checkTimeout);
						if (completedTask == checkTimeout)
						{
							ConsoleUtil.WriteLine("Max intermission length exceeded - forcing skip and reconnect...", ConsoleColor.Yellow);
							GenerateNewToken();
							await Task.Delay(100);
							await Refresh();
						}
					}
				}
			}
			catch (OperationCanceledException) { }
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while streaming song at path '" + path + "':", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
			}
			finally
			{
				currentTrack = null;
				isPlaying = false;
			}
		}
		static async Task TrackStreamTimeout(string path, CancellationToken token)
		{
			await Task.Delay(await YoutubeUtil.GetSongLength(path) + MaxIntermissionLength, token);
		}

		public async Task<bool> JoinAsync(SocketVoiceChannel? channel)
		{
			try
			{
				if (channel == null)
					return false;
				if (Program.Bot == null)
					return false;

				if (audioClient != null)
				{
					if (channel == currentChannel && Program.Bot.GetSelfUser(channel.Guild).VoiceChannel == channel)
						return true;

					await HandleCleanUp();
				}

				audioClient = await channel.ConnectAsync();
				if (audioClient == null)
					return false;

				await Program.Bot.GetSelfUser(channel.Guild).ModifyAsync(x => x.Deaf = true);

				audioStream = audioClient.CreatePCMStream(AudioApplication.Music);

				currentChannel = channel;

				await Task.Delay(1000);
				return true;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while joining voice channel:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}
		}
		public async Task LeaveAsync()
		{
			try
			{
				loop = false;
				personalize = true;
				paused = false;

				if (lastAnnouncedPauseStatus)
				{
					EventUtil.Dispatch(new PausedQueueEvent(guild, false));
					lastAnnouncedPauseStatus = false;
				}

				while (queue.TryDequeue(out var dummy)) ;
				EventUtil.Dispatch(new ClearedQueueEvent(guild));
				while (loopQueue.TryDequeue(out var dummy)) ;
				await HandleCleanUp();
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while leaving voice channel:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
			}
		}

		public async Task<bool> Refresh()
		{
			try
			{
				if (currentChannel == null)
					return false;
				await currentChannel.DisconnectAsync();
				return true;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while refreshing audio stream:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}
		}

		public void Suspend()
		{
			switch (currentSuspendState)
			{
				case SuspendState.Active:
					currentSuspendState = SuspendState.Suspended;
					break;
				case SuspendState.Reinstated:
					currentSuspendState = SuspendState.Inactive;
					break;
			}
		}
		public void Reinstate(SocketVoiceChannel channel)
		{
			try
			{
				if (channel == null)
					return;

				currentChannel = channel;
			}
			finally
			{
				switch (currentSuspendState)
				{
					case SuspendState.Inactive:
						currentSuspendState = SuspendState.Reinstated;
						break;
					case SuspendState.Suspended:
						currentSuspendState = SuspendState.Active;
						break;
				}
			}
		}

		public async Task HandleCleanUp()
		{
			GenerateNewToken();

			try
			{
				audioStream?.Dispose();
			}
			catch (ObjectDisposedException) { }

			if (audioClient != null)
			{
				await audioClient.StopAsync();
				audioClient.Dispose();
			}

			if (currentChannel != null)
				await currentChannel.DisconnectAsync();

			audioClient = null;
			currentChannel = null;
			currentTrack = null;
			isPlaying = false;
		}

		public void AddToQueue(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return;

			queue.Enqueue(path);
		}
		public bool PushToQueue(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			var newQueue = new Queue<string>();
			newQueue.Enqueue(path);
			while (queue.TryDequeue(out var track))
				newQueue.Enqueue(track);
			while (newQueue.Count > 0)
				queue.Enqueue(newQueue.Dequeue());

			return true;
		}
		public IEnumerable<string> GetQueuedTracks(int count)
		{
			foreach (var track in queue.Where(x => Get<SheetsService>().ShouldPlaySong(YoutubeUtil.PathToID(x))).Take(count))
			{
				var output = YoutubeUtil.PathToID(track);
				if (!string.IsNullOrEmpty(output))
					yield return output;
			}
		}
		public int GetQueueLength(bool includeNonPlaying)
		{
			return queue.Where(x => includeNonPlaying || Get<SheetsService>().ShouldPlaySong(YoutubeUtil.PathToID(x))).Count() + (isPlaying ? 1 : 0);
		}
		public void Skip()
		{
			skipFlag = true;
		}
		public void ShuffleQueue()
		{
			var newQueue = queue.Shuffled().ToList();
			while (queue.TryDequeue(out _)) ;
			foreach (var track in newQueue)
				queue.Enqueue(track);
		}

		public class PlayedSongEvent : EventInstance
		{
			public SocketGuild? guild;
			public string id;

			public PlayedSongEvent(SocketGuild? guild, string id)
			{
				this.guild = guild;
				this.id = id;
			}
		}
		public class PausedQueueEvent : EventInstance
		{
			public SocketGuild? guild;
			public bool paused;

			public PausedQueueEvent(SocketGuild? guild, bool paused)
			{
				this.guild = guild;
				this.paused = paused;
			}
		}
		public class ClearedQueueEvent : EventInstance
		{
			public SocketGuild? guild;

			public ClearedQueueEvent(SocketGuild? guild)
			{
				this.guild = guild;
			}
		}
		public class PlaybackOptionsUpdatedEvent : EventInstance
		{
			public SocketGuild? guild;

			public PlaybackOptionsUpdatedEvent(SocketGuild? guild)
			{
				this.guild = guild;
			}
		}
		public class MaxIntermissionExceededEvent : EventInstance
		{
			public SocketGuild? guild;

			public MaxIntermissionExceededEvent(SocketGuild? guild)
			{
				this.guild = guild;
			}
		}
	}
	public static class PlaybackControllerExt
	{
		public static bool PersonalizeEnabled(this PlaybackController? controller)
		{
			if (controller == null)
				return false;
			return controller.personalize;
		}
		public static bool LoopEnabled(this PlaybackController? controller)
		{
			if (controller == null)
				return false;
			return controller.loop;
		}
	}
}