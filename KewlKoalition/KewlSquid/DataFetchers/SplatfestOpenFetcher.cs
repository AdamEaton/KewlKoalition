using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class SplatfestOpenFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "Splatfest Battle (Open) Rotations"; } }
		protected override string BaseRotationTitle { get { return "Splatfest Battle (Open) Rotation"; } }
		protected override string CachedImageName { get { return "SplatfestOpen.png"; } }

		protected override bool shoutOutChallenge { get { return true; } }
		protected override bool shoutOutTriColor { get { return true; } }
		protected override bool shoutOutSplatfest { get { return false; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.splatfestOpenRotations; }
	}
}
