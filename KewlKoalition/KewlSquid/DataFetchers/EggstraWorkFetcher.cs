using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class EggstraWorkFetcher : CoopFetcher
	{
		protected override string EmbedTitle { get { return "Eggstra Work Shifts"; } }
		protected override string BaseRotationTitle { get { return "Eggstra Work Shift"; } }
		protected override string CachedImageName { get { return "EggstraWork.png"; } }

		protected override bool shoutOutBigRun { get { return true; } }
		protected override bool shoutOutEggstra { get { return false; } }

		protected override IEnumerable<CoopRotations.Rotation> GetRotations() { return Rotations.eggstra; }
	}
}
