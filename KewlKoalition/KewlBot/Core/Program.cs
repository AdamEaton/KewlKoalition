using Discord;
using KewlKommon.Core;

namespace KewlBot.Core
{
	public class Program : KewlProgram<Program, KewlConfig<GuildConfig>>
	{
		public override string programName { get { return "Kewl Bot"; } }
		public override string componentIdPrefix { get { return "KewlBot"; } }
		public override Color themeColor { get { return new Color(0x0040FF); } }

		public override int featureVersion { get { return 0; } }
		public override int patchVersion { get { return 0; } }

		static void Main(string[] args)
		{
			StartUp(args);
		}
	}
}