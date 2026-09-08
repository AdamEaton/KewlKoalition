using System;

namespace KewlSquid.DataFetchers
{
	public enum SplatoonGame
	{
		Regular,
		AnarchyOpen,
		AnarchySeries,
		SplatfestOpen,
		SplatfestPro,
		SplatfestTriColor,
		XBattle,
		Challenge,
		SalmonRun,
		BigRun,
		EggstraWork,
	}
	public static class SplatoonGameExt
	{
		public static bool IsVersus(this SplatoonGame game)
		{
			switch (game)
			{
				case SplatoonGame.Regular:
				case SplatoonGame.AnarchyOpen:
				case SplatoonGame.AnarchySeries:
				case SplatoonGame.SplatfestOpen:
				case SplatoonGame.SplatfestPro:
				case SplatoonGame.SplatfestTriColor:
				case SplatoonGame.XBattle:
				case SplatoonGame.Challenge:
					return true;
				case SplatoonGame.SalmonRun:
				case SplatoonGame.BigRun:
				case SplatoonGame.EggstraWork:
					return false;
			}
			throw new NotSupportedException("The specified SplatoonGame object (" + game.ToString() + ") is not supported.");
		}
		public static bool IsCoop(this SplatoonGame game)
		{
			switch (game)
			{
				case SplatoonGame.Regular:
				case SplatoonGame.AnarchyOpen:
				case SplatoonGame.AnarchySeries:
				case SplatoonGame.SplatfestOpen:
				case SplatoonGame.SplatfestPro:
				case SplatoonGame.SplatfestTriColor:
				case SplatoonGame.XBattle:
				case SplatoonGame.Challenge:
					return false;
				case SplatoonGame.SalmonRun:
				case SplatoonGame.BigRun:
				case SplatoonGame.EggstraWork:
					return true;
			}
			throw new NotSupportedException("The specified SplatoonGame object (" + game.ToString() + ") is not supported.");
		}
	}
}
