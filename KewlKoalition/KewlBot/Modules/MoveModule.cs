using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KewlKommon.Context;
using KewlKommon.Extensions;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlBot.Components;

namespace KewlBot.Modules
{
	[HelpInfo("move", "For relocating conversations. (Admins only)", HelpPriorities.Move)]
	public class MoveModule : KewlBotModule
	{
		[SlashCommand("move", "Move all the members of one voice room into another. (Admins only)", false, RunMode.Async)]
		public async Task Move
			(
				[Summary("channel", "The name of the existing or new (temporary) voice channel to move to.")]
				string channel
			)
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context, Requirement.Admin);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					var voiceChannel = contextInfo.user?.VoiceChannel;
					if (voiceChannel == null)
					{
						ConsoleUtil.WriteLine("User is not in a voice channel.");
						await SendResponse(false, contextInfo.mention + " Join a voice room first.");
						return;
					}

					var target = contextInfo.guild?.VoiceChannels.FirstOrDefault(x => x.Name.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
						?? contextInfo.guild?.VoiceChannels.FirstOrDefault(x => x.Name.Equals(TempRoomManager.StringToVoiceName(channel), StringComparison.InvariantCultureIgnoreCase));
					if (target == null)
					{
						if (!TempRoomManager.Get(Context.Guild).CanCreateRoom(channel, out var error))
						{
							ConsoleUtil.WriteLine(TempRoomManager.GetErrorLog(channel, error));
							await SendResponse(false, TempRoomManager.GetErrorResponse(contextInfo, error));
							return;
						}
					}

					await DeferResponse(true);

					var users = new List<SocketGuildUser>(voiceChannel.ConnectedUsers);
					bool doBotStuff = users.Any(KewlExt.IsBot);
					var role = contextInfo.guild?.GetSpecialRole(KewlUtil.BotRoleKey);

					if (doBotStuff && role != null)
						await SendReply(role.Mention + " Road trip!");

					int counter = 50;
					while (users.Any(KewlExt.IsBot) && counter-- > 0)
					{
						await Task.Delay(100);
						users = [.. voiceChannel.ConnectedUsers];
					}

					if (target == null)
					{
						var room = await TempRoomManager.Get(Context.Guild).CreateRoom(contextInfo, channel, users.Where(x => x != contextInfo.user && !x.IsBot()));
						if (room == null)
						{
							ConsoleUtil.WriteLine("Creation of room '" + channel + "' failed.");
							await SetDeferredResponse(contextInfo.mention + " Whoops, something went wrong.");
							return;
						}
						else if (!room.isValid)
						{
							ConsoleUtil.WriteLine(TempRoomManager.GetErrorLog(channel, room.error));
							await SetDeferredResponse(TempRoomManager.GetErrorResponse(contextInfo, room.error));
							return;
						}

						target = contextInfo.guild?.VoiceChannels.FirstOrDefault(x => x.Name == room.voice?.Name);

						ConsoleUtil.WriteLine("Creation of room '" + room.Name + "' by request of user '" + contextInfo.username + "' complete.");
						await SetDeferredResponse(contextInfo.mention + " Your krew has been moved to the requested voice channel '" + room.voice?.Mention + "', and the temporary text channel '" + room.text?.Mention + "' is available as well.\n\n These channels will stay open as long as someone is in the voice room.");
						if (doBotStuff && role != null && room.voice != null)
							await SendReply(role.Mention + " We're here! " + room.voice.Mention);
						return;
					}

					await SetDeferredResponse(contextInfo.mention + " ... and _awaaaaay_ we go!");
					if (doBotStuff && role != null)
						await SendReply(role.Mention + " We're here! " + target.Mention);

					foreach (var user in users.Where(x => !x.IsBot()))
					{
						try
						{
							await user.ModifyAsync(x => x.Channel = target);
						}
						catch (Exception ex)
						{
							ConsoleUtil.WriteLine("Error while moving user:", ConsoleColor.Red);
							ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
						}
					}

					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'move' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}