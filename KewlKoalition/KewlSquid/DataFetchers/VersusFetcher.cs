using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using KewlKommon.Core;
using KewlKommon.Interaction;
using KewlKommon.Utilities;
using KewlSquid.Utilities;

using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using KewlKommon.Extensions;

namespace KewlSquid.DataFetchers
{
	public abstract class VersusFetcher : DataFetcher<VersusRotations.Rotation>
	{
		const int IconBuffer = 16;
		const int IconSize = 128;
		const int IconOutlineCount = 16;
		const float IconOutlineWidth = 5f;

		const string NoImageName = "Unrecognized.png";
		public const string NoEventTitle = "Unrecognized";
		public const string NoEventSubtitle = "This event mode isn't recognized.";
		public const string NoEventDescription = "I may need to be updated to properly display information about this event.";
		static string ResourcesPath { get { return ResourcesUtil.GetResourcePath("Versus.txt"); } }

		static string IconsPath { get { return ResourcesUtil.GetResourcePath(Path.Combine("Icons", "Versus")); } }
		static string ModeIconsPath { get { return Path.Combine(IconsPath, "Modes"); } }
		static string StageIconsPath { get { return Path.Combine(IconsPath, "Stages"); } }
		public static VersusResources Resources { get; private set; } = new VersusResources();
		public static VersusRotations Rotations { get; private set; } = new VersusRotations();

		protected override string? EmbedFooter
		{
			get
			{
				if (Rotations == null)
					return null;

				var shoutOuts = new List<string>();
				if (shoutOutChallenge && Rotations.challengeRotations.Count > 0)
				{
					shoutOuts.Add("Challenges");
				}
				if (shoutOutSplatfest && (Rotations.splatfestOpenRotations.Count + Rotations.splatfestProRotations.Count) > 0)
				{
					shoutOuts.Add("Splatfest battles");
				}
				if (shoutOutTriColor && Rotations.splatfestTriColorRotations.Count > 0)
				{
					shoutOuts.Add("Tri-Color battles");
				}

				switch (shoutOuts.Count)
				{
					case 0:
						return null;
					case 1:
						return "Information on " + shoutOuts[0] + " is also available.";
					case 2:
						return "Information on " + shoutOuts[0] + " and " + shoutOuts[1] + " is also available.";
					default:
						{
							string output = "";

							for (int i = 0; i < shoutOuts.Count; i++)
							{
								if (i > 0)
									output += ", ";
								if (i >= shoutOuts.Count - 1)
									output += "and ";

								output += shoutOuts[i];
							}

							return "Information on " + output + " is also available.";
						}
				}
			}
		}
		protected abstract bool shoutOutChallenge { get; }
		protected abstract bool shoutOutTriColor { get; }
		protected abstract bool shoutOutSplatfest { get; }

		public async override Task Execute(IResponseHandler responseHandler)
		{
			try
			{
				ImportResources();

				await FetchResources();
				if (VersusResources.State != RequestState.Received)
				{
					ConsoleUtil.WriteLine("Resource state still '" + VersusResources.State.ToString() + "' after fetching.", ConsoleColor.Red);
					await responseHandler.SetDeferredResponse("OatmealDome has failed me...");
					return;
				}

				await FetchRotations();
				if (VersusRotations.State != RequestState.Received)
				{
					ConsoleUtil.WriteLine("Rotation state still '" + VersusRotations.State.ToString() + "' after fetching.", ConsoleColor.Red);
					await responseHandler.SetDeferredResponse("OatmealDome has failed me...");
					return;
				}

				var embed = new EmbedBuilder
				{
					Title = EmbedTitle,
					Description = null
				};

				bool hasContent = false;
				var rotations = GetRotations().ToArray();
				if (rotations != null && rotations.Length > 0)
				{
					bool isFirst = true;
					foreach (var rotation in rotations)
					{
						if (isFirst && !string.IsNullOrWhiteSpace(rotation.eventType))
						{
							embed.Title = Resources.GetEventTitle(rotation.eventType, true);
							embed.AddField(
								Resources.GetEventSubtitle(rotation.eventType, true),
								Resources.GetEventDescription(rotation.eventType, true));
						}

						embed.AddField(
							GetRotationTitle(rotation, isFirst),
							rotation.EmbedString);
						isFirst = false;
						hasContent = true;
					}
				}
				if (!hasContent)
				{
					embed.AddField("No data available.", "The game mode is probably not active at the moment.");
				}

				embed = embed
					.WithColor(KewlProgram.Bot.themeColor)
					.WithImageUrl("attachment://" + Path.GetFileName(CachedImagePath));
				var footer = EmbedFooter;
				if (!string.IsNullOrWhiteSpace(footer))
					embed = embed.WithFooter(footer);

				await responseHandler.SetDeferredResponse(embed: embed.Build(), filePath: CachedImagePath);
				return;
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Error while executing command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				await responseHandler.SetDeferredResponse("Whoopsie doopsie!");
				return;
			}
		}

		static JsonSerializerOptions GetJsonOptions()
		{
			var output = new JsonSerializerOptions(JsonSerializerOptions.Default);
			output.IncludeFields = true;
			return output;
		}

		static void ImportResources()
		{
			if (VersusResources.State >= RequestState.Imported)
				return;

			try
			{
				using (var file = File.OpenRead(ResourcesPath))
				using (var reader = new StreamReader(file))
				{
					Resources = JsonSerializer.Deserialize<VersusResources>(reader.ReadToEnd(), GetJsonOptions()) ?? new VersusResources();
				}
			}
			catch
			{
				Resources = new VersusResources();
			}
			finally
			{
				VersusResources.State = RequestState.Imported;
			}
		}
		static void ExportResources()
		{
			if (VersusResources.State < RequestState.Imported)
				return;

			try
			{
				using (var file = File.OpenWrite(ResourcesPath))
				using (var writer = new StreamWriter(file))
				{
					writer.WriteLine(JsonSerializer.Serialize(Resources, GetJsonOptions()));
				}
			}
			catch (Exception ex)
			{
				ConsoleUtil.WriteLine("Exception while exporting versus resources:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
			}
		}

		static string FormatEventText(string rawChallengeText)
		{
			string output = rawChallengeText;
			output = output.Replace("\n", " ");
			output = output.Replace("\u30FB", "\n\u30FB");
			while (output.Contains(" \n"))
				output = output.Replace(" \n", "\n");
			while (output.Contains("\n "))
				output = output.Replace("\n ", "\n");
			return output;
		}

		protected static async Task FetchResources()
		{
			ImportResources();

			while (VersusResources.State == RequestState.Requested)
			{
				await Task.Delay(1000);
			}
			if (VersusResources.State == RequestState.Received)
			{
				return;
			}

			VersusResources.State = RequestState.Requested;

			var request = await HttpUtil.NewGetMessage(
				"three/resources/versus",
				Resources.eTag,
				Resources.eTagIsWeak,
				("language", "USen"));
			ConsoleUtil.WriteLine("Sending request " + request.RequestUri);
			var response = await HttpUtil.Client.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseContentRead);

			var code = (int)response.StatusCode;
			switch (code)
			{
				case 200:   // OK
					{
						ConsoleUtil.WriteLine("Got response: OK");

						try
						{
							var content = await response.Content.ReadAsStringAsync();
							Resources = new VersusResources();
							Resources.eTag = response.Headers.ETag?.Tag;
							Resources.eTagIsWeak = response.Headers.ETag?.IsWeak ?? true;

							if (JsonNode.Parse(content) is JsonNode node)
							{
								if (node.TryGetObject("stages", out var stagesObj))
									foreach (var stageData in stagesObj)
										if (stageData.Value?.ToString() is string s)
											Resources.stages[stageData.Key] = s;

								if (node.TryGetObject("rules", out var modesObj))
									foreach (var modeData in modesObj)
										if (modeData.Value?.ToString() is string s)
											Resources.modes[modeData.Key] = modeData.Value.ToString();

								if (node.TryGetObject("league/events", out var eventsObj))
									foreach (var eventData in eventsObj)
										Resources.events[eventData.Key] = new VersusResources.EventData(
											FormatEventText(eventData.Value.TryGetString("title", out var title) ? title : NoEventTitle),
											FormatEventText(eventData.Value.TryGetString("subtitle", out var subtitle) ? subtitle : NoEventSubtitle),
											FormatEventText(eventData.Value.TryGetString("description", out var description) ? description : NoEventDescription));
							}

							Resources.stages["-1"] = "???";
							Resources.modes["-1"] = "???";
							Resources.events["-1"] = new VersusResources.EventData(
								NoEventTitle + " (-1)",
								NoEventSubtitle + " (-1)",
								NoEventDescription + " (-1)");
							ExportResources();
							VersusResources.State = RequestState.Received;
						}
						catch
						{
							VersusResources.State = RequestState.Imported;
							throw;
						}
						break;
					}
				case 304:   // Not Modified
					{
						ConsoleUtil.WriteLine("Got response: Not Modified");

						VersusResources.State = RequestState.Received;
						break;
					}
				case 429:   // Too Many Requests
					{
						ConsoleUtil.WriteLine("Got response: Too Many Requests", ConsoleColor.Yellow);

						await Task.Delay(5000);
						VersusResources.State = RequestState.Imported;
						await FetchResources();
						break;
					}
				default:
					{
						ConsoleUtil.WriteLine("Got response: Unrecognized (" + code + ")", ConsoleColor.Red);
						throw new System.Net.HttpListenerException(code, "The response status code is not recognized: " + code);
					}
			}
		}
		static async Task FetchRotations()
		{
			while (VersusRotations.State == RequestState.Requested)
			{
				await Task.Delay(1000);
			}
			if (VersusRotations.State == RequestState.Received)
			{
				return;
			}

			VersusRotations.State = RequestState.Requested;
			Rotations = new VersusRotations();

			await FetchNormalAndFestRotations();
			await FetchEventRotations();

			bool cacheTimeValid = false;
			VersusRotations.CacheExpiryTime = DateTimeOffset.MaxValue;
			foreach (var rotation in Rotations.GetAllRotations())
			{
				if (rotation.endTime < VersusRotations.CacheExpiryTime)
				{
					VersusRotations.CacheExpiryTime = rotation.endTime;
					cacheTimeValid = true;
				}
			}
			if (!cacheTimeValid)
			{
				ConsoleUtil.WriteLine("Cache expiry not valid", ConsoleColor.Yellow);
				VersusRotations.CacheExpiryTime = DateTimeOffset.UtcNow;
			}

			VersusRotations.State = RequestState.Received;
			VersusRotations.Update();
		}
		static async Task FetchNormalAndFestRotations()
		{
			var request = await HttpUtil.NewGetMessage(
				"three/versus/phases",
				("count", "2"));
			ConsoleUtil.WriteLine("Sending request " + request.RequestUri);
			var response = await HttpUtil.Client.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseContentRead);

			var code = (int)response.StatusCode;
			switch (code)
			{
				case 200:   // OK
					{
						ConsoleUtil.WriteLine("Got response: OK");

						var content = await response.Content.ReadAsStringAsync();
						if (JsonNode.Parse(content) is not JsonNode node)
							throw new InvalidDataException("Could not parse HTTP response.");

						if (node.TryGetArray("normal", out var normalArr))
							foreach (var normal in normalArr)
							{
								if (!normal.TryGetValue("startTime", out var start))
									continue;
								if (!normal.TryGetValue("endTime", out var end))
									continue;

								var startTime = DateTimeOffset.Parse(start.ToString());
								var endTime = DateTimeOffset.Parse(end.ToString());

								if (normal.TryGetNode("Regular", out var regular))
									ParseRotation(regular, startTime, endTime, Rotations.regularRotations);
								if (normal.TryGetNode("Bankara", out var anarchySeries))
									ParseRotation(anarchySeries, startTime, endTime, Rotations.anarchySeriesRotations);
								if (normal.TryGetNode("BankaraOpen", out var anarchyOpen))
									ParseRotation(anarchyOpen, startTime, endTime, Rotations.anarchyOpenRotations);
								if (normal.TryGetNode("X", out var xBattle))
									ParseRotation(xBattle, startTime, endTime, Rotations.xBattleRotations);
							}

						JsonArray? festArr = null;
						try
						{
							if (node.TryGetObject("fest", out var festObj))
								festArr = festObj.First().Value?.AsArray();
						}
						catch (Exception)
						{
							return;
						}
						if (festArr == null)
							return;

						foreach (var fest in festArr)
						{
							if (!fest.TryGetValue("startTime", out var start))
								continue;
							if (!fest.TryGetValue("endTime", out var end))
								continue;

							var startTime = DateTimeOffset.Parse(start.ToString());
							var endTime = DateTimeOffset.Parse(end.ToString());

							if (fest.TryGetNode("FestRegular", out var splatfestOpen))
								ParseRotation(splatfestOpen, startTime, endTime, Rotations.splatfestOpenRotations);
							if (fest.TryGetNode("FestChallenge", out var splatfestPro))
								ParseRotation(splatfestPro, startTime, endTime, Rotations.splatfestProRotations);
							if (fest.TryGetNode("FestTriColor", out var splatfestTriColor))
								ParseRotation(splatfestTriColor, startTime, endTime, Rotations.splatfestTriColorRotations);
						}
						return;
					}
				case 400:   // Bad Request
					{
						ConsoleUtil.WriteLine("Got response: Bad Request", ConsoleColor.Red);
						VersusRotations.State = RequestState.Default;
						throw new System.Net.HttpListenerException(code, "The server responded with code 400 (Bad Request).");
					}
				default:
					{
						ConsoleUtil.WriteLine("Got response: Unrecognized (" + code + ")", ConsoleColor.Red);
						throw new System.Net.HttpListenerException(code, "The response status code is not recognized: " + code);
					}
			}
		}
		static async Task FetchEventRotations()
		{
			var request = await HttpUtil.NewGetMessage(
				"three/versus/league/events",
				("count", "2"));
			ConsoleUtil.WriteLine("Sending request " + request.RequestUri);
			var response = await HttpUtil.Client.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseContentRead);

			var code = (int)response.StatusCode;
			switch (code)
			{
				case 200:   // OK
					{
						ConsoleUtil.WriteLine("Got response: OK");

						var content = await response.Content.ReadAsStringAsync();

						if (JsonNode.Parse(content) is not JsonNode node)
							return;

						var challengesArr = node.AsArray();
						foreach (var challenge in challengesArr)
						{
							if (!challenge.TryGetString("rule", out var mode))
								continue;
							if (!challenge.TryGetString("eventType", out var eventType))
								continue;

							var stages = new List<string>();
							if (challenge.TryGetArray("stages", out var stagesArr))
								foreach (var stage in stagesArr)
									if (stage?.ToString() is string s)
										stages.Add(s);

							if (challenge.TryGetArray("phases", out var phasesArr))
								foreach (var phase in phasesArr)
								{
									if (!phase.TryGetString("startTime", out var start))
										continue;
									if (!phase.TryGetString("endTime", out var end))
										continue;

									var rotation = new VersusRotations.Rotation();
									rotation.startTime = DateTimeOffset.Parse(start);
									rotation.endTime = DateTimeOffset.Parse(end);

									if (rotation.endTime <= DateTimeOffset.UtcNow)
										continue;

									rotation.mode = mode;
									rotation.eventType = eventType;
									rotation.stages = [.. stages];

									Rotations.challengeRotations.Add(rotation);
									if (Rotations.challengeRotations.Count >= 2)
										return;
								}

						}

						break;
					}
				case 400:   // Bad Request
					{
						ConsoleUtil.WriteLine("Got response: Bad Request", ConsoleColor.Red);
						VersusRotations.State = RequestState.Default;
						throw new System.Net.HttpListenerException(code, "The server responded with code 400 (Bad Request).");
					}
				default:
					{
						ConsoleUtil.WriteLine("Got response: Unrecognized (" + code + ")", ConsoleColor.Red);
						throw new System.Net.HttpListenerException(code, "The response status code is not recognized: " + code);
					}
			}
		}
		static void ParseRotation(JsonNode node, DateTimeOffset start, DateTimeOffset end, List<VersusRotations.Rotation> list)
		{
			if (node == null)
				return;

			var rotationObj = node.AsObject();
			var output = new VersusRotations.Rotation();
			if (!rotationObj.TryGetString("rule", out var mode))
				return;
			if (mode == "None")
				return;

			output.startTime = start;
			output.endTime = end;
			output.mode = mode;

			if (rotationObj.TryGetArray("stages", out var stagesArr))
				foreach (var stage in stagesArr)
					if (stage?.ToString() is string s)
						output.stages.Add(s);

			list.Add(output);
		}
		public override void CacheRotationImage()
		{
			Image? stageImage;

			var rotation = GetMainRotation();
			if (rotation == null || rotation.stages == null || rotation.stages.Count <= 0)
			{
				try
				{
					stageImage = Image.FromFile(Path.Combine(StageIconsPath, NoImageName));
				}
				catch
				{
					ConsoleUtil.WriteLine("Couldn't find stage resource '" + NoImageName + "'.", ConsoleColor.Red);
					return;
				}
			}
			else
			{
				stageImage = GetStageImage(rotation.stages[0]);
				if (stageImage == null)
				{
					ConsoleUtil.WriteLine("Couldn't find stage resource for stage '" + rotation.stages[0] + "'.", ConsoleColor.Red);
					return;
				}
			}


			using (var graphics = Graphics.FromImage(stageImage))
			{
				if (rotation != null )
				{
					if (rotation.stages != null)
						for (int i = 1; i < rotation.stages.Count; i++)
						{
							var newStageImage = GetStageImage(rotation.stages[i]);
							if (newStageImage == null)
								continue;

							var rect = new RectangleF(stageImage.Size.Width * (float)i / rotation.stages.Count, 0f, (float)stageImage.Size.Width / rotation.stages.Count, stageImage.Size.Height);
							graphics.DrawImage(newStageImage, rect, rect, GraphicsUnit.Pixel);
						}

					DrawIconWithOutline(graphics,
						Path.Combine(ModeIconsPath, Resources.GetModeName(rotation.mode, false) + ".png"),
						new RectangleF(IconBuffer, IconBuffer, IconSize, IconSize),
						true);
				}
				graphics.Save();
			}

			stageImage.Save(CachedImagePath, ImageFormat.Png);

			bool DrawIconWithOutline(Graphics graphics, string path, RectangleF rect, bool fallback)
			{
				try
				{
					var icon = Image.FromFile(path);

					float[][] colorMatrixElements =
						[
							[0, 0, 0, 0, 0],
							[0, 0, 0, 0, 0],
							[0, 0, 0, 0, 0],
							[0, 0, 0, 1, 0],
							[0, 0, 0, 0, 1],
						];
					var colorMatrix = new ColorMatrix(colorMatrixElements);
					var outlineAttributes = new ImageAttributes();
					outlineAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

					var rectPoints = new PointF[3];
					rectPoints[0] = new PointF(rect.Left, rect.Top);
					rectPoints[1] = new PointF(rect.Right, rect.Top);
					rectPoints[2] = new PointF(rect.Left, rect.Bottom);

					for (int i = 0; i < IconOutlineCount; i++)
					{
						graphics.DrawImage(icon,
							[.. rectPoints.Select(p => p + new SizeF(
								IconOutlineWidth * (float)Math.Cos((Math.PI * 2f * i) / IconOutlineCount),
								IconOutlineWidth * (float)Math.Sin((Math.PI * 2f * i) / IconOutlineCount)))],
							new RectangleF(new Point(), icon.Size), GraphicsUnit.Pixel, outlineAttributes);
					}

					graphics.DrawImage(icon, rect);
					return true;
				}
				catch (Exception ex)
				{
					if (fallback)
					{
						ConsoleUtil.WriteLine("Error drawing icon '" + path + "'; using fallback:", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);

						if (Path.GetDirectoryName(path) is not string directory)
							return false;

						path = Path.Combine(directory, NoImageName);
						return DrawIconWithOutline(graphics, path, rect, false);
					}
					else
					{
						ConsoleUtil.WriteLine("Error drawing icon '" + path + "':", ConsoleColor.Red);
						ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
						return false;
					}
				}
			}
		}
		static Image? GetStageImage(string stageId)
		{
			Image output;
			try
			{
				output = Image.FromFile(Path.Combine(StageIconsPath, Resources.GetStageName(stageId, false) + ".png"));
			}
			catch
			{
				ConsoleUtil.WriteLine("Couldn't find stage resource '" + Resources.GetStageName(stageId, false) + ".png'", ConsoleColor.Red);
				try
				{
					output = Image.FromFile(Path.Combine(StageIconsPath, NoImageName));
				}
				catch
				{
					ConsoleUtil.WriteLine("Couldn't find stage resource '" + NoImageName + "'.", ConsoleColor.Red);
					return null;
				}
			}
			return output;
		}
	}
	public class VersusResources
	{
		public static RequestState State { get; set; } = RequestState.Default;
		public string? eTag = null;
		public bool eTagIsWeak = true;
		public Dictionary<string, string> stages = [];
		public Dictionary<string, string> modes = [];
		public Dictionary<string, EventData> events = [];

		static string GetName(Dictionary<string, string> dict, string? id, bool includeUnrecognizedId)
		{
			return (!string.IsNullOrEmpty(id) && dict.TryGetValue(id, out var name)) ? name : ("Unrecognized" + (includeUnrecognizedId ? " (" + id + ")" : ""));
		}

		public string GetStageName(string stageId, bool includeUnrecognizedId)
		{
			return GetName(stages, stageId, includeUnrecognizedId);
		}
		public string GetModeName(string? modeId, bool includeUnrecognizedId)
		{
			return GetName(modes, modeId, includeUnrecognizedId);
		}
		public string? GetEventTitle(string eventId, bool includeUnrecognizedId)
		{
			return (events.TryGetValue(eventId, out var e) ? e.title : VersusFetcher.NoEventTitle + (includeUnrecognizedId ? " (" + eventId + ")" : ""));
		}
		public string? GetEventSubtitle(string eventId, bool includeUnrecognizedId)
		{
			return (events.TryGetValue(eventId, out var e) ? e.subtitle : VersusFetcher.NoEventSubtitle + (includeUnrecognizedId ? " (" + eventId + ")" : ""));
		}
		public string? GetEventDescription(string eventId, bool includeUnrecognizedId)
		{
			return (events.TryGetValue(eventId, out var e) ? e.description : VersusFetcher.NoEventDescription + (includeUnrecognizedId ? " (" + eventId + ")" : ""));
		}

		public class EventData
		{
			public string? title = null;
			public string? subtitle = null;
			public string? description = null;

			public EventData(string title, string subtitle, string description)
			{
				this.title = title;
				this.subtitle = subtitle;
				this.description = description;
			}
		}
	}
	public class VersusRotations
	{
		public static void Update()
		{
			updatedEvent?.Invoke();
		}
		public static event Action? updatedEvent;

		static RequestState _State = RequestState.Default;
		public static RequestState State
		{
			get
			{
				switch (_State)
				{
					case RequestState.Default:
						return RequestState.Default;
					case RequestState.Imported:
						return RequestState.Default;
					case RequestState.Requested:
						return RequestState.Requested;
					case RequestState.Received:
						return CacheExpiryTime <= DateTimeOffset.UtcNow ? RequestState.Default : RequestState.Received;
				}
				return RequestState.Default;
			}
			set { _State = value; }
		}
		public static DateTimeOffset CacheExpiryTime { get; set; } = DateTimeOffset.UtcNow;
		public List<Rotation> splatfestTriColorRotations = [];
		public List<Rotation> splatfestOpenRotations = [];
		public List<Rotation> splatfestProRotations = [];
		public List<Rotation> challengeRotations = [];
		public List<Rotation> regularRotations = [];
		public List<Rotation> anarchyOpenRotations = [];
		public List<Rotation> anarchySeriesRotations = [];
		public List<Rotation> xBattleRotations = [];

		public IEnumerable<Rotation> GetAllRotations()
		{
			foreach (var rotation in splatfestTriColorRotations) yield return rotation;
			foreach (var rotation in splatfestOpenRotations) yield return rotation;
			foreach (var rotation in splatfestProRotations) yield return rotation;
			foreach (var rotation in challengeRotations) yield return rotation;
			foreach (var rotation in regularRotations) yield return rotation;
			foreach (var rotation in anarchyOpenRotations) yield return rotation;
			foreach (var rotation in anarchySeriesRotations) yield return rotation;
			foreach (var rotation in xBattleRotations) yield return rotation;
		}
		public Rotation? GetMainRotation()
		{
			return GetAllRotations().FirstOrDefault();
		}

		public class Rotation : RotationData
		{
			public List<string> stages = [];
			public string? mode = null;
			public string? eventType = null;

			public string EmbedString
			{
				get
				{
					string output = string.Empty;
					output += "Starts: " + new TimestampTag(startTime, TimestampTagStyles.ShortDateTime);
					output += "\nEnds: " + new TimestampTag(endTime, TimestampTagStyles.ShortDateTime);
					output += "\nMode: " + VersusFetcher.Resources.GetModeName(mode, true);
					if (stages != null && stages.Count > 0)
					{
						output += "\nStages: ";
						for (int i = 0; i < stages.Count; i++)
						{
							if (i > 0)
								output += ", ";
							output += VersusFetcher.Resources.GetStageName(stages[i], true);
						}
					}
					return output;
				}
			}
		}
	}
}