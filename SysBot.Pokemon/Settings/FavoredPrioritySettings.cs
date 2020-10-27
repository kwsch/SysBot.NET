using System;
using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class FavoredPrioritySettings : IFavoredCPQSetting
    {
        private const string Operation = nameof(Operation);
        private const string Configure = nameof(Configure);
        public override string ToString() => "Favoritism Settings";

        // We want to allow hosts to give preferential treatment, while still providing service to users without favor.
        // These are the minimum values that we permit. These values yield a fair placement for the favored.
        private const int _mfi = 15;
        private const float _mexp = 0.5f;
        private const float _mmul = 0.1f;

        private int _minimumFreeAhead = _mfi;
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

        [Category(Configure), Description("Multiply: Inserted after (unfavored users)*(Multiply) unfavored users.")]
        public float Multiply
        {
            get => _multiply;
            set => _multiply = Math.Max(_mmul, value);
        }

        [Category(Configure), Description("Amount of unfavored users to not skip over.")]
        public int MinimumFreeAhead
        {
            get => _minimumFreeAhead;
            set => _minimumFreeAhead = Math.Max(_mfi, value);
        }
    }
}
