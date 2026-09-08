using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class SplatfestTriColorFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "Tri-Color Battle Rotations"; } }
		protected override string BaseRotationTitle { get { return "Tri-Color Battle Rotation"; } }
		protected override string CachedImageName { get { return "TriColor.png"; } }

		protected override bool shoutOutChallenge { get { return true; } }
		protected override bool shoutOutTriColor { get { return false; } }
		protected override bool shoutOutSplatfest { get { return false; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.splatfestTriColorRotations; }
	}
}
