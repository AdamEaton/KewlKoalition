# Kewl Dice
A simple dice-rolling bot that can roll multiple dice, add modifiers, and calculate ranges and totals.

## Features
- Supports rolling up to 128 dice at a time with up to 1024 sides

## Quick Setup Guide
To integrate this bot into a Discord server:
1. Follow the [Quick Setup Guide in the main README file](https://github.com/AdamEaton/KewlKoalition/blob/main/README.md#quick-setup-guide)
2. Add the following data to the configuration file:
	1. For each server the bot is to be included in, the `servers` array should have a corresponding element with the following data:
		1. `id`: The Server ID of the requisite server
		2. `roles`: A subobject containing Role IDs for the following keys:
			1. `admin`: A role granting access to admin-only features of the bot
