using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class SalmonRunFetcher : CoopFetcher
	{
		protected override string EmbedTitle { get { return "Salmon Run Shifts"; } }
		protected override string BaseRotationTitle { get { return "Salmon Run Shift"; } }
		protected override string CachedImageName { get { return "SalmonRun.png"; } }

		protected override bool shoutOutBigRun { get { return true; } }
		protected override bool shoutOutEggstra { get { return true; } }

		protected override IEnumerable<CoopRotations.Rotation> GetRotations() { return Rotations.normal; }
	}
}
