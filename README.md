# SysBot.NET
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)

## Support Discord:

Wenn Sie Unterstützung beim Einrichten Ihrer eigenen Instanz von SysBot.NET benötigen, können Sie sich gerne dem Discord anschließen! (Vorsicht vor inoffiziellen Zwietracht, die behaupten, offiziell zu sein)

[<img src="https://canary.discordapp.com/api/guilds/401014193211441153/widget.png?style=banner2">](https://discord.gg/tDMvSRv)

[sys-botbase](https://github.com/olliz0r/sys-botbase) Client zur Fernsteuerungsautomatisierung von Nintendo Switch-Konsolen.

## SysBot.Base:
- Basislogikbibliothek, auf der in spielspezifischen Projekten aufgebaut werden kann.
– Enthält eine synchrone und asynchrone Bot-Verbindungsklasse zur Interaktion mit sys-botbase.

## SysBot.Tests:
- Unit-Tests, um sicherzustellen, dass sich die Logik wie beabsichtigt verhält :)

# Example Implementations

Die treibende Kraft bei der Entwicklung dieses Projekts sind automatisierte Bots für Nintendo Switch-Pokémon-Spiele. In diesem Repo wird eine Beispielimplementierung bereitgestellt, um interessante Aufgaben zu demonstrieren, die dieses Framework ausführen kann. Siehe die [Wiki](https://github.com/kwsch/SysBot.NET/wiki) Weitere Informationen zu den unterstützten Pokémon-Funktionen finden Sie hier.

## SysBot.Pokemon:
- Klassenbibliothek, die SysBot.Base verwendet, um Logik zum Erstellen und Ausführen von Sword/Shield-Bots zu enthalten.

## SysBot.Pokemon.WinForms:
- Einfacher GUI-Launcher zum Hinzufügen, Starten und Stoppen von Pokémon-Bots (wie oben beschrieben).
- Die Konfiguration der Programmeinstellungen erfolgt in der App und wird als lokale JSON-Datei gespeichert.

## SysBot.Pokemon.Discord:
- Discord-Schnittstelle für die Remote-Interaktion mit der WinForms-GUI.
- Geben Sie ein Discord-Anmeldetoken und die Rollen an, die mit Ihren Bots interagieren dürfen.
- Es werden Befehle zum Verwalten und Beitreten zur Verteilungswarteschlange bereitgestellt.

## SysBot.Pokemon.Twitch:
- Twitch.tv-Schnittstelle zur Fernankündigung des Beginns der Verteilung.
- Geben Sie ein Twitch-Anmeldetoken, einen Benutzernamen und einen Kanal für die Anmeldung an.

## SysBot.Pokemon.YouTube:
- YouTube.com-Schnittstelle zur Fernankündigung des Beginns der Verteilung.
- Geben Sie für die Anmeldung eine YouTube-Anmelde-Client-ID, ein Client-Geheimnis und eine Kanal-ID an.

Uses [Discord.Net](https://github.com/discord-net/Discord.Net) , [TwitchLib](https://github.com/TwitchLib/TwitchLib) and [StreamingClientLibary](https://github.com/SaviorXTanren/StreamingClientLibrary) as a dependency via Nuget.

## Other Dependencies
Pokémon API logic is provided by [PKHeX](https://github.com/kwsch/PKHeX/), and template generation is provided by [Auto-Legality Mod](https://github.com/architdate/PKHeX-Plugins/). Current template generation uses [@santacrab2](https://www.github.com/santacrab2)'s [Auto-Legality Mod fork](https://github.com/santacrab2/PKHeX-Plugins).

# License
Refer to the `License.md` for details regarding licensing.
