using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

public class TradeSetModal : IModal
{
    public string Title => "Trade Showdown Set";

    [InputLabel("Showdown Set")]
    [ModalTextInput("showdown", TextInputStyle.Paragraph, placeholder: "Paste your Showdown set here...")]
    public string Showdown { get; set; } = string.Empty;

    [RequiredInput(false)]
    [InputLabel("Trade Code (optional)")]
    [ModalTextInput("code", TextInputStyle.Short, placeholder: "Leave blank for a random code", maxLength: 8)]
    public string? Code { get; set; }
}
