using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Utilities;

namespace KewlKommon.Modules
{
	public abstract class KewlModule : InteractionModuleBase<SocketInteractionContext>
	{
		InteractionResponseHandler? _responseHandler = null;
		protected InteractionResponseHandler responseHandler
		{
			get
			{
				_responseHandler ??= new InteractionResponseHandler(this);

				return _responseHandler;
			}
		}

		public async Task SendReply(string? message = null, Embed? embed = null, MessageComponent? components = null)
		{
			await responseHandler.SendReply(message, embed, components);
		}

		public async Task SendResponse(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			await responseHandler.SendResponse(visible, message, embed, components, filePath);
		}
		public async Task DeferResponse(bool visible)
		{
			await responseHandler.DeferResponse(visible);
		}
		public async Task SetDeferredResponse(string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			await responseHandler.SetDeferredResponse(message, embed, components, filePath);
		}
		public async Task ModifyResponse(string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			await responseHandler.ModifyResponse(message, embed, components, filePath);
		}
		public async Task SetResponse(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			await responseHandler.SetResponse(visible, message, embed, components, filePath);
		}

		public async Task SendFollowup(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			await responseHandler.SendFollowup(visible, message, embed, components, filePath);
		}

		public async Task PerformStandardAcknowledgement(bool visible)
		{
			await responseHandler.PerformStandardAcknowledgement(visible);
		}
		public async Task<bool> PerformStandardContextResponse(ParsedContextInfo contextInfo, ResponseMode responseMode = ResponseMode.Response)
		{
			return await responseHandler.PerformStandardContextResponse(contextInfo, responseMode);
		}

		public class InteractionResponseHandler : ResponseHandler<KewlModule>
		{
			public InteractionResponseHandler(KewlModule module)
				: base(module) { }

			public override SocketInteraction interaction { get { return target.Context.Interaction; } }

			protected override SocketGuildUser GetUser() { return target.Context.Guild.GetUser(target.Context.User.Id); }

			protected override async Task<IUserMessage> Reply(string? message, Embed? embed, MessageComponent? components)
			{
				return await target.ReplyAsync(text: message, embed: embed, components: components);
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
				await target.DeferAsync(!visible);
			}
			protected override async Task<IUserMessage> Modify(string? message, Embed? embed, MessageComponent? components, string? filePath)
			{
				return await target.ModifyOriginalResponseAsync(
					(x) =>
					{
						x.Content = message;
						x.Embed = embed;
						x.Components = components;
						x.Attachments = string.IsNullOrEmpty(filePath) ? null : new FileAttachment[] { new FileAttachment(filePath) };
					});
			}
			protected override async Task<IUserMessage> Followup(bool visible, string? message, Embed? embed, MessageComponent? components, string? filePath)
			{
				if (string.IsNullOrEmpty(filePath))
					return await target.FollowupAsync(text: message, embed: embed, ephemeral: !visible, components: components);
				else
					return await target.FollowupWithFileAsync(filePath, text: message, embed: embed, components: components, ephemeral: !visible);
			}
			
			protected override async Task OnDisposeNew()
			{
				ConsoleUtil.WriteLine("Interaction response handler unexpectedly terminated (state: New).", ConsoleColor.Red);
				await SendResponse(false, "Something went super wrong.");
			}
			protected override async Task OnDisposeDeferred()
			{
				ConsoleUtil.WriteLine("Interaction response handler unexpectedly terminated (state: Deferred).", ConsoleColor.Red);
				await SendResponse(false, "Something went super wrong.");
			}
			protected override Task OnDisposeResponded()
			{
				return Task.CompletedTask;
			}
		}
	}
}