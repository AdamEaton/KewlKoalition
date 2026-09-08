using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KewlKommon.Components;
using KewlKommon.Interaction;
using KewlKommon.Utilities;
using KewlKommon.Context;

namespace KewlKommon.Core
{
	public abstract class KewlProgram
	{
		static KewlProgram? _Bot;
		public static KewlProgram Bot
		{
			get { return _Bot ?? throw new InvalidOperationException("Bot has not been initialized!"); }
			protected set { _Bot = value; }
		}
		public abstract KewlConfig configBase { get; }

		public abstract string programName { get; }
		public abstract string componentIdPrefix { get; }
		public abstract Color themeColor { get; }

		public abstract int featureVersion { get; }
		public abstract int patchVersion { get; }
		public string versionNumber { get { return string.Format("{0}.{1}.{2}.{3}", KewlUtil.MajorVersion, KewlUtil.MinorVersion, featureVersion, patchVersion); } }
		
		public virtual Requirement defaultContextRequirements { get { return Requirement.None; } }
		public virtual Requirement componentContextRequirements { get { return defaultContextRequirements; } }
		public virtual Requirement menuContextRequirements { get { return defaultContextRequirements; } }

		public string? ComponentNameToId(string? componentName)
		{
			if (string.IsNullOrEmpty(componentName))
				return null;

			return componentIdPrefix + componentName;
		}
		public string? ComponentIdToName(string? componentId)
		{
			if (string.IsNullOrEmpty(componentId))
				return null;

			if (!componentId.StartsWith(componentIdPrefix))
				return null;

			return componentId[componentIdPrefix.Length..];
		}

		public DiscordSocketClient client { get; private set; }
		protected readonly InteractionService interactions;
		protected readonly ServiceProvider serviceProvider;

		public IEnumerable<SocketGuild> GetGuilds()
		{
			foreach (var id in configBase.guildIds)
				if (GetGuild(id) is SocketGuild guild)
					yield return guild;
		}
		public SocketGuild? GetGuild(ulong id)
		{
			return client.Guilds.FirstOrDefault(x => x.Id == id);
		}
		public SocketGuildUser GetSelfUser(SocketGuild guild)
		{
			return guild.GetUser(client.CurrentUser.Id);
		}

		public bool initialized { get; private set; } = false;
		public bool shuttingDown { get; private set; } = false;
		protected readonly HashSet<ShutDownLock> shutdownLocks = [];
		public void ShutDown()
		{
			shuttingDown = true;
		}

		public KewlProgram()
		{
			var config = new DiscordSocketConfig();
			config.GatewayIntents &= ~GatewayIntents.GuildScheduledEvents;
			config.GatewayIntents &= ~GatewayIntents.GuildInvites;
			config.GatewayIntents |= GatewayIntents.GuildMembers;
			config.GatewayIntents |= GatewayIntents.MessageContent;
			config.AlwaysDownloadUsers = true;
			config.EnableVoiceDaveEncryption = true;
			config.HandlerTimeout = 10000;

			client = new DiscordSocketClient(config);
			interactions = new InteractionService(client);

			var collection = new ServiceCollection();
			collection.AddSingleton(config);
			collection.AddSingleton(client);
			serviceProvider = collection.BuildServiceProvider();

			Discord.LibDave.Dave.SetLogSink(DaveLogSink);
			client.Log += _OnClientLog;
			client.Ready += _OnClientReady;
			client.MessageReceived += _OnClientMessageReceived;
			client.InteractionCreated += _OnClientInteractionCreated;
			client.ButtonExecuted += _OnClientButtonExecuted;
			client.SelectMenuExecuted += _OnClientSelectMenuExecuted;
		}

		static void DaveLogSink(Discord.LibDave.Binding.LoggingSeverity severity, string file, int line, string message)
		{

		}

		protected abstract Task Run(string[] args);

		protected virtual Task OnInitialize() { return Task.CompletedTask; }
		protected virtual Task OnStartShutdown() { return Task.CompletedTask; }
		protected virtual Task OnCleanUp() { return Task.CompletedTask; }

		protected virtual Task OnClientMessage(SocketUserMessage message) { return Task.CompletedTask; }

		private async Task Initialize()
		{
			if (initialized)
				return;

			var kkAssembly = typeof(KewlProgram).Assembly;
			var entryAssembly = Assembly.GetEntryAssembly();
			await interactions.AddModulesAsync(kkAssembly, serviceProvider);
			if (entryAssembly != kkAssembly)
				await interactions.AddModulesAsync(entryAssembly, serviceProvider);

			foreach (var guildId in configBase.guildIds)
				await interactions.RegisterCommandsToGuildAsync(guildId);

			await OnInitialize();

			ConsoleUtil.WriteLine("Connected as '" + client.CurrentUser.Username + "'.");

			initialized = true;

			AddRegisteredListeners();
			StartMonitors();
		}

		static void AddRegisteredListeners()
		{
			ReflectionUtil.InvokeStaticOverrides(typeof(IRegisteredListener), nameof(IRegisteredListener.AddRegisteredListener));
		}
		static void RemoveRegisteredListeners()
		{
			ReflectionUtil.InvokeStaticOverrides(typeof(IRegisteredListener), nameof(IRegisteredListener.RemoveRegisteredListener));
		}
		static void StartMonitors()
		{
			foreach (var type in ReflectionUtil.GetImplementingTypes(typeof(IPerGuildMonitor))
				.Where(x => x.IsAssignableTo(typeof(IPerGuildMonitor))))
			{
				foreach (var id in Bot.configBase.guildIds)
				{
					if (KewlUtil.GetSingletonForGuild(type, id) is IPerGuildMonitor monitor)
						monitor.Monitor();
				}
			}
		}

		private Task _OnClientLog(LogMessage log)
		{
			ConsoleUtil.WriteLine(log.ToString(prependTimestamp: false));
			return Task.CompletedTask;
		}
		private async Task _OnClientReady()
		{
			await Initialize();
		}
		private Task _OnClientMessageReceived(SocketMessage messageData)
		{
			if (messageData is not SocketUserMessage message)
				return Task.CompletedTask;

			return OnClientMessage(message);
		}
		private async Task _OnClientInteractionCreated(SocketInteraction interaction)
		{
			var context = new SocketInteractionContext(client, interaction);
			await interactions.ExecuteCommandAsync(context, serviceProvider);
		}
		private Task _OnClientButtonExecuted(SocketMessageComponent button)
		{
			try
			{
				var contextInfo = ParsedContextInfo.ParseContext(button, componentContextRequirements);
				EventUtil.Dispatch(new ButtonClickedEvent(contextInfo));
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error responding to button click:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex);
			}
			return Task.CompletedTask;
		}
		private Task _OnClientSelectMenuExecuted(SocketMessageComponent menu)
		{
			try
			{
				var contextInfo = ParsedContextInfo.ParseContext(menu, menuContextRequirements);
				EventUtil.Dispatch(new MenuSelectedEvent(contextInfo));
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error responding to select menu:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex);
			}
			return Task.CompletedTask;
		}

		public class ShutDownLock : IDisposable
		{
			KewlProgram bot;

			public ShutDownLock(KewlProgram bot)
			{
				this.bot = bot;
				bot.shutdownLocks.Add(this);
			}
			public void Dispose()
			{
				GC.SuppressFinalize(this);
				bot.shutdownLocks.Remove(this);
			}
		}
	}
	public abstract class KewlProgram<TProgram, TConfig> : KewlProgram
		where TProgram : KewlProgram<TProgram, TConfig>, new()
		where TConfig : KewlConfig, new()
	{
		public static new TProgram Bot
		{
			get { return KewlProgram.Bot as TProgram ?? throw new InvalidOperationException("Bot has not been initialized!"); }
			protected set { KewlProgram.Bot = value; }
		}
		public override KewlConfig configBase
		{
			get
			{
				return config;
			}
		}
		public static TConfig Config
		{
			get { return Bot.config; }
		}

		TConfig? _config = null;
		public TConfig config
		{
			get { return _config ?? throw new InvalidOperationException("Bot config has not been initialized!"); }
			protected set { _config = value; }
		}

		protected static void StartUp(string[] args)
		{
			Bot = new TProgram();
			Bot.Run(args).GetAwaiter().GetResult();
		}

		protected sealed override async Task Run(string[] args)
		{
			ConsoleUtil.WriteLine(programName + " version " + versionNumber);

			string? path = string.IsNullOrEmpty(args.FirstOrDefault()) ? KewlConfig.DefaultName : args.FirstOrDefault();
			config = new TConfig();
			while (true)
			{
				ConsoleUtil.WriteLine("Loading configuration '" + path + "'...");
				if (config.Load(path))
					break;

				ConsoleUtil.WriteLine("Couldn't load configuration.", ConsoleColor.Red);
				ConsoleUtil.WriteLine("Please enter the name of the configuration file:");
				path = ConsoleUtil.ReadLine();
			}

			ConsoleUtil.WriteLine("Config loaded.");

			await client.LoginAsync(TokenType.Bot, config.discordToken);
			await client.StartAsync();

			while (!shuttingDown)
				await Task.Delay(100);

			ConsoleUtil.WriteLine("Preparing for shutdown...");
			await OnStartShutdown();

			while (shutdownLocks.Count > 0)
			{
				ConsoleUtil.WriteLine("Waiting on " + shutdownLocks.Count + " outstanding actions...");
				await Task.Delay(100);
			}

			ConsoleUtil.WriteLine("Cleaning up...");
			await OnCleanUp();

			await client.LogoutAsync();
			await Task.Delay(1000);
		}
	}
}