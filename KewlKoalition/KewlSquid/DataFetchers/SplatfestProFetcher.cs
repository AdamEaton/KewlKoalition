using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class SplatfestProFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "Splatfest Battle (Pro) Rotations"; } }
		protected override string BaseRotationTitle { get { return "Splatfest Battle (Pro) Rotation"; } }
		protected override string CachedImageName { get { return "SplatfestPro.png"; } }

		protected override bool shoutOutChallenge { get { return true; } }
		protected override bool shoutOutTriColor { get { return true; } }
		protected override bool shoutOutSplatfest { get { return false; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.splatfestProRotations; }
	}
}
