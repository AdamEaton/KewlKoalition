# Kewl Koalition
A study in asynchronous programming in the form of a suite of Discord bots. The package includes 6 projects, representing 5 individual bots, along with a shared common library on which they were built.

## Projects

### KewlKommon
A shared common library on which each of the other bots is built, handling the boilerplate for loading configurations, streamlining interactions, and other common functions of Discord bots.

### KewlBot
A user and moderator assistant bot, enabling users limited access to privileges not granted by their assigned permissions, as well as convenience commands for moderators.

### KewlDice
A simple dice-rolling bot that can roll multiple dice, add modifiers, and calculate ranges and totals.

### KewlTewns
A music streaming bot that has been partially thwarted by Youtube's recent anti-bot updates.

### KewlStreams
A Twitch stream announcing bot, letting users know when community members or other interesting streams begin.

### KewlSquid
A Splatoon bot that fetches details about the current game modes in Splatoon 3 (powered by OatmealDome's JelonzoBotDX API).

## Quick Setup Guide
To integrate a bot into a Discord server:
1. Create an application in the Discord Developer Hub
2. In the General Information tab, give the application an icon, name, and description.
3. In the Installation tab, set the installation context to `Guild Install`, and the Install Link to `None`.
4. In the Bot Tab:
	1. Give the bot user a username, user icon, and banner image
	2. Disable the `Public Bot` option
	3. Enable the `Server Members` and `Message Content` intents
	4. Generate a bot token and copy it for inclusion in the bot's config file
5. In the OAuth2 tab's Oath2 URL Generator:
	1. Turn on the `bot` scope in the OAuth2 URL Generator
	2. Turn on the `Administrator` permission in the Bot Permissions section (or cherry-pick permissions required for the bot's specific functions)
	3. Set the integration type to `Guild Install`
	4. Visit the generated URL in your browser to trigger the install wizard
6. Build the application
7. Copy the included `config_template.json` file to the default config path `.\Configs\config.json` (or use another name if working with multiple configurations)
8. Add the Discord bot token from step `4.iv` under the `discord/token` key
9. Expand the `servers` array to include one element for each server the bot is to be included in
10. Refer to each project's `README` for information on additional setup requirements (or to the `README` in KewlKommon for information on building a custom bot using the framework)