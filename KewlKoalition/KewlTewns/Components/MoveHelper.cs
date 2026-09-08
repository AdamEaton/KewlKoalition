using System;
using System.Linq;
using Discord.WebSocket;
using KewlKommon.Components;
using KewlKommon.Utilities;

namespace KewlTewns.Components
{
	public class MoveHelper : PerGuildSingleton<MoveHelper>, IRegisteredListener
	{
		public static void AddListeners()
		{
			EventUtil.AddListener<BotMentionReceivedEvent>(OnBotMentionReceived);
		}
		public static void RemoveListeners()
		{
			EventUtil.RemoveListener<BotMentionReceivedEvent>(OnBotMentionReceived);
		}

		static void OnBotMentionReceived(BotMentionReceivedEvent e)
		{
			try
			{
				var mover = Get(e.guild);

				ConsoleUtil.WriteLine("Bot mention received!");

				if (e.message.Source != Discord.MessageSource.Bot)
				{
					ConsoleUtil.WriteLine("Mention is not from another bot - ignoring.");
					return;
				}

				if (e.message.MentionedChannels.Count <= 0)
				{
					ConsoleUtil.WriteLine("Suspending playback before move.");
					mover.Suspend();
				}
				else
				{
					ConsoleUtil.WriteLine("Resuming playback after move.");
					mover.Resume(e.message.MentionedChannels.OfType<SocketVoiceChannel>().First());
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing 'move' command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return;
			}
		}

		static bool InTransit = false;

		void Suspend()
		{
			if (InTransit)
				return;

			Get<PlaybackController>().Suspend();
			InTransit = true;
		}
		void Resume(SocketVoiceChannel channel)
		{
			if (!InTransit)
				return;

			Get<PlaybackController>().Reinstate(channel);
			InTransit = false;
		}
	}

	public class BotMentionReceivedEvent : EventInstance
	{
		public SocketGuild guild;
		public SocketUserMessage message;

		public BotMentionReceivedEvent(SocketGuild guild, SocketUserMessage message)
		{
			this.guild = guild;
			this.message = message;
		}
	}
}
