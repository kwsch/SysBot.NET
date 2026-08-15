using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

public class TradeBase64Modal : IModal
{
    public string Title => "Trade Base64 File";

    [InputLabel("Base64 Text")]
    [ModalTextInput("base64", TextInputStyle.Paragraph, placeholder: "Paste the Base64 text here...")]
    public string Base64 { get; set; } = string.Empty;

    [RequiredInput(false)]
    [InputLabel("Trade Code (optional)")]
    [ModalTextInput("code", TextInputStyle.Short, placeholder: "Leave blank for a random code", maxLength: 8)]
    public string? Code { get; set; }
}
