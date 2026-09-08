# Kewl Kommon
A shared common library on which each of the other bots is built, handling the boilerplate for loading configurations, streamlining interactions, and other common functions of Discord bots.

## Features
- Handles setup of connection configuration appropriate for small bots on small server communities
- Includes a set of common basic slash commands
- Tooling for common bot operations
	- Configuration parsing
	- Interaction context and entitlement parsing
	- Shutdown-blocking locks
- Project-wide detection, initialization, and handling of common feature patterns
	- Slash command registration
	- Per-guild monitors
	- Event listener registration/deregistration
	- Help information

## Quick Setup Guide
To build a new bot using the KewlKommon framework:
- Add a new project to your solution, and add KewlKommon as a project dependency
- Add the required `libdave.dll`, `libsodium.dll`, and `opus.dll` dependencies, and mark them for copying to the build output
- Additional config requirements can included by deriving from the `KewlConfig` and/or `KewlGuildConfig` classes as required
- Implement the KewlProgram class and call `Startup` on program entry
```
// Example KewlProgram class
namespace KewlExample.Core
{
	public class Program : KewlProgram<Program, KewlConfig<KewlGuildConfig>>
	{
		public override string programName { get { return "Your Bot Name"; } }
		public override string componentIdPrefix { get { return "ServerWideUniqueIdentifier"; } }
		public override Color themeColor { get { return new Color(0xFFFFFF); } }
		
		public override int featureVersion { get { return 1; } }	// 3rd tuplet
		public override int patchVersion { get { return 0; } }		// 4th tuplet
	}
}
```