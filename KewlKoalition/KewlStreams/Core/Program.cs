using System.Threading.Tasks;
using Discord;
using KewlKommon.Core;
using KewlStreams.Twitch;

namespace KewlStreams.Core
{
	public class Program : KewlProgram<Program, Config>
	{
		public override string programName { get { return "Kewl Streams"; } }
		public override string componentIdPrefix { get { return "KewlStreams"; } }
		public override Color themeColor { get { return new Color(0xA020FF); } }

		public override int featureVersion { get { return 0; } }
		public override int patchVersion { get { return 0; } }

		static void Main(string[] args)
		{
			StartUp(args);
		}

		protected override Task OnInitialize()
		{
			Announcer.Init();
			TwitchManager.Init();
			return Task.CompletedTask;
		}
	}
}