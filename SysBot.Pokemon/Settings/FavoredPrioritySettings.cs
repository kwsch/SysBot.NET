using System;
using System.ComponentModel;

namespace SysBot.Pokemon;

public class FavoredPrioritySettings : IFavoredCPQSetting
{
    private const string Operation = nameof(Operation);
    private const string Configure = nameof(Configure);
    public override string ToString() => "Favoritism Settings";

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

    [Category(Operation), Description("Determines how the insertion position of favored users is calculated. \"None\" will prevent any favoritism from being applied.")]
    public FavoredMode Mode { get; set; }

    [Category(Configure), Description("Inserted after (unfavored users)^(exponent) unfavored users.")]
    public float Exponent
    {
        get => _exponent;
        set => _exponent = Math.Max(_mexp, value);
    }

    [Category(Configure), Description("Multiply: Inserted after (unfavored users)*(multiply) unfavored users. Setting this to 0.2 adds in after 20% of users.")]
    public float Multiply
    {
        get => _multiply;
        set => _multiply = Math.Max(_mmul, value);
    }

    [Category(Configure), Description("Number of unfavored users to not skip over. This only is enforced if a significant number of unfavored users are in the queue.")]
    public int MinimumFreeAhead
    {
        get => _minimumFreeAhead;
        set => _minimumFreeAhead = Math.Max(_mfi, value);
    }

    [Category(Configure), Description("Minimum number of unfavored users in queue to cause {MinimumFreeAhead} to be enforced. When the aforementioned number is higher than this value, a favored user is not placed ahead of {MinimumFreeAhead} unfavored users.")]
    public int MinimumFreeBypass => (int)Math.Ceiling(MinimumFreeAhead * MinimumFreeBypassFactor);

    [Category(Configure), Description("Scalar that is multiplied with {MinimumFreeAhead} to determine the {MinimumFreeBypass} value.")]
    public float MinimumFreeBypassFactor
    {
        get => _bypassFactor;
        set => _bypassFactor = Math.Min(_bmax, Math.Max(_bmin, value));
    }
}
