using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlSquid.DataFetchers;

namespace KewlSquid.Modules
{
	[HelpInfo("splatoon", "For getting information on rotations in Splatoon 3.", HelpPriorities.Splatoon)]
	public class SplatoonModule : KewlSquidModule
	{
		Dictionary<SplatoonGame, DataFetcher> dataFetchers = [];
		bool initialized = false;
		void Initialize()
		{
			if (initialized)
				return;

			dataFetchers[SplatoonGame.Regular] = new RegularFetcher();
			dataFetchers[SplatoonGame.AnarchyOpen] = new AnarchyOpenFetcher();
			dataFetchers[SplatoonGame.AnarchySeries] = new AnarchySeriesFetcher();
			dataFetchers[SplatoonGame.SplatfestOpen] = new SplatfestOpenFetcher();
			dataFetchers[SplatoonGame.SplatfestPro] = new SplatfestProFetcher();
			dataFetchers[SplatoonGame.SplatfestTriColor] = new SplatfestTriColorFetcher();
			dataFetchers[SplatoonGame.XBattle] = new XBattleFetcher();
			dataFetchers[SplatoonGame.Challenge] = new ChallengeFetcher();
			dataFetchers[SplatoonGame.SalmonRun] = new SalmonRunFetcher();
			dataFetchers[SplatoonGame.BigRun] = new BigRunFetcher();
			dataFetchers[SplatoonGame.EggstraWork] = new EggstraWorkFetcher();

			VersusRotations.updatedEvent += OnVersusRotationsUpdated;
			CoopRotations.updatedEvent += OnCoopRotationsUpdated;
		}

		[SlashCommand("splatoon", "Display information about the current and upcoming rotations in Splatoon 3.")]
		public async Task Splatoon
			(
				[Summary("game", "The game mode to get information about.")]
				SplatoonGame game
			)
		{
			using (responseHandler)
			{
				try
				{
					await DeferResponse(true);

					Initialize();

					if (!dataFetchers.TryGetValue(game, out var fetcher))
					{
						ConsoleUtil.WriteLine("No data fetcher found for game type (" + game.ToString() + ")", ConsoleColor.Red);
						await SetDeferredResponse("Whoopsie doopsie!");
						return;
					}

					await fetcher.Execute(responseHandler);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'splatoon' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					await SetDeferredResponse("Whoopsie doopsie!");
					return;
				}
			}
		}

		void OnVersusRotationsUpdated()
		{
			foreach (var game in EnumUtil.GetValues<SplatoonGame>())
			{
				if (!game.IsVersus())
					continue;

				dataFetchers[game].CacheRotationImage();
			}
		}
		void OnCoopRotationsUpdated()
		{
			foreach (var game in EnumUtil.GetValues<SplatoonGame>())
			{
				if (!game.IsCoop())
					continue;

				dataFetchers[game].CacheRotationImage();
			}
		}
	}
}
