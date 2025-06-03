using FuzzySharp;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class BallHelper<T> where T : PKM, new()
    {
        public static Task<string> GetLegalBall(ushort speciesIndex, string formNameForBallVerification, string ballName, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization, PKM pk)
        {
            var closestBall = GetClosestBall(ballName, inputLocalization, targetLocalization);
            if (closestBall != null)
            {
                var ballIndex = Array.IndexOf(targetLocalization.Strings.balllist, closestBall);
                if (ballIndex >= 0)
                {
                    pk.Ball = (byte)ballIndex;
                    if (new LegalityAnalysis(pk).Valid)
                        return Task.FromResult(closestBall);
                }
            }
            var legalBall = BallApplicator.ApplyBallLegalByColor(pk);
            return Task.FromResult(targetLocalization.Strings.balllist[legalBall]);
        }

        public static string? GetClosestBall(string userBall, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            var targetBallList = targetLocalization.Strings.balllist.Where(b => !string.IsNullOrWhiteSpace(b)).ToArray();
            var inputBallList = inputLocalization.Strings.balllist.Where(b => !string.IsNullOrWhiteSpace(b)).ToArray();

            var (BallName, Distance) = targetBallList
                .Select(b => (BallName: b, Distance: Fuzz.PartialRatio(userBall, b)))
                .OrderByDescending(b => b.Distance)
                .FirstOrDefault();

            var inputFuzzyBall = inputBallList
                .Select(b => (BallName: b, Distance: Fuzz.PartialRatio(userBall, b)))
                .OrderByDescending(b => b.Distance)
                .FirstOrDefault();

            const int MinAcceptableScore = 70;

            if (targetBallList.Length > 0 && Distance >= MinAcceptableScore)
            {
                return BallName;
            }

            if (inputBallList.Length > 0 && inputFuzzyBall.Distance >= MinAcceptableScore)
            {
                var inputIndex = Array.IndexOf(inputLocalization.Strings.balllist, inputFuzzyBall.BallName);
                if (inputIndex >= 0 && inputIndex < targetLocalization.Strings.balllist.Length)
                {
                    return targetLocalization.Strings.balllist[inputIndex];
                }
            }

            if (targetBallList.Length > 0 && inputBallList.Length > 0)
            {
                return Distance >= inputFuzzyBall.Distance ? BallName :
                       TranslateBallToTarget(inputFuzzyBall.BallName, inputLocalization, targetLocalization);
            }

            if (targetBallList.Length > 0)
                return BallName;

            if (inputBallList.Length > 0)
                return TranslateBallToTarget(inputFuzzyBall.BallName, inputLocalization, targetLocalization);

            return null;
        }

        private static string? TranslateBallToTarget(string? ballName, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            if (string.IsNullOrEmpty(ballName))
                return null;

            var inputIndex = Array.IndexOf(inputLocalization.Strings.balllist, ballName);
            if (inputIndex >= 0 && inputIndex < targetLocalization.Strings.balllist.Length)
            {
                return targetLocalization.Strings.balllist[inputIndex];
            }

            return ballName;
        }
    }
}
