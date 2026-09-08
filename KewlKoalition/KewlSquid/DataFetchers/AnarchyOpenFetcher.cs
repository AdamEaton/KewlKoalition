using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class AnarchyOpenFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "Anarchy Battle (Open) Rotations"; } }
		protected override string BaseRotationTitle { get { return "Anarchy Battle (Open) Rotation"; } }
		protected override string CachedImageName { get { return "AnarchyOpen.png"; } }

		protected override bool shoutOutChallenge { get { return true; } }
		protected override bool shoutOutTriColor { get { return true; } }
		protected override bool shoutOutSplatfest { get { return true; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.anarchyOpenRotations; }
	}
}
