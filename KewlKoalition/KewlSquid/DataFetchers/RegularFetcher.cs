using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class RegularFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "Regular Battle Rotations"; } }
		protected override string BaseRotationTitle { get { return "Regular Battle Rotation"; } }
		protected override string CachedImageName { get { return "Regular.png"; } }

		protected override bool shoutOutChallenge { get { return true; } }
		protected override bool shoutOutTriColor { get { return true; } }
		protected override bool shoutOutSplatfest { get { return true; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.regularRotations; }
	}
}
