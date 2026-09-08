using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class XBattleFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "X Battle Rotations"; } }
		protected override string BaseRotationTitle { get { return "X Battle Rotation"; } }
		protected override string CachedImageName { get { return "XBattle.png"; } }

		protected override bool shoutOutChallenge { get { return true; } }
		protected override bool shoutOutTriColor { get { return true; } }
		protected override bool shoutOutSplatfest { get { return true; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.xBattleRotations; }
	}
}
