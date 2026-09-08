using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class AnarchySeriesFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "Anarchy Battle (Series) Rotations"; } }
		protected override string BaseRotationTitle { get { return "Anarchy Battle (Series) Rotation"; } }
		protected override string CachedImageName { get { return "AnarchySeries.png"; } }

		protected override bool shoutOutChallenge { get { return true; } }
		protected override bool shoutOutTriColor { get { return true; } }
		protected override bool shoutOutSplatfest { get { return true; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.anarchySeriesRotations; }
	}
}
