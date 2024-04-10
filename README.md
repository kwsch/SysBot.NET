# SysBot.NET
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)

## Dies ist ein benutzerdefinierter Fork des Sysbots und somit ein eigenes Projekt - darum bitte nicht mit unseren Problemen die Pkhex Entwickler belästigen

## Note: for all those who don't understand German but only English, we still use SysBot from PKHex in English, using SysBot is free, feel free!
### - Our support Discord from Aura and Furby: [Discord](https://discord.gg/MzVM8DVM9w)

## Support Discord von Aura und Furby, die SysBot Benutzung ist kostenlos bei uns:
- [Discord](https://discord.gg/MzVM8DVM9w)

## Support Discord:

Wenn Sie Unterstützung beim Einrichten Ihrer eigenen Instanz von SysBot.NET benötigen, können Sie sich gerne dem Discord anschließen! (Vorsicht vor inoffiziellen Zwietracht, die behaupten, offiziell zu sein)

[<img src="https://canary.discordapp.com/api/guilds/401014193211441153/widget.png?style=banner2">](https://discord.gg/tDMvSRv)

[sys-botbase](https://github.com/olliz0r/sys-botbase) Client zur Fernsteuerungsautomatisierung von Nintendo Switch-Konsolen.

## SysBot.Base:
- Basislogikbibliothek, auf der in spielspezifischen Projekten aufgebaut werden kann.
– Enthält eine synchrone und asynchrone Bot-Verbindungsklasse zur Interaktion mit sys-botbase.

## SysBot.Tests:
- Unit-Tests, um sicherzustellen, dass sich die Logik wie beabsichtigt verhält :)

# Beispielimplementierungen

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

Verwendet [Discord.Net](https://github.com/discord-net/Discord.Net) , [TwitchLib](https://github.com/TwitchLib/TwitchLib) und [StreamingClientLibary](https://github.com/SaviorXTanren/StreamingClientLibrary) als Abhängigkeit über Nuget.

## Andere Abhängigkeiten
Die Pokémon-API-Logik wird bereitgestellt von [PKHeX](https://github.com/kwsch/PKHeX/), und die Vorlagengenerierung wird bereitgestellt von [Auto-Legality Mod](https://github.com/architdate/PKHeX-Plugins/). Aktuelle Vorlagengenerierungsverwendungen [@santacrab2](https://www.github.com/santacrab2)'s [Auto-Legality Mod fork](https://github.com/santacrab2/PKHeX-Plugins).

# License
Siehe die `License.md` Einzelheiten zur Lizenzierung.
