using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlDice.Utilities;

namespace KewlDice.Modules
{
	[HelpInfo("roll", "For using standard dice notation.", HelpPriorities.Roll)]
	public class RollModule : KewlDiceModule
	{
		private static readonly char[] anyOf = ['+', '-'];

		[SlashCommand("roll", "Roll some dice.", false, RunMode.Async)]
		public async Task Roll
			(
				[Summary("notation", "The standard notation for the roll (e.g. '4d6' or '2d12+3').")]
				string notation
			)
		{
			using (responseHandler)
			{
				try
				{
					var d = notation.ToLowerInvariant().IndexOf('d');
					if (d < 0)
					{
						ConsoleUtil.WriteLine("Couldn't find the 'd' in '" + notation + "'.");
						await SendResponse(false, "Use standard dice notation with the 'roll' command.");
						return;
					}

					int count = 0;

					var countString = notation[..d];
					if (countString.Length <= 0)
					{
						count = 1;
					}
					else if (!TryParseInt(countString, out count))
					{
						ConsoleUtil.WriteLine("Can't parse '" + countString + "' as int.");
						await SendResponse(false, "I don't know how to roll '" + countString + "' dice.");
						return;
					}

					var numsString = notation[(d + 1)..];

					int sides = 0;
					int modifier = 0;

					int m = numsString.IndexOfAny(anyOf);
					if (m < 0)
					{
						if (!TryParseInt(numsString, out sides))
						{
							ConsoleUtil.WriteLine("Can't parse '" + numsString + "' as int.");
							await SendResponse(false, "I don't know how to roll '" + numsString + "'-sided dice.");
							return;
						}
					}
					else
					{
						var sidesString = numsString[..m];
						var modString = numsString[m..];

						if (!TryParseInt(sidesString, out sides))
						{
							ConsoleUtil.WriteLine("Can't parse '" + sidesString + "' as int.");
							await SendResponse(false, "I don't know how to roll '" + sidesString + "'-sided dice.");
							return;
						}
						if (!TryParseSignedInt(modString, out modifier))
						{
							ConsoleUtil.WriteLine("Can't parse '" + modString + "' as int.");
							await SendResponse(false, "I don't know how to add '" + modString + "' to your roll.");
							return;
						}
					}

					await DiceUtil.RollDice(this, sides, count, modifier);
				}
				catch (Exception ex)
				{
					ConsoleUtil.WriteLine("Error while executing 'roll' command:", ConsoleColor.Red);
					ConsoleUtil.WriteLine(ex, ConsoleColor.Red);
					return;
				}
			}
		}

		static bool TryParseInt(string s, out int i)
		{
			if (!s.All(char.IsDigit))
			{
				i = 0;
				return false;
			}

			if (s.Length > 0)
			{
				if (!int.TryParse(s, out i))
					return false;
			}
			else
			{
				i = 0;
			}
			return true;
		}
		static bool TryParseSignedInt(string s, out int i)
		{
			if (!(s.All(char.IsDigit) || s.Skip(1).All(char.IsDigit) && IsSignOrDigit(s[0])))
			{
				i = 0;
				return false;
			}

			int multiplier = 1;
			if (!char.IsDigit(s[0]))
			{
				multiplier = (s[0] == '-' ? -1 : 1);
				s = s[1..];
			}

			if (s.Length > 0)
			{
				if (!int.TryParse(s, out i))
					return false;
			}
			else
			{
				i = 0;
			}

			i *= multiplier;
			return true;
		}
		static bool IsSignOrDigit(char c)
		{
			return char.IsDigit(c) || c == '+' || c == '-';
		}
	}
}
