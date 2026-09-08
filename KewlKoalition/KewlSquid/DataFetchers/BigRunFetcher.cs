using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class BigRunFetcher : CoopFetcher
	{
		protected override string EmbedTitle { get { return "Big Run Shifts"; } }
		protected override string BaseRotationTitle { get { return "Big Run Shift"; } }
		protected override string CachedImageName { get { return "BigRun.png"; } }

		protected override bool shoutOutBigRun { get { return false; } }
		protected override bool shoutOutEggstra { get { return true; } }

		protected override IEnumerable<CoopRotations.Rotation> GetRotations() { return Rotations.bigRun; }
	}
}
