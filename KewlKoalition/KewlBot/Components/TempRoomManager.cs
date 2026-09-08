using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using KewlKommon.Components;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Utilities;
using KewlBot.Core;
using KewlBot.Utilities;

namespace KewlBot.Components
{
	public class TempRoomManager : PerGuildMonitor<TempRoomManager>
	{
		public enum ErrorCode
		{
			None,
			UserNotFound,
			CategoryNotFound,
			NameEmpty,
			NameInvalid,
			NameTooLong,
			RoomCollision,
			CreationFailed,
		}

		const int MaxNameLength = 100;

		public const string TextChannelSuffix = "-text";
		public const string VoiceChannelSuffix = " Voice";

		public static string FullTrim(string s)
		{
			var output = new string([.. s.Select(x => char.IsWhiteSpace(x) ? ' ' : x)]);
			while (output.Contains("  "))
				output = output.Replace("  ", " ");
			return output.Trim();
		}

		public static string? StringToTextName(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			return FullTrim(name).Replace(' ', '-').ToLowerInvariant() + TextChannelSuffix;
		}
		public static string? StringToVoiceName(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			return FullTrim(name) + VoiceChannelSuffix;
		}

		public static bool IsTextChannelName(string? name)
		{
			return name?.EndsWith(TextChannelSuffix) ?? false;
		}
		public static bool IsVoiceChannelName(string? name)
		{
			return name?.EndsWith(VoiceChannelSuffix) ?? false;
		}
		public static string? RemoveChannelTypeSuffix(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return string.Empty;

			if (IsTextChannelName(name))
				return name[..^TextChannelSuffix.Length];
			if (IsVoiceChannelName(name))
				return name[..^VoiceChannelSuffix.Length];

			return name;
		}
		
		class ChannelNameComparer : StringComparer
		{
			public static readonly ChannelNameComparer Default = new ChannelNameComparer();

			public override int Compare(string? x, string? y)
			{
				return InvariantCulture.Compare(
					CollisionSafeEncoding(x),
					CollisionSafeEncoding(y));
			}

			public override bool Equals(string? x, string? y)
			{
				return InvariantCulture.Equals(
					CollisionSafeEncoding(x),
					CollisionSafeEncoding(y));
			}
			public override int GetHashCode(string obj)
			{
				return InvariantCulture.GetHashCode(CollisionSafeEncoding(obj));
			}

			public static bool CompareNames(string textName, string voiceName)
			{
				return CollisionSafeEncoding(RemoveChannelTypeSuffix(textName)) == CollisionSafeEncoding(RemoveChannelTypeSuffix(voiceName));
			}

			public static string CollisionSafeEncoding(string? name)
			{
				if (string.IsNullOrEmpty(name))
					return string.Empty;

				return new string([.. FullTrim(name).Where(char.IsLetterOrDigit)]).ToLowerInvariant();
			}
		}

		SocketCategoryChannel? GetCategoryChannel()
		{
			if (guild == null)
				return null;

			var guildConfig = Program.Config.GetGuildConfig(guild.Id);
			return guildConfig.TryGetSpecialChannel(BotUtil.TempCategoryChannelKey, out var channel) ? channel as SocketCategoryChannel : null;
		}

		HashSet<ulong> inProgress = [];
		HashSet<ulong> voiceStrikes = [];
		HashSet<ulong> textStrikes = [];
		public override async void Monitor()
		{
			while (Program.Bot == null)
			{
				await Task.Delay(100);
				continue;
			}

			while (!KewlProgram.Bot.shuttingDown)
			{
				try
				{
					var onHold = inProgress;
					var vStrikes = voiceStrikes;
					var tStrikes = textStrikes;
					if (guild == null || GetCategoryChannel() is not SocketCategoryChannel category)
					{
						await Task.Delay(1000);
						continue;
					}

					var guildConfig = Program.Config.GetGuildConfig(guild.Id);
					foreach (var voice in category.Channels.OfType<SocketVoiceChannel>())
					{
						if (guildConfig.IsTempChannelException(voice))
							continue;

						if (voice.ConnectedUsers.Count <= 0 && !onHold.Contains(voice.Id))
						{
							if (vStrikes.Remove(voice.Id))
							{
								ConsoleUtil.WriteLine("Cleaning up voice room '" + voice.Name + "'.");
								await voice.DeleteAsync();
							}
							else
							{
								ConsoleUtil.WriteLine("Voice room '" + voice.Name + "' marked for deletion.");
								vStrikes.Add(voice.Id);
							}
						}
						else
						{
							if (vStrikes.Remove(voice.Id))
							{
								ConsoleUtil.WriteLine("Voice room '" + voice.Name + "' unmarked for deletion.");
							}
						}
					}

					foreach (var text in category.Channels.OfType<SocketTextChannel>())
					{
						if (guildConfig.IsTempChannelException(text))
							continue;
						if (text.Name.Contains(' '))
							continue;

						var voice = category.Channels.OfType<SocketVoiceChannel>().FirstOrDefault(x => ChannelNameComparer.Default.Equals(RemoveChannelTypeSuffix(x.Name), RemoveChannelTypeSuffix(text.Name)));
						if (voice == null || voice.ConnectedUsers.Count <= 0 && !onHold.Contains(voice.Id))
						{
							if (tStrikes.Remove(text.Id))
							{
								ConsoleUtil.WriteLine("Cleaning up text room '" + text.Name + "'.");
								await text.DeleteAsync();
							}
							else
							{
								ConsoleUtil.WriteLine("Text room '" + text.Name + "' marked for deletion.");
								tStrikes.Add(text.Id);
							}
						}
						else
						{
							if (tStrikes.Contains(text.Id))
							{
								ConsoleUtil.WriteLine("Text room '" + voice.Name + "' unmarked for deletion.");
								tStrikes.Remove(text.Id);
							}
						}
					}
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while monitoring temp rooms:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				}

				await Task.Delay(1000);
			}
		}

		public bool CanCreateRoom(string? name, out ErrorCode error)
		{
			if (string.IsNullOrEmpty(name))
			{
				error = ErrorCode.NameEmpty;
				return false;
			}

			var textName = StringToTextName(name);
			var voiceName = StringToVoiceName(name);
			if (string.IsNullOrEmpty(textName) || string.IsNullOrEmpty(voiceName)
				|| !ChannelNameComparer.Default.Equals(RemoveChannelTypeSuffix(textName), RemoveChannelTypeSuffix(voiceName)))
			{
				error = ErrorCode.NameInvalid;
				return false;
			}

			if (textName.Length > MaxNameLength || voiceName.Length > MaxNameLength)
			{
				error = ErrorCode.NameTooLong;
				return false;
			}

			if (GetCategoryChannel() is not SocketCategoryChannel category)
			{
				error = ErrorCode.CategoryNotFound;
				return false;
			}
			if (category.Channels.Any(x => ChannelNameComparer.Default.Equals(x.Name, textName) || ChannelNameComparer.Default.Equals(x.Name, voiceName)))
			{
				error = ErrorCode.RoomCollision;
				return false;
			}

			error = ErrorCode.None;
			return true;
		}
		public async Task<RestTempRoomResult> CreateRoom(ParsedContextInfo contextInfo, string name, IEnumerable<SocketGuildUser>? additionalUsers = null)
		{
			if (!CanCreateRoom(name, out var error))
				return new RestTempRoomResult(error);

			var textName = StringToTextName(name);
			var voiceName = StringToVoiceName(name);

			if (guild == null || GetCategoryChannel() is not SocketCategoryChannel category)
				return new RestTempRoomResult(ErrorCode.CategoryNotFound);

			var voiceChannel = await guild.CreateVoiceChannelAsync(voiceName,
				x =>
				{
					x.CategoryId = category.Id;
				});
			var id = voiceChannel.Id;
			inProgress.Add(id);
			try
			{
				if (contextInfo.user != null)
					await contextInfo.user.ModifyAsync(x => x.Channel = voiceChannel);
				var textChannel = await category.Guild.CreateTextChannelAsync(textName,
					x =>
					{
						x.CategoryId = category.Id;
						x.Topic = "Don't get too attached.";
					});

				if (additionalUsers != null)
				{
					foreach (var user in additionalUsers)
					{
						try
						{
							await user.ModifyAsync(x => x.Channel = voiceChannel);
						}
						catch (Exception ex)
						{
							ConsoleUtil.WriteLine("Error moving user '" + user.Username + "':", ConsoleColor.Red);
							ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
						}
					}
				}
				return new RestTempRoomResult(voiceChannel, textChannel);
			}
			finally
			{
				inProgress.Remove(id);
			}
		}
		public SocketTempRoom? GetRoom(SocketGuildUser? owner)
		{
			if (owner == null)
				return null;

			if (GetCategoryChannel() is not SocketCategoryChannel category)
				return null;

			var guildConfig = Program.Config.GetGuildConfig(owner.Guild.Id);
			SocketVoiceChannel? voice = category.Channels.OfType<SocketVoiceChannel>().FirstOrDefault(x => owner.VoiceChannel != null && owner.VoiceChannel.Name == x.Name);
			SocketTextChannel? text = null;
			if (voice == null || guildConfig.IsTempChannelException(voice))
				return null;

			text = category.Channels.OfType<SocketTextChannel>().FirstOrDefault(x => ChannelNameComparer.Default.Equals(RemoveChannelTypeSuffix(voice.Name), RemoveChannelTypeSuffix(x.Name)));
			if (text == null || guildConfig.IsTempChannelException(text))
				return null;

			return new SocketTempRoom(voice, text);
		}
		public async Task<bool> RenameRoom(SocketGuildUser? owner, string name)
		{
			var room = GetRoom(owner);
			if (room == null)
				return false;
			
			if (room.voice != null)
				await room.voice.ModifyAsync(x => x.Name = StringToVoiceName(name));
			if (room.text != null)
				await room.text.ModifyAsync(x => x.Name = StringToTextName(name));
			return true;
		}

		public static string GetErrorLog(string channel, ErrorCode error)
		{
			switch (error)
			{
				case ErrorCode.NameEmpty:
					return "Room name '" + channel + "' is empty.";
				case ErrorCode.NameInvalid:
					return "Room name '" + channel + "' is invalid.";
				case ErrorCode.NameTooLong:
					return "Room name '" + channel + "' is too long.";
				case ErrorCode.RoomCollision:
					return "Room name '" + channel + "' is already taken.";
				case ErrorCode.CreationFailed:
					return "Room '" + channel + "' raised an unexpected error.";
			}
			return "Unexpected error creating room '" + channel + "'.";
		}
		public static string GetErrorResponse(ParsedContextInfo contextInfo, ErrorCode error)
		{
			switch (error)
			{
				case ErrorCode.NameEmpty:
					return contextInfo.mention + " Give your room a name.";
				case ErrorCode.NameInvalid:
					return contextInfo.mention + " Give your room a _normal_ name.";
				case ErrorCode.NameTooLong:
					return contextInfo.mention + " Give your room a _reasonably short_ name.";
				case ErrorCode.RoomCollision:
					return contextInfo.mention + " Give your room an _original_ name.";
				case ErrorCode.CreationFailed:
					return contextInfo.mention + " Really not feeling up to it right now, sorry.";
			}
			return contextInfo.mention + " Whoops, something went wrong.";
		}
	}
	public class RestTempRoomResult
	{
		public RestVoiceChannel? voice;
		public RestTextChannel? text;
		public TempRoomManager.ErrorCode error;

		public bool isValid { get { return error == TempRoomManager.ErrorCode.None; } }

		public string Name
		{
			get
			{
				if (voice == null || text == null)
					return "";

				return "'" + voice.Name + "'/'" + text.Name + "'";
			}
		}

		public RestTempRoomResult(TempRoomManager.ErrorCode error)
		{
			if (error != TempRoomManager.ErrorCode.None)
			{
				this.error = error;
			}
			else
			{
				this.error = TempRoomManager.ErrorCode.CreationFailed;
			}

			voice = null;
			text = null;
		}
		public RestTempRoomResult(RestVoiceChannel voice, RestTextChannel text)
		{
			if (voice != null && text != null)
			{
				this.voice = voice;
				this.text = text;

				error = TempRoomManager.ErrorCode.None;
			}
			else
			{
				this.voice = null;
				this.text = null;

				error = TempRoomManager.ErrorCode.CreationFailed;
			}
		}
		
		public async void Rename(string name)
		{
			if (text != null)
				await text.ModifyAsync(x => x.Name = TempRoomManager.StringToTextName(name));
			if (voice != null)
				await voice.ModifyAsync(x => x.Name = TempRoomManager.StringToVoiceName(name));
		}
	}
	public class SocketTempRoom
	{
		public SocketVoiceChannel voice;
		public SocketTextChannel text;

		public string Name
		{
			get
			{
				if (voice == null || text == null)
					return "";

				return "'" + voice.Name + "'/'" + text.Name + "'";
			}
		}

		public SocketTempRoom(SocketVoiceChannel voice, SocketTextChannel text)
		{
			this.voice = voice;
			this.text = text;
		}

		public async void Rename(string name)
		{
			await text.ModifyAsync(x => x.Name = TempRoomManager.StringToTextName(name));
			await voice.ModifyAsync(x => x.Name = TempRoomManager.StringToVoiceName(name));
		}
	}
}