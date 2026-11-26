using System;
using System.Collections.Generic;

namespace CoinRush.Core
{
    /// <summary>Owns the timer and scores for one authoritative round.</summary>
    public sealed class Round
    {
        private readonly Dictionary<int, int> _scores = new();
        private readonly HashSet<int> _collectedCoins = new();
        private readonly int _durationTicks;

        public Round(IEnumerable<int> players, int durationTicks)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));
            if (durationTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationTicks));

            foreach (var player in players)
                _scores.Add(player, 0);

            if (_scores.Count < 2 || _scores.Count > 4)
                throw new ArgumentException("A round requires two to four players.", nameof(players));

            _durationTicks = durationTicks;
            RemainingTicks = durationTicks;
        }

        public int RemainingTicks { get; private set; }
        public bool IsFinished => RemainingTicks == 0;

        public void Advance(int ticks)
        {
            if (ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(ticks));

            RemainingTicks = Math.Max(0, RemainingTicks - ticks);
        }

        /// <summary>Call after validating the pickup on the host. Each spawn needs a unique coin ID.</summary>
        public bool TryCollectCoin(int playerId, int coinId)
        {
            if (IsFinished || !_scores.TryGetValue(playerId, out var score) || !_collectedCoins.Add(coinId))
                return false;

            _scores[playerId] = score + 1;
            return true;
        }

        public int GetScore(int playerId) => _scores[playerId];

        public IReadOnlyList<int> GetWinners()
        {
            if (!IsFinished)
                throw new InvalidOperationException("The round is still in progress.");

            var winners = new List<int>();
            var bestScore = -1;
            foreach (var player in _scores)
            {
                if (player.Value > bestScore)
                {
                    winners.Clear();
                    bestScore = player.Value;
                }

                if (player.Value == bestScore)
                    winners.Add(player.Key);
            }

            winners.Sort();
            return winners;
        }

        public Round CreateRematch() => new(_scores.Keys, _durationTicks);
    }
}
