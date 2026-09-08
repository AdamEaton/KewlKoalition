# Kewl Bot
A user and moderator assistant bot, enabling users limited access to privileges not granted by their assigned permissions, as well as convenience commands for moderators.

## Features
- Users with and without the "Manage Channels" permission can create and manage temporary voice/text channel pairs that last only as long as they're in use
- Users with and without the "Create Invite" permission can create single-use temporary invites
- Admins have convenience tools to quickly and easily put users in timeout or move users to new or existing voice channels

## Quick Setup Guide
To integrate this bot into a Discord server:
1. Follow the [Quick Setup Guide in the main README file](https://github.com/AdamEaton/KewlKoalition/blob/main/README.md#quick-setup-guide)
2. Add the following data to the configuration file:
	1. For each server the bot is to be included in, the `servers` array should have a corresponding element with the following data:
		1. `id`: The Server ID of the requisite server
		2. `roles`: A subobject containing Role IDs for the following keys:
			1. `admin`: A role granting access to admin-only features of the bot
			2. `bot`: A role assigned to all bot users in the server
			3. `convict`: A role given to users who have been timed out by a moderator
		3. `channels`: A subobject containing Channel IDs for the following keys:
			1. `tempCategory`: A category channel under which temporary rooms should be created and managed
			2. `jail`: A voice channel where timed-out users can be relocated to
		4. `tempExceptions`: An array of Channel IDs for channels within the temporary channel category that should *not* be treated as temporary by the bot
