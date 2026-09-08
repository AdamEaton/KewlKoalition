# Kewl Streams
A Twitch stream announcing bot, letting users know when community members or other interesting streams begin.

## Features
- Supports rolling up to 128 dice at a time with up to 1024 sides

## Quick Setup Guide
To integrate this bot into a Discord server:
1. Follow the Quick Setup Guide in the main README file
2. Create a Twitch application for the bot to access Twitch through
	1. Set the OAuth Redirect URL to `http://localhost:8080/redirect/`
3. Add the following data to the configuration file:
	1. A `twitch` key with the following data:
		1. `clientId`: The Client ID of your Twitch application
		2. `clientSecret`: The Client Secret of your Twitch application
		3. `redirectUri`: The OAuth Redirect URL of your Twitch application
		4. `accessToken`: An empty string (to be automatically updated by the bot)
		5. `refreshToken`: An empty string (to be automatically updated by the bot)
	1. For each server the bot is to be included in, the `servers` array should have a corresponding element with the following data:
		1. `id`: The Server ID of the server
		2. `roles`: A subobject containing Role IDs for the following keys:
			1. `admin`: A role granting access to admin-only features of the bot
		3. `streams`: An array of Twitch IDs that should be announced by the bot in this server