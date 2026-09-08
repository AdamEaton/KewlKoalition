using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KewlKommon.Context;
using KewlKommon.Extensions;
using KewlKommon.Utilities;

namespace KewlKommon.Interaction
{
	public class MessageComponentResponseHandler : ResponseAcknowledger<SocketMessageComponent>
	{
		public MessageComponentResponseHandler(SocketMessageComponent module)
			: base(module)
		{
			SocketMessageComponentExt.RegisterHandler(this);
		}

		public override SocketInteraction interaction { get { return target; } }

		protected SocketTextChannel GetChannel()
		{
			return target.GetChannel() ?? throw new InvalidOperationException("MessageComponent's channel could not be determined.");
		}
		protected override SocketGuildUser GetUser()
		{
			return target.GetUser() ?? throw new InvalidOperationException("MessageComponent's user could not be determined.");
		}

		protected override async Task Acknowledge()
		{
			await target.DeferAsync();
		}
		protected override async Task<IUserMessage> Reply(string? message, Embed? embed, MessageComponent? components)
		{
			return await GetChannel().SendMessageAsync(text: message, embed: embed, components: components);
		}
		protected override async Task<IUserMessage> Respond(bool visible, string? message, Embed? embed, MessageComponent? components, string? filePath)
		{
			if (string.IsNullOrEmpty(filePath))
				await target.RespondAsync(text: message, embed: embed, components: components, ephemeral: !visible);
			else
				await target.RespondWithFileAsync(filePath, text: message, embed: embed, components: components, ephemeral: !visible);
			return await target.GetOriginalResponseAsync();
		}
		protected override async Task Defer(bool visible)
		{
			await target.DeferLoadingAsync(!visible);
		}
		protected override async Task<IUserMessage> Modify(string? message, Embed? embed, MessageComponent? components, string? filePath)
		{
			var response = await target.ModifyOriginalResponseAsync(
				(x) =>
				{
					x.Content = message;
					x.Embed = embed;
					x.Components = components;
					x.Attachments = string.IsNullOrEmpty(filePath) ? null : new FileAttachment[] { new FileAttachment(filePath) };
				});
			return response;
		}
		protected override async Task<IUserMessage> Followup(bool visible, string? message, Embed? embed, MessageComponent? components, string? filePath)
		{
			SocketMessageComponentExt.DisposeHandler(this);
			if (currentState == State.Deferred)
				currentState = State.Responded;
			if (string.IsNullOrEmpty(filePath))
				return await target.FollowupAsync(text: message, embed: embed, components: components, ephemeral: !visible);
			else
				return await target.FollowupWithFileAsync(filePath, text: message, embed: embed, components: components, ephemeral: !visible);
		}

		protected override async Task OnDisposeNew()
		{
			SocketMessageComponentExt.DisposeHandler(this);
			ConsoleUtil.WriteLine("Component response handler unexpectedly terminated (state: New).", ConsoleColor.Red);
			await SendResponse(false, "Something went super wrong.");
		}
		protected override async Task OnDisposeDeferred()
		{
			SocketMessageComponentExt.DisposeHandler(this);
			ConsoleUtil.WriteLine("Component response handler unexpectedly terminated (state: Deferred).", ConsoleColor.Red);
			await SendResponse(false, "Something went super wrong.");
		}
		protected override Task OnDisposeResponded()
		{
			SocketMessageComponentExt.DisposeHandler(this);
			return Task.CompletedTask;
		}
	}

	public class MessageComponentInteractionEvent : EventInstance
	{
		public ParsedMessageComponentContextInfo contextInfo;

		public MessageComponentInteractionEvent(ParsedMessageComponentContextInfo contextInfo)
		{
			this.contextInfo = contextInfo;
		}
	}
	public class ButtonClickedEvent : MessageComponentInteractionEvent
	{
		public ButtonClickedEvent(ParsedMessageComponentContextInfo contextInfo)
			: base(contextInfo) { }
	}
	public class MenuSelectedEvent : MessageComponentInteractionEvent
	{
		public MenuSelectedEvent(ParsedMessageComponentContextInfo contextInfo)
			: base(contextInfo) { }
	}
}