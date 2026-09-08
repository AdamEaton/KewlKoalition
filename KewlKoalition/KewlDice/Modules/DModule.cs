using System;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlDice.Utilities;

namespace KewlDice.Modules
{
	[HelpInfo("d", "For rolling dice parametrically.", HelpPriorities.D)]
	public class DModule : KewlDiceModule
	{
		[SlashCommand("d", "Roll some dice.", false, RunMode.Async)]
		public async Task D
			(
				[Summary("sides", "The number of sides on the dice.")]
				int sides,
				[Summary("count", "The number of dice to roll. (Default: 1)")]
				int count = 1,
				[Summary("modifier", "An amount to add or subtract from the total roll. (Default: 0)")]
				int modifier = 0
			)
		{
			using (responseHandler)
			{
				try
				{
					await DiceUtil.RollDice(this, sides, count, modifier);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'd' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}
	}
}
