using System;
using System.Threading.Tasks;
using Discord;
using KewlKommon.Utilities;
using KewlDice.Modules;

namespace KewlDice.Utilities
{
	public static class DiceUtil
	{
		public const int MaxSides = 1024;
		public const int MaxDice = 128;

		public static async Task RollDice(KewlDiceModule module, int sides, int count, int modifier)
		{
			if (sides <= 0)
			{
				ConsoleUtil.WriteLine("Can't roll a die with " + sides + " sides.");
				await module.SendResponse(false, "Dice need to have at least one side!");
				return;
			}
			if (sides > MaxSides)
			{
				ConsoleUtil.WriteLine("Can't roll a die with " + sides + " sides.");
				await module.SendResponse(false, "I can't roll dice with more than " + MaxSides + " sides.");
				return;
			}
			if (count <= 0)
			{
				ConsoleUtil.WriteLine("Can't roll " + count + " dice.");
				await module.SendResponse(false, "You need to roll at least one die!");
				return;
			}
			if (count > MaxDice)
			{
				ConsoleUtil.WriteLine("Can't roll " + count + " dice.");
				await module.SendResponse(false, "I can't roll more than " + MaxDice + " dice at once.");
				return;
			}

			ConsoleUtil.WriteLine("Rolling " + count + "d" + sides + (modifier != 0 ? (modifier < 0 ? "-" : "+") + Math.Abs(modifier) : "") + "...");

			var embed = new EmbedBuilder() { Title = "Results of rolling " + count + "d" + sides + (modifier != 0 ? (modifier < 0 ? "-" : "+") + Math.Abs(modifier) : "") };

			int total = 0;
			int min = sides + 1;
			int max = 0;
			string results = "";
			var r = new Random();
			for (int i = 0; i < count; i++)
			{
				if (i > 0)
					results += ", ";

				int roll = r.Next() % sides + 1;
				min = Math.Min(min, roll);
				max = Math.Max(max, roll);
				total += roll;
				results += roll;
			}

			embed.AddField("Results", results);

			if (modifier != 0)
			{
				embed.AddField("Total", total + (modifier < 0 ? " - " : " + ") + Math.Abs(modifier) + " = " + (total + modifier));
			}
			if (count > 1)
			{
				if (modifier == 0)
					embed.AddField("Total", total);

				embed.AddField("Minimum Roll", min);
				embed.AddField("Maximum Roll", max);
			}

			await module.SendResponse(true, embed: embed
				.WithColor(KewlKommon.Core.KewlProgram.Bot.themeColor)
				.Build());
		}
	}
}