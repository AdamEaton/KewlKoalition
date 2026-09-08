# KewlSquid
A Splatoon bot that fetches details about the current game modes in Splatoon 3 (powered by OatmealDome's JelonzoBotDX API).

## Features
- Shares live information about requested game modes
- Builds attractive embed images
- Alerts players to the availability of relevant limited-time game modes

## Quick Setup Guide
To integrate this bot into a Discord server:
1. Follow the Quick Setup Guide in the main README file
2. Add the following data to the configuration file:
	1. For each server the bot is to be included in, the `servers` array should have a corresponding element with the following data:
		1. `id`: The Server ID of the requisite server
		2. `roles`: A subobject containing Role IDs for the following keys:
			1. `admin`: A role granting access to admin-only features of the bot