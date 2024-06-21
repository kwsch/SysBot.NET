using FuzzySharp;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class BallHelper<T> where T : PKM, new()
    {
        public static Task<string>? GetLegalBall(ushort speciesIndex, string formNameForBallVerification, string ballName, GameStrings gameStrings, PKM pk)
        {
            var closestBall = GetClosestBall(ballName, gameStrings);
            if (closestBall != null)
            {
                pk.Ball = (byte)Array.IndexOf(gameStrings.balllist, closestBall);
                if (new LegalityAnalysis(pk).Valid)
                    return Task.FromResult(closestBall);
            }
            var legalBall = BallApplicator.ApplyBallLegalByColor(pk);
            return Task.FromResult(gameStrings.balllist[legalBall]);
        }

        public static string? GetClosestBall(string userBall, GameStrings gameStrings)
        {
            var ballList = gameStrings.balllist.Where(b => !string.IsNullOrWhiteSpace(b)).ToArray();
            var fuzzyBall = ballList
                .Select(b => (BallName: b, Distance: Fuzz.PartialRatio(userBall, b)))
                .OrderByDescending(b => b.Distance)
                .FirstOrDefault();
            return fuzzyBall != default ? fuzzyBall.BallName : null;
        }
    }
}
