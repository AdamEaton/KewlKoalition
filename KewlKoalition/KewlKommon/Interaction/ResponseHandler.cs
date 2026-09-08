using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KewlKommon.Context;
using KewlKommon.Utilities;

namespace KewlKommon.Interaction
{
	public interface IResponseHandler : IDisposable
	{
		Task<IUserMessage> SendReply(string? message = null, Embed? embed = null, MessageComponent? components = null);

		Task<IUserMessage> SendResponse(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null);
		Task DeferResponse(bool visible);
		Task<IUserMessage> SetDeferredResponse(string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null);
		Task<IUserMessage> ModifyResponse(string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null);
		Task<IUserMessage> SetResponse(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null);

		Task<IUserMessage> SendFollowup(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null);

		Task PerformResponse(ResponseMode responseMode, bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null);

		Task PerformStandardAcknowledgement(bool visible);
		Task<bool> PerformStandardContextResponse(ParsedContextInfo contextInfo, ResponseMode responseMode = ResponseMode.Response);

		SocketInteraction interaction { get; }
	}
	public abstract class ResponseHandler<T> : IResponseHandler
	{
		public T target { get; protected set; }
		public ResponseHandler(T target)
		{
			this.target = target;
			currentState = State.New;
		}
		protected State currentState = State.New;

		protected static string? LimitSize(string? message)
		{
			if (message == null)
				return null;

			if (message.Length > KewlUtil.MaxMessageLength)
				return message[..KewlUtil.MaxMessageLength];
			return message;
		}

		public abstract SocketInteraction interaction { get; }

		protected abstract SocketGuildUser GetUser();

		protected abstract Task<IUserMessage> Reply(string? message, Embed? embed, MessageComponent? components);
		protected abstract Task<IUserMessage> Respond(bool visible, string? message, Embed? embed, MessageComponent? components, string? filePath);
		protected abstract Task Defer(bool visible);
		protected abstract Task<IUserMessage> Modify(string? message, Embed? embed, MessageComponent? components, string? filePath);
		protected abstract Task<IUserMessage> Followup(bool visible, string? message, Embed? embed, MessageComponent? components, string? filePath);

		protected abstract Task OnDisposeNew();
		protected abstract Task OnDisposeDeferred();
		protected abstract Task OnDisposeResponded();

		public async Task<IUserMessage> SendReply(string? message = null, Embed? embed = null, MessageComponent? components = null)
		{
			message = LimitSize(message);

			return await Reply(message, embed, components);
		}

		public async Task<IUserMessage> SendResponse(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			message = LimitSize(message);

			switch (currentState)
			{
				case State.New:
					currentState = State.Responded;
					return await Respond(visible, message, embed, components, filePath);
				case State.Responded:
					ConsoleUtil.WriteLine("Response already sent - modifying.", ConsoleColor.Yellow);
					goto case State.Deferred;
				case State.Deferred:
					currentState = State.Responded;
					return await Modify(message, embed, components, filePath);
			}
			return await Modify(message, embed, components, filePath);
		}
		public async Task DeferResponse(bool visible)
		{
			switch (currentState)
			{
				case State.New:
					await Defer(visible);
					currentState = State.Deferred;
					break;
				case State.Deferred:
					ConsoleUtil.WriteLine("Response already deferred - ignoring.", ConsoleColor.Yellow);
					break;
				case State.Responded:
					ConsoleUtil.WriteLine("Response already sent - not deferring.", ConsoleColor.Yellow);
					break;
			}
		}
		public async Task<IUserMessage> SetDeferredResponse(string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			message = LimitSize(message);

			switch (currentState)
			{
				case State.New:
					ConsoleUtil.WriteLine("Response not deferred - sending original response as ephemeral.", ConsoleColor.Yellow);
					return await SendResponse(false, message, embed, components, filePath);
				case State.Responded:
					ConsoleUtil.WriteLine("Response already sent - modifying.", ConsoleColor.Yellow);
					goto case State.Deferred;
				case State.Deferred:
					currentState = State.Responded;
					return await Modify(message, embed, components, filePath);
			}
			return await interaction.GetOriginalResponseAsync();
		}
		public async Task<IUserMessage> ModifyResponse(string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			message = LimitSize(message);

			switch (currentState)
			{
				case State.New:
					ConsoleUtil.WriteLine("Response not yet sent - sending original response.", ConsoleColor.Yellow);
					return await SendResponse(false, message, embed, components, filePath);
				case State.Deferred:
					ConsoleUtil.WriteLine("Response not yet sent, only deferred - sending deferred response.", ConsoleColor.Yellow);
					return await SetDeferredResponse(message, embed, components, filePath);
				case State.Responded:
					return await Modify(message, embed, components, filePath);
			}
			return await interaction.GetOriginalResponseAsync();
		}
		public async Task<IUserMessage> SetResponse(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			message = LimitSize(message);

			switch (currentState)
			{
				case State.New:
					return await SendResponse(visible, message, embed, components, filePath);
				case State.Deferred:
					return await SetDeferredResponse(message, embed, components, filePath);
				case State.Responded:
					return await ModifyResponse(message, embed, components, filePath);
			}
			return await interaction.GetOriginalResponseAsync();
		}

		public async Task<IUserMessage> SendFollowup(bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			message = LimitSize(message);

			return await Followup(visible, message, embed, components, filePath);
		}

		public async Task PerformResponse(ResponseMode responseMode, bool visible, string? message = null, Embed? embed = null, MessageComponent? components = null, string? filePath = null)
		{
			switch (responseMode)
			{
				case ResponseMode.Response:
					await SendResponse(visible, message, embed, components, filePath);
					break;
				case ResponseMode.Followup:
					await SendFollowup(visible, message, embed, components, filePath);
					break;
				case ResponseMode.Reply:
					if (!string.IsNullOrEmpty(filePath))
						ConsoleUtil.WriteLine("Ignoring filePath in 'PerformResponse' because responseMode is 'Reply', which does not support attachments.", ConsoleColor.Yellow);
					await SendReply(message, embed, components);
					break;
			}
		}

		public async Task PerformStandardAcknowledgement(bool visible)
		{
			await SendResponse(visible, GetUser().Mention + " Processing...");
		}
		public async Task<bool> PerformStandardContextResponse(ParsedContextInfo contextInfo, ResponseMode responseMode = ResponseMode.Response)
		{
			if (contextInfo.errorCode == ParsedContextInfo.ErrorCode.None)
				return true;

			ConsoleUtil.WriteLine(KewlUtil.GetStandardContextLog(contextInfo));
			await PerformResponse(responseMode, false, KewlUtil.GetStandardContextReply(contextInfo));
			return false;
		}

		public virtual async void Dispose()
		{
			GC.SuppressFinalize(this);
			switch (currentState)
			{
				case State.New:
					await OnDisposeNew();
					break;
				case State.Deferred:
					await OnDisposeDeferred();
					break;
				case State.Responded:
					await OnDisposeResponded();
					break;
			}
			currentState = State.Responded;
		}

		public enum State
		{
			New,
			Deferred,
			Responded,
		}
	}
	public interface IResponseAcknowledger : IResponseHandler
	{
		Task SendAcknowledgement();
	}
	public abstract class ResponseAcknowledger<T> : ResponseHandler<T>, IResponseAcknowledger
	{
		public ResponseAcknowledger(T target)
			: base(target) { }

		public async Task SendAcknowledgement()
		{
			switch (currentState)
			{
				case State.New:
					await Acknowledge();
					currentState = State.Responded;
					break;
				case State.Deferred:
					ConsoleUtil.WriteLine("Response already deferred - not acknowledging.", ConsoleColor.Yellow);
					break;
				case State.Responded:
					ConsoleUtil.WriteLine("Response already sent - not deferring.", ConsoleColor.Yellow);
					break;
			}
		}

		protected abstract Task Acknowledge();
	}
	public enum ResponseMode
	{
		Response,
		Followup,
		Reply,
	}
}
