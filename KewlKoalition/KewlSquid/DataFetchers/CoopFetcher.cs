using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Discord;
using KewlKommon.Core;
using KewlKommon.Extensions;
using KewlKommon.Interaction;
using KewlKommon.Utilities;
using KewlSquid.Utilities;

using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace KewlSquid.DataFetchers
{
	public abstract class CoopFetcher : DataFetcher<CoopRotations.Rotation>
	{
		const int IconBuffer = 16;
		const int IconSize = 144;
		const int IconOutlineCount = 16;
		const float IconOutlineWidth = 5f;

		const string NoImageName = "Unrecognized.png";
		static string ResourcesPath { get { return ResourcesUtil.GetResourcePath("Coop.txt"); } }

		static string IconsPath { get { return ResourcesUtil.GetResourcePath(Path.Combine("Icons", "Coop")); } }
		static string KingIconsPath { get { return Path.Combine(IconsPath, "Kings"); } }
		static string StageIconsPath { get { return Path.Combine(IconsPath, "Stages"); } }
		static string WeaponIconsPath { get { return Path.Combine(IconsPath, "Weapons"); } }
		public static CoopResources Resources { get; private set; } = new CoopResources();
		public static CoopRotations Rotations { get; private set; } = new CoopRotations();

		static JsonSerializerOptions GetJsonOptions()
		{
			var output = new JsonSerializerOptions(JsonSerializerOptions.Default);
			output.IncludeFields = true;
			return output;
		}

		protected override string? EmbedFooter
		{
			get
			{
				if (Rotations == null)
					return null;

				var shoutOuts = new List<string>();
				if (shoutOutBigRun && Rotations.bigRun.Count > 0)
				{
					shoutOuts.Add("Big Run");
				}
				if (shoutOutEggstra && Rotations.eggstra.Count > 0)
				{
					shoutOuts.Add("Eggstra Work");
				}

				switch (shoutOuts.Count)
				{
					case 0:
						return null;
					case 1:
						return "Information on " + shoutOuts[0] + " shifts is also available.";
					case 2:
						return "Information on " + shoutOuts[0] + " and " + shoutOuts[1] + " shifts is also available.";
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
		protected abstract bool shoutOutBigRun { get; }
		protected abstract bool shoutOutEggstra { get; }

		public async override Task Execute(IResponseHandler responseHandler)
		{
			try
			{
				ImportResources();

				await FetchResources();
				if (CoopResources.State != RequestState.Received)
				{
					ConsoleUtil.WriteLine("Resource state still '" + CoopResources.State.ToString() + "' after fetching.", ConsoleColor.Red);
					await responseHandler.SetDeferredResponse("OatmealDome has failed me...");
					return;
				}

				await FetchRotations();
				if (CoopRotations.State != RequestState.Received)
				{
					ConsoleUtil.WriteLine("Shift state still '" + CoopRotations.State.ToString() + "' after fetching.", ConsoleColor.Red);
					await responseHandler.SetDeferredResponse("OatmealDome has failed me...");
					return;
				}

				var embed = new EmbedBuilder
				{
					Title = EmbedTitle,
					Description = null
				};

				bool hasContent = false;
				bool isFirst = true;
				foreach (var rotation in GetRotations())
				{
					embed.AddField(
						GetRotationTitle(rotation, isFirst),
						rotation.EmbedString);
					isFirst = false;
					hasContent = true;
				}
				if (!hasContent)
				{
					embed.AddField("No data available.", "Looks like Grizzco isn't hiring for this job...");
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
				ConsoleUtil.WriteLine("Error while executing salmon command:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
				await responseHandler.SetDeferredResponse("Whoopsie doopsie!");
				return;
			}
		}

		static void ImportResources()
		{
			if (CoopResources.State >= RequestState.Imported)
				return;

			try
			{
				using (var file = File.OpenRead(ResourcesPath))
				using (var reader = new StreamReader(file))
				{
					Resources = JsonSerializer.Deserialize<CoopResources>(reader.ReadToEnd(), GetJsonOptions()) ?? new CoopResources();
				}
			}
			catch
			{
				Resources = new CoopResources();
			}
			finally
			{
				CoopResources.State = RequestState.Imported;
			}
		}
		static void ExportResources()
		{
			if (CoopResources.State < RequestState.Imported)
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
				ConsoleUtil.WriteLine("Exception while exporting co-op resources:", ConsoleColor.Red);
				ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
			}
		}

		static async Task FetchResources()
		{
			ImportResources();

			while (CoopResources.State == RequestState.Requested)
			{
				await Task.Delay(1000);
			}
			if (CoopResources.State == RequestState.Received)
			{
				return;
			}

			CoopResources.State = RequestState.Requested;

			var request = await HttpUtil.NewGetMessage(
				"three/resources/coop",
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
							if (JsonNode.Parse(content) is not JsonNode node)
								throw new InvalidDataException("Could not parse HTTP response content.");

							Resources.eTag = response.Headers.ETag?.Tag;
							Resources.eTagIsWeak = response.Headers?.ETag?.IsWeak ?? true;

							if (node.TryGetObject("stages", out var stagesObj))
								foreach (var stageData in stagesObj)
									if (stageData.Value?.ToString() is string s)
										Resources.stages[stageData.Key] = s;
							Resources.stages["-1"] = "???";

							if (node.TryGetObject("weapons/main", out var weaponsObj))
								foreach (var weaponData in weaponsObj)
									if (weaponData.Value?.ToString() is string s)
										Resources.weapons[weaponData.Key] = s;
							Resources.weapons["-1"] = "Random";
							Resources.weapons["-2"] = "Grizzco Random";
							Resources.weapons["-3"] = "???";

							if (node.TryGetObject("enemy", out var enemiesObj))
								foreach (var enemyData in enemiesObj)
									if (enemyData.Value?.ToString() is string s)
										Resources.enemies[enemyData.Key] = s;

							Resources.enemies["-1"] = "Random";
							Resources.enemies["Random"] = "Random";

							ExportResources();
							CoopResources.State = RequestState.Received;
						}
						catch
						{
							CoopResources.State = RequestState.Imported;
							throw;
						}
						break;
					}
				case 304:   // Not Modified
					{
						ConsoleUtil.WriteLine("Got response: Not Modified");

						CoopResources.State = RequestState.Received;
						break;
					}
				case 429:   // Too Many Requests
					{
						ConsoleUtil.WriteLine("Got response: Too Many Requests", ConsoleColor.Yellow);

						await Task.Delay(5000);
						CoopResources.State = RequestState.Imported;
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
			while (CoopRotations.State == RequestState.Requested)
			{
				await Task.Delay(1000);
			}
			if (CoopRotations.State == RequestState.Received)
			{
				return;
			}

			CoopRotations.State = RequestState.Requested;

			var request = await HttpUtil.NewGetMessage(
				"three/coop/phases",
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

						Rotations = new CoopRotations();

						if (node.TryGetArray("Normal", out var normalArr))
							foreach (var normalRot in normalArr)
								ParseRotation(normalRot, Rotations.normal);
						if (node.TryGetArray("BigRun", out var bigRunArr))
							foreach (var bigRunRot in bigRunArr)
								ParseRotation(bigRunRot, Rotations.bigRun);
						if (node.TryGetArray("TeamContest", out var eggstraArr))
							foreach (var eggstraRot in eggstraArr)
								ParseRotation(eggstraRot, Rotations.eggstra);

						bool cacheTimeValid = false;
						CoopRotations.CacheExpiryTime = DateTimeOffset.MaxValue;
						foreach (var rotation in Rotations.GetAllRotations())
						{
							if (rotation.endTime < CoopRotations.CacheExpiryTime)
							{
								CoopRotations.CacheExpiryTime = rotation.endTime;
								cacheTimeValid = true;
							}
						}
						if (!cacheTimeValid)
						{
							ConsoleUtil.WriteLine("Resetting Versus cache");
							CoopRotations.CacheExpiryTime = DateTimeOffset.UtcNow;
						}

						CoopRotations.State = RequestState.Received;
						CoopRotations.Update();
						break;
					}
				case 400:   // Bad Request
					{
						ConsoleUtil.WriteLine("Got response: Bad Request", ConsoleColor.Red);
						CoopRotations.State = RequestState.Default;
						throw new System.Net.HttpListenerException(code, "The server responded with code 400 (Bad Request).");
					}
				default:
					{
						ConsoleUtil.WriteLine("Got response: Unrecognized (" + code + ")", ConsoleColor.Red);
						throw new System.Net.HttpListenerException(code, "The response status code is not recognized: " + code);
					}
			}
		}
		static void ParseRotation(JsonNode? node, List<CoopRotations.Rotation> list)
		{
			if (node == null)
				return;

			var rotationObj = node.AsObject();
			var output = new CoopRotations.Rotation();

			if (rotationObj.TryGetString("startTime", out var start))
				output.startTime = DateTimeOffset.Parse(start);
			if (rotationObj.TryGetString("endTime", out var end))
				output.endTime = DateTimeOffset.Parse(end);

			if (rotationObj.TryGetArray("weapons", out var weaponsArr))
				foreach (var weapon in weaponsArr)
					output.weapons.Add(weapon != null ? weapon.ToString() : "-3");
			if (rotationObj.TryGetArray("rareWeapons", out var rareWeaponsArr))
				foreach (var weapon in rareWeaponsArr)
					output.weapons.Add(weapon != null ? weapon.ToString() : "-3");

			if (rotationObj.TryGetString("stage", out var stage))
				output.stage = stage;
			if (rotationObj.TryGetString("bigBoss", out var king))
				output.king = king;

			list.Add(output);
		}
		public override void CacheRotationImage()
		{
			Image stageImage;

			var rotation = GetMainRotation();
			if (rotation == null)
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
				try
				{
					stageImage = Image.FromFile(Path.Combine(StageIconsPath, Resources.GetStageName(rotation.stage, false) + ".png"));
				}
				catch
				{
					ConsoleUtil.WriteLine("Couldn't find stage resource '" + Resources.GetStageName(rotation.stage, false) + ".png'", ConsoleColor.Red);
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
			}

			using (var graphics = Graphics.FromImage(stageImage))
			{
				if (rotation != null)
				{
					int weaponCount = 0;
					for (int i = 0; i < rotation.weapons.Count; i++)
					{
						var weaponName = Resources.GetWeaponName(rotation.weapons[i], false);
						if (weaponName == "???")
							weaponName = "Unrecognized";

						DrawIconWithOutline(graphics,
							Path.Combine(WeaponIconsPath, weaponName + ".png"),
							new RectangleF(IconBuffer + weaponCount * IconSize, IconBuffer, IconSize, IconSize),
							false);
						weaponCount++;
					}
					if (rotation.rareWeapons.Count > 0)
					{
						var weaponName = (rotation.rareWeapons.Count > 1 ? "Grizzco Random" : Resources.GetWeaponName(rotation.rareWeapons[0], false));
						if (weaponName == "???")
							weaponName = "Grizzco Random";

						DrawIconWithOutline(graphics,
							Path.Combine(WeaponIconsPath, weaponName + ".png"),
							new RectangleF(IconBuffer, stageImage.Height - IconBuffer - IconSize, IconSize, IconSize),
							false);
					}
					if (!string.IsNullOrEmpty(rotation.king))
					{
						DrawIconWithOutline(graphics,
							Path.Combine(KingIconsPath, Resources.GetEnemyName(rotation.king, false) + ".png"),
							new RectangleF(stageImage.Width - IconBuffer - IconSize, stageImage.Height - IconBuffer - IconSize, IconSize, IconSize),
							true);
					}
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

						if (Path.GetDirectoryName(path) is string directory)
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

	}
	public class CoopResources
	{
		public static RequestState State { get; set; } = RequestState.Default;

		public string? eTag = null;
		public bool eTagIsWeak = true;
		public Dictionary<string, string> stages = [];
		public Dictionary<string, string> weapons = [];
		public Dictionary<string, string> enemies = [];

		static string GetName(Dictionary<string, string> dict, string? id, bool includeUnrecognizedId)
		{
			if (string.IsNullOrEmpty(id))
				return "Unrecognized" + (includeUnrecognizedId ? " (None)" : "");

			return (dict.TryGetValue(id, out var name) ? name : "Unrecognized" + (includeUnrecognizedId ? " (" + id + ")" : ""));
		}

		public string GetStageName(string? stageId, bool includeUnrecognizedId)
		{
			return GetName(stages, stageId, includeUnrecognizedId);
		}
		public string GetWeaponName(string? weaponId, bool includeUnrecognizedId)
		{
			return GetName(weapons, weaponId, includeUnrecognizedId);
		}
		public string GetEnemyName(string? enemyId, bool includeUnrecognizedId)
		{
			return GetName(enemies, enemyId, includeUnrecognizedId);
		}
	}
	public class CoopRotations
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
		public List<Rotation> normal = [];
		public List<Rotation> bigRun = [];
		public List<Rotation> eggstra = [];

		public IEnumerable<Rotation> GetAllRotations()
		{
			foreach (var rotation in bigRun)
				yield return rotation;
			foreach (var rotation in eggstra)
				yield return rotation;
			foreach (var rotation in normal)
				yield return rotation;
		}
		public Rotation? GetMainRotation()
		{
			return GetAllRotations()?.FirstOrDefault();
		}

		public class Rotation : RotationData
		{
			public List<string> weapons = [];
			public List<string> rareWeapons = [];
			public string? stage = null;
			public string? king = null;

			public string EmbedString
			{
				get
				{
					string output = string.Empty;
					output += "Starts: " + new TimestampTag(startTime, TimestampTagStyles.ShortDateTime);
					output += "\nEnds: " + new TimestampTag(endTime, TimestampTagStyles.ShortDateTime);
					output += "\nStage: " + CoopFetcher.Resources.GetStageName(stage, true);
					if (!string.IsNullOrEmpty(king))
						output += "\nKing: " + CoopFetcher.Resources.GetEnemyName(king, true);
					if (weapons != null && weapons.Count > 0)
					{
						output += "\nWeapons: ";
						for (int i = 0; i < weapons.Count; i++)
						{
							if (i > 0)
								output += ", ";
							output += CoopFetcher.Resources.GetWeaponName(weapons[i], true);
						}
					}
					if (rareWeapons != null && rareWeapons.Count > 0)
					{
						output += "\nRare Weapons: ";
						for (int i = 0; i < rareWeapons.Count; i++)
						{
							if (i > 0)
								output += ", ";
							output += CoopFetcher.Resources.GetWeaponName(rareWeapons[i], true);
						}
					}
					return output;
				}
			}
		}
	}
}