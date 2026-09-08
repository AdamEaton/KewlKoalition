using System.Collections.Generic;

namespace KewlSquid.DataFetchers
{
	public class ChallengeFetcher : VersusFetcher
	{
		protected override string EmbedTitle { get { return "Challenge Rotations"; } }
		protected override string BaseRotationTitle { get { return "Challenge Rotation"; } }
		protected override string CachedImageName { get { return "Challenge.png"; } }

		protected override bool shoutOutChallenge { get { return false; } }
		protected override bool shoutOutTriColor { get { return true; } }
		protected override bool shoutOutSplatfest { get { return true; } }

		protected override IEnumerable<VersusRotations.Rotation> GetRotations() { return Rotations.challengeRotations; }
	}
}
