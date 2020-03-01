# SysBot.NET
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)

## Support Discord:

For support on setting up your own instance of SysBot.NET, feel free to join the discord! (Beware of un-official discords who claim to be official)

[<img src="https://canary.discordapp.com/api/guilds/401014193211441153/widget.png?style=banner2">](https://discord.gg/tDMvSRv)

[sys-botbase](https://github.com/olliz0r/sys-botbase) client for remote control automation of Nintendo Switch consoles.

## SysBot.Base:
- Base logic library to be built upon in game-specific projects.
- Contains a synchronous and asynchronous Bot connection class to interact with sys-botbase.

## SysBot.Tests:
- Unit Tests for ensuring logic behaves as intended :)

# Example Implementations

The driving force to develop this project is automated bots for Nintendo Switch Pokémon games. An example implementation is provided in this repo to demonstrate interesting tasks this framework is capable of performing.

## SysBot.Pokemon:
- Class library using SysBot.Base to contain logic related to Sword/Shield bots.

Supported Pokémon Sword/Shield Bots:
- Surprise Trade Bot: Surprise Trades random Pokémon files from a folder.
- Link Trade Bot: Trades out Pokémon files to specific Link Codes, and can randomly distribute when no priority trades are needed.
- Shiny Egg Finding Bot: Repeatedly grabs eggs until a shiny egg is received.
- Dudu Bot: Hosts link trades and quits when the trade partner offers a Pokémon, and prints any Raid RNG details to the log.

## SysBot.Pokemon.WinForms:
- Simple GUI Launcher for spinning up Pokémon bots.
- Is currently set up to spin up Pokémon bots (as described above). Refer to the Wiki for details on how to use.
- Configuration of program settings is performed in-app and is saved as a local json file.

## SysBot.Pokemon.Discord:
- Discord interface for remotely interacting with the WinForms GUI.
- Provide a discord login token and the Roles that are allowed to interact with your bots.
- Commands are provided to manage & join the distribution queue.

## SysBot.Pokemon.Twitch:
- Twitch.tv interface for remotely announcing when the distribution starts.
- Provide a Twitch login token, username, and channel for login.

Uses [Discord.Net](https://github.com/discord-net/Discord.Net) and [TwitchLib](https://github.com/TwitchLib/TwitchLib) as a dependency via Nuget.

## Dependencies
Pokémon API logic is provided by [PKHeX](https://github.com/kwsch/PKHeX/), and template generation is provided by [AutoMod](https://github.com/architdate/PKHeX-Plugins/).

# License
Refer to the `License.md` for details regarding licensing.
