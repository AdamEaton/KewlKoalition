using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Xabe.FFmpeg.Downloader;
using KewlKommon.Context;
using KewlKommon.Core;
using KewlKommon.Extensions;
using KewlKommon.Utilities;
using KewlTewns.Components;
using KewlTewns.Context;

namespace KewlTewns.Core
{
	public class Program : KewlProgram<Program, KewlConfig<GuildConfig>>
	{
		public override string programName { get { return "Kewl Tewns"; } }
		public override string componentIdPrefix { get { return "KewlTewns"; } }
		public override Color themeColor { get { return new Color(0x484050); } }

		public override int featureVersion { get { return 0; } }
		public override int patchVersion { get { return 0; } }

		public override Requirement defaultContextRequirements { get { return TewnsRequirement.Dj | Requirement.VoiceConnection; } }

		static void Main(string[] args)
		{
			StartUp(args);
		}

		protected override async Task OnInitialize()
		{
			SheetsService.Initialize();
			await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Full);
		}
		protected override async Task OnStartShutdown()
		{
			var tasks = new List<Task>();
			foreach (var player in PlaybackController.Instances)
			{
				tasks.Add(player.LeaveAsync());
			}
			await Task.WhenAll(tasks);
		}

		protected override async Task OnClientMessage(SocketUserMessage message)
		{
			try
			{
				var botRole = message.MentionedRoles.FirstOrDefault(x => x.IsSpecialRole(KewlUtil.BotRoleKey));
				if (botRole == null)
					return;

				var guildUser = botRole.Guild.GetUser(message.Author.Id);
				if (guildUser == null)
					return;

				if (!guildUser.IsBot())
					return;

				EventUtil.Dispatch(new BotMentionReceivedEvent(botRole.Guild, message));

				await Task.CompletedTask;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error responding to message:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
			}
		}
	}
}