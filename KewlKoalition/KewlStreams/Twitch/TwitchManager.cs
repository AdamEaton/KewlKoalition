using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using KewlKommon.Utilities;
using KewlStreams.Core;

namespace KewlStreams.Twitch
{
	public static class TwitchAuthenticator
	{
		static List<string> Scopes = [];

		public static async Task<(string accessToken, string refreshToken)> GetTokens(TwitchAPI api)
		{
			if (Program.Bot.config.twitchClientId is not string clientId)
				throw new InvalidOperationException("Unable to authorize with Twitch (no client ID).");
			if (Program.Bot.config.twitchClientSecret is not string clientSecret)
				throw new InvalidOperationException("Unable to authorize with Twitch (no client secret).");
			if (Program.Bot.config.twitchRedirectUri is not string redirectUri)
				throw new InvalidOperationException("Unable to authorize with Twitch (no redirect URI).");

			api.Settings.ClientId = clientId;
			var server = new WebServer(redirectUri);
			ConsoleUtil.WriteLine("Authorize here:", ConsoleColor.Yellow);
			ConsoleUtil.WriteLine(GetAuthorizationCodeURL(clientId, redirectUri, Scopes), ConsoleColor.Yellow);

			try
			{
				if (await server.Listen() is not Authorization auth)
					throw new InvalidOperationException("Bot authorization failed.");
				var response = await api.Auth.GetAccessTokenFromCodeAsync(auth.code, clientSecret, redirectUri);
				ConsoleUtil.WriteLine("Authenticated!");
				return (response.AccessToken, response.RefreshToken);
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while authorizing bot:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return (string.Empty, string.Empty);
			}
		}
		public static async Task<string> RefreshToken(TwitchAPI api)
		{
			var response = await api.Auth.RefreshAuthTokenAsync(Program.Bot.config.botRefreshToken, Program.Bot.config.twitchClientSecret);
			return response.AccessToken;
		}

		static string GetAuthorizationCodeURL(string clientID, string redirectURI, List<string> scopes)
		{
			var scopesString = string.Join("+", scopes);

			return "https://id.twitch.tv/oauth2/authorize?"
				+ $"client_id={clientID}&"
				+ $"redirect_uri={System.Web.HttpUtility.UrlEncode(redirectURI)}&"
				+ $"response_type=code&"
				+ $"scope={scopesString}";
		}

		class Authorization
		{
			public string code;

			public Authorization(string code)
			{
				this.code = code;
			}
		}
		class WebServer
		{
			HttpListener listener;

			public WebServer(string uri)
			{
				listener = new HttpListener();
				listener.Prefixes.Add(uri);
			}

			public async Task<Authorization?> Listen()
			{
				listener.Start();

				while (listener.IsListening)
				{
					var context = await listener.GetContextAsync();
					var request = context.Request;
					var response = context.Response;

					using (var writer = new StreamWriter(response.OutputStream))
					{
						if (request.QueryString.AllKeys.OfType<string>().Any("code".Contains) && request.QueryString["code"] is string code)
						{
							writer.WriteLine("Authenticated!");
							writer.Flush();
							return new Authorization(code);
						}
						else
						{
							writer.WriteLine("No code found in query string.");
							writer.Flush();
						}
					}
				}

				return null;
			}
		}
	}
	public static class TwitchManager
	{
		static TwitchAPI Api = new TwitchAPI();

		static HashSet<ulong> ActiveBroadcasters = [];
		public static IEnumerable<ulong> GetActiveBroadcasters()
		{
			foreach (var channel in ActiveBroadcasters)
				yield return channel;
		}

		public static void Init()
		{
			CheckLoop();
		}

		public static async Task<StreamInfo> GetTwitchInfo(ulong channelId)
		{
			while (true)
			{
				try
				{
					var id = channelId.ToString();

					var channelInfo = (await Api.Helix.Channels.GetChannelInformationAsync(id)).Data.First();
					var streamInfo = (await Api.Helix.Streams.GetStreamsAsync(userIds: [id])).Streams.First();
					var userInfo = (await Api.Helix.Users.GetUsersAsync(ids: [id])).Users.First();

					return new StreamInfo(channelInfo.BroadcasterName, channelInfo.Title, channelInfo.GameName, streamInfo.StartedAt, userInfo.ProfileImageUrl);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error in GetTwitchInfo:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				}
			}
		}
		
		static async void CheckLoop()
		{
			Api.Settings.ClientId = Program.Bot.config.twitchClientId;
			Api.Settings.AccessToken = Program.Bot.config.botAccessToken;

			bool reauth = string.IsNullOrWhiteSpace(Program.Bot.config.botRefreshToken);
			bool refresh = string.IsNullOrWhiteSpace(Program.Bot.config.botAccessToken);

			while (true)
			{
				if (reauth)
				{
					try
					{
						ConsoleUtil.WriteLine("Reauthorizing...");

						Program.Bot.config.Reauthorize(await TwitchAuthenticator.GetTokens(Api));
						Api.Settings.AccessToken = Program.Bot.config.botAccessToken;

						reauth = false;
						refresh = false;
					}
					catch (Exception ex)
					{
						ConsoleUtil.WriteLine("Error when authenticating:", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
						await Task.Delay(1000);
						continue;
					}
				}
				if (refresh)
				{
					try
					{
						ConsoleUtil.WriteLine("Refreshing Access Token...");

						var token = await TwitchAuthenticator.RefreshToken(Api);

						Program.Bot.config.Refresh(token);
						Api.Settings.AccessToken = Program.Bot.config.botAccessToken;

						ConsoleUtil.WriteLine("Refreshing complete!");
						refresh = false;
					}
					catch (BadParameterException)
					{
						ConsoleUtil.WriteLine("Refreshing failed. Authentication required.");

						reauth = true;
					}
					catch (BadScopeException)
					{
						ConsoleUtil.WriteLine("Refreshing failed. Authentication required.");

						reauth = true;
					}
					catch (Exception ex)
					{
						ConsoleUtil.WriteLine("Error when refreshing token:", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					}
				}

				try
				{
					foreach (var channel in Config.GetAllBroadcasters())
					{
						try
						{
							if (await BroadcasterOnline(channel))
							{
								HandleStreamStart(channel);
							}
							else
							{
								HandleStreamEnd(channel);
							}
						}
						catch (GatewayTimeoutException) { }
						catch (BadGatewayException) { }
						catch (BadScopeException)
						{
							ConsoleUtil.WriteLine("Access token invalid.");
							refresh = true;
							break;
						}
						catch (Exception ex)
						{
							ConsoleUtil.WriteLine("Error in CheckLoop in channel with ID " + channel + ":", ConsoleColor.Red);
							ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
						}
					}
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error in CheckLoop:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				}

				if (!refresh)
					await Task.Delay(5000);
			}
		}

		static async Task<bool> BroadcasterOnline(ulong channelId)
		{
			try
			{
				return (await Api.Helix.Streams.GetStreamsAsync(userIds: [channelId.ToString()])).Streams.Length != 0;
			}
			catch (BadScopeException)
			{
				throw;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error in BroadcasterOnline:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				return false;
			}
		}

		public static void HandleStreamStart(ulong channelId)
		{
			if (!ActiveBroadcasters.Add(channelId))
				return;

			EventUtil.Dispatch(new StreamStartedEvent(channelId));
		}
		public static void HandleStreamEnd(ulong channelId)
		{
			if (!ActiveBroadcasters.Remove(channelId))
				return;

			EventUtil.Dispatch(new StreamEndedEvent(channelId));
		}
	}

	public class StreamStartedEvent : EventInstance
	{
		public ulong channelId;

		public StreamStartedEvent(ulong channelId)
		{
			this.channelId = channelId;
		}
	}
	public class StreamEndedEvent : EventInstance
	{
		public ulong channelId;

		public StreamEndedEvent(ulong channelId)
		{
			this.channelId = channelId;
		}
	}
}