using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlBot.Components;

namespace KewlBot.Modules
{
	[HelpInfo("room", "For creating temporary rooms.", HelpPriorities.Room)]
	public class RoomModule : KewlBotModule
	{
		[SlashCommand("room", "Create a temporary text and voice channel pair with the specified name.", false, RunMode.Async)]
		public async Task Room
			(
				[Summary("channel", "The name of the room.")]
				string channel
			)
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					if (contextInfo.user?.VoiceChannel == null)
					{
						ConsoleUtil.WriteLine("User is not in a voice channel.");
						await SendResponse(false, contextInfo.mention + " Join a voice room first.");
						return;
					}

					if (!TempRoomManager.Get(Context.Guild).CanCreateRoom(channel, out var error))
					{
						ConsoleUtil.WriteLine(TempRoomManager.GetErrorLog(channel, error));
						await SendResponse(false, TempRoomManager.GetErrorResponse(contextInfo, error));
						return;
					}

					await DeferResponse(true);

					var room = await TempRoomManager.Get(Context.Guild).CreateRoom(contextInfo, channel);
					if (!room.isValid)
					{
						ConsoleUtil.WriteLine(TempRoomManager.GetErrorLog(channel, error));
						await SetDeferredResponse(TempRoomManager.GetErrorResponse(contextInfo, error));
						return;
					}

					ConsoleUtil.WriteLine("Creation of room " + room.Name + " by request of user '" + contextInfo.user.Username + "' complete.");
					await SetDeferredResponse(contextInfo.user.Mention + " You've been added to the requested voice channel '" + room.voice?.Mention + "', and the temporary text channel '" + room.text?.Mention + "' is available as well.\n\n These channels will stay open as long as someone is in the voice room.");
					return;
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while creating room '" + channel + "':", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}