# SysBot.NET
[sys-botbase](https://github.com/olliz0r/sys-botbase) client for remote control automation of Nintendo Switch consoles.

## SysBot.Base:
- Base logic library to be built upon in game-specific projects.
- Contains a synchronous and asynchronous Bot connection class to interact with sys-botbase.

## SysBot.Pokemon:
- Class library using SysBot.Base to contain logic related to Sword/Shield bots.

Supported Pokémon Sword/Shield Bots:
- Surprise Trade Bot: Surprise Trades random Pokémon files from a folder.
- Link Trade Bot: Trades out Pokémon files to specific Link Codes, and can randomly distribute when no priority trades are needed.
- Shiny Egg Finding Bot: Repeatedly grabs eggs until a shiny egg is received.

## SysBot.Console:
- Simple Launcher for spinning up bots. Provide config files in separate folders to launch specific bots.

## SysBot.Tests:
- Unit Tests for ensuring logic behaves as intended :)
