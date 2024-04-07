using System;
using System.ComponentModel;

namespace SysBot.Pokemon;

public class FavoredPrioritySettings : IFavoredCPQSetting
{
    private const string Operation = nameof(Operation);
    private const string Configure = nameof(Configure);
    public override string ToString() => "Einstellungen zur Bevorzugung";

    // We want to allow hosts to give preferential treatment, while still providing service to users without favor.
    // These are the minimum values that we permit. These values yield a fair placement for the favored.
    private const int _mfi = 2;
    private const float _bmin = 1;
    private const float _bmax = 3;
    private const float _mexp = 0.5f;
    private const float _mmul = 0.1f;

    private int _minimumFreeAhead = _mfi;
    private float _bypassFactor = 1.5f;
    private float _exponent = 0.777f;
    private float _multiply = 0.5f;

    [Category(Operation), Description("Legt fest, wie die Einfügeposition der bevorzugten Benutzer berechnet wird. \"None\" verhindert die Anwendung von Bevorzugungen.")]
    public FavoredMode Mode { get; set; }

    [Category(Configure), Description("Eingefügt nach (unbeliebte Nutzer)^(Exponent) unbeliebte Nutzer.")]
    public float Exponent
    {
        get => _exponent;
        set => _exponent = Math.Max(_mexp, value);
    }

    [Category(Configure), Description("Multiplizieren: Eingefügt nach (unbeliebte Benutzer)*(multiplizieren) unbeliebte Benutzer. Bei einer Einstellung von 0,2 wird nach 20 % der Nutzer eingefügt.")]
    public float Multiply
    {
        get => _multiply;
        set => _multiply = Math.Max(_mmul, value);
    }

    [Category(Configure), Description("Anzahl der nicht bevorzugten Benutzer, die nicht übersprungen werden sollen. Dies wird nur erzwungen, wenn sich in der Warteschlange eine beträchtliche Anzahl von nicht bevorzugten Benutzern befindet.")]
    public int MinimumFreeAhead
    {
        get => _minimumFreeAhead;
        set => _minimumFreeAhead = Math.Max(_mfi, value);
    }

    [Category(Configure), Description("Mindestanzahl der nicht bevorzugten Benutzer in der Warteschlange, damit {MinimumFreeAhead} erzwungen wird. Wenn die oben genannte Zahl höher als dieser Wert ist, wird ein bevorzugter Benutzer nicht vor {MinimumFreeAhead} nicht bevorzugte Benutzer gestellt.")]
    public int MinimumFreeBypass => (int)Math.Ceiling(MinimumFreeAhead * MinimumFreeBypassFactor);

    [Category(Configure), Description("Skalar, der mit {MinimumFreeAhead} multipliziert wird, um den Wert {MinimumFreeBypass} zu bestimmen.")]
    public float MinimumFreeBypassFactor
    {
        get => _bypassFactor;
        set => _bypassFactor = Math.Min(_bmax, Math.Max(_bmin, value));
    }
}
