using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Context;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlBot.Components;

namespace KewlBot.Modules
{
	[HelpInfo("rename", "For renaming temporary rooms.", HelpPriorities.Rename)]
	public class RenameModule : KewlBotModule
	{
		[SlashCommand("rename", "Rename a voice/text room created with **/room**.", false, RunMode.Async)]
		public async Task Rename
			(
				[Summary("name", "The new name for the voice channel.")]
				string name
			)
		{
			using (responseHandler)
			{
				try
				{
					var contextInfo = ParsedContextInfo.ParseContext(Context);
					if (!await PerformStandardContextResponse(contextInfo))
						return;

					var room = TempRoomManager.Get(Context.Guild).GetRoom(contextInfo.user);
					if (room == null)
					{
						ConsoleUtil.WriteLine("User '" + Context.User + "' doesn't have a room to rename.");
						await SendResponse(false, contextInfo.mention + " You don't even _have_ a room to rename.");
						return;
					}

					var textChannel = contextInfo.guild?.TextChannels.FirstOrDefault(x => x.Name == TempRoomManager.StringToTextName(name));
					var voiceChannel = contextInfo.guild?.VoiceChannels.FirstOrDefault(x => x.Name == TempRoomManager.StringToVoiceName(name));

					if (textChannel != null || voiceChannel != null)
					{
						ConsoleUtil.WriteLine("Room name '" + name + "' is already taken.");
						await SendResponse(false, contextInfo.mention + " Try to pick an _original_ name?");
						return;
					}

					ConsoleUtil.WriteLine("Renaming room " + room.Name + " to '" + name + "' by request of user '" + Context.User.Username + "'.");
					await SendResponse(true, "Renaming " + contextInfo.mention + "'s room to '" + name + "' now.");
					await TempRoomManager.Get(Context.Guild).RenameRoom(contextInfo.user, name);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while renaming temp room:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}