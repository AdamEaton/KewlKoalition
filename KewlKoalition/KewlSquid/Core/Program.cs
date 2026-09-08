using Discord;
using KewlKommon.Core;

namespace KewlSquid.Core
{
	public class Program : KewlProgram<Program, KewlConfig<KewlGuildConfig>>
	{
		public override string programName { get { return "Kewl Squid"; } }
		public override string componentIdPrefix { get { return "KewlSquid"; } }
		public override Color themeColor { get { return new Color(0x20FF40); } }

		public override int featureVersion { get { return 0; } }
		public override int patchVersion { get { return 0; } }

		static void Main(string[] args)
		{
			StartUp(args);
		}
	}
}