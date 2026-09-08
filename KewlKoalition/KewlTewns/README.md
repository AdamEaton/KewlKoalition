# Kewl Tewns
A music streaming bot that has been partially thwarted by Youtube's recent anti-bot updates.

**Due to recent Youtube updates, only songs previously played and cached by the application can be streamed on Discord.**

See [Forging a Cached Playlist](https://github.com/AdamEaton/KewlKoalition/tree/main/KewlKoalition/KewlTewns#forging-a-cached-playlist) for information on formatting songs as the bot does for its cache.

## Features
- Queue up songs from Youtube by supplying their video URLs
- Queue entire Youtube playlists using their playlist URLs
- Build advanced playlists in Google Sheets for tracking additional metadata
- Advanced playlists can be used to automatically personalize playback to suit the present audience, favour lesser-played tracks, and favour tracks that have been played less recently
- Export playlists as properly organized and tagged music files
- Automatic song caching speeds up subsequent plays and preserves removed videos

## Quick Setup Guide
To integrate this bot into a Discord server:
1. Follow the [Quick Setup Guide in the main README file](https://github.com/AdamEaton/KewlKoalition/blob/main/README.md#quick-setup-guide)
2. Add the following data to the configuration file:
	1. For each server the bot is to be included in, the `servers` array should have a corresponding element with the following data:
		1. `id`: The Server ID of the target server
		2. `roles`: A subobject containing Role IDs for the following keys:
			1. `admin`: A role granting access to admin-only features of the bot
			2. `bot`: A role assigned to all bot users in the server
			3. `dj`: A role given to users who are allowed to control the bot's playback
		3. `commands`: An array of custom command objects, each of which contains the following data:
			1. `name`: The name of the command, to be displayed to users in the command list and typed by users invoking the command
			2. `description`: A brief description of the command, to be displayed to users in the command list
			3. `body`: A list of commands to issue to the bot
3. Create a [Google Cloud](https://developers.google.com/) project for the bot to access Google Sheets through
	1. Download and place the project's OAuth `credentials.json` file in the application folder

## Building a Google Sheets Playlist
Google Sheets playlists provide the greatest flexibility in controlling the playback of tracks. Use the following procedure to construct one that the bot can interact with:
1. Create a new Google Sheets document
2. Ensure that the account that authorized access to Google Sheets when prompted by the application has read/write permission in the sheet
3. Reduce the number of columns to 9
4. Add headers to each column in row 1

The columns must be in a fixed order to be correctly parsed by the bot. In order, the columns should be:
1. Title (Column A): The name of the track, as it should be displayed during playback
2. URL (Column B): The Youtube URL of the track
3. Source (Column C): Information about the source of the track, as it should be displayed during playback (can be used to record artist, album, curator, or other similar metadata about the track that is relevant to the playlist)
4. Requesters (Column D): A comma-separated, case-insensitive set of username exerpts, to be used when playback personalization is enabled, to identify users who endorsed the track (tracks will be automatically skipped if no connected users' usernames contain any of the exerpts)
5. Vetoers (Column E): A comma-separated, case-insensitive set of username exerpts, to be used when playback personalization is enabled, to identify users who vetoed the track (tracks will be automatically skipped if any connected users' usernames contain any of the exerpts)
6. Played (Column F): The number of times the track was played, to be used when playback is sorted by least played (automatically updated on each stream)
7. Last Play Time (Column G): The timestamp of the last time the track was played, to be used when playback is sorted by least recently played (automatically updated on each stream)
8. Length (Column H): The length of the track (automatically updated on first stream)
9. Info (Column I): A comma-separated set of metadata tags to assist the list curator in the maintenance of the list (automatically updated)

## Forging a Cached Playlist
Because Youtube no longer reliably serves Youtube videos to anonymous connections, the bot is only able to play songs that have been previously cached by it.

The application's audio cache is stored in the directory `.\Tewns\`. Cached tracks are stored as \*.mp3 files directly inside this folder.
Included is a set of prepared example songs available to populate the cache for testing purposes. These tracks can be played by passing the URL `https://www.youtube.com/playlist?list=PLnF_5UKxdwAAAdrAb-ak1CcKqFCxTN3Rf` to the `/play list` command.
An existing \*.mp3 file can be used to forge a cached track using the following procedure:
1. A Youtube-style ID must be selected for the song
	1. IDs of existing videos as well as arbitrarily constructed IDs can both be used
	2. IDs are case-sensitive, and can contain letters, numbers, hyphens, and underscores
	3. For convenience, the ID of a working copy of the same track is recommended
2. The track's \*.mp3 filename contains two portions:
	1. The selected ID
	2. A set of `(` and `)` corresponding to each of the letter characters in the ID (lowercase letters add `(`, uppercase letters add `)`)

For example:
- Selected ID: `aBc-123_XyZ`
- Cached track path: `.\Tewns\Abc-123_xYZ)((()).mp3`
- Youtube pseudo-URL: `https://www.youtube.com/watch?v=Abc-123_xYZ`
