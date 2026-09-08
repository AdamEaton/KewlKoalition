using Discord;
using KewlKommon.Core;

namespace KewlDice.Core
{
	public class Program : KewlProgram<Program, KewlConfig<KewlGuildConfig>>
	{
		public override string programName { get { return "Kewl Dice"; } }
		public override string componentIdPrefix { get { return "KewlDice"; } }
		public override Color themeColor { get { return new Color(0xFF2040); } }

		public override int featureVersion { get { return 0; } }
		public override int patchVersion { get { return 0; } }

		static void Main(string[] args)
		{
			StartUp(args);
		}
	}
}