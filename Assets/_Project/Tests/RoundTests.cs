using System;
using CoinRush.Core;
using NUnit.Framework;

namespace CoinRush.Tests
{
    public sealed class RoundTests
    {
        [TestCase(1)]
        [TestCase(5)]
        public void RoundRejectsUnsupportedPlayerCount(int playerCount)
        {
            var players = new int[playerCount];
            for (var i = 0; i < players.Length; i++)
                players[i] = i + 1;

            Assert.Throws<ArgumentException>(() => new Round(players, 10));
        }

        [Test]
        public void CoinCanOnlyBeAwardedOnce()
        {
            var round = new Round(new[] { 1, 2 }, 10);

            Assert.That(round.TryCollectCoin(1, 42), Is.True);
            Assert.That(round.TryCollectCoin(2, 42), Is.False);
            Assert.That(round.GetScore(1), Is.EqualTo(1));
            Assert.That(round.GetScore(2), Is.Zero);
        }

        [Test]
        public void PickupGoesToFirstContactEvenIfOtherPlayerEndsCloser()
        {
            var dasher = CoinPickup.EntryTime(-1f, 0f, -0.7f, 0f, 0.85f);
            var pushedPlayer = CoinPickup.EntryTime(1.3f, 0f, 0.65f, 0f, 0.85f);

            Assert.That(dasher, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(dasher, Is.LessThan(pushedPlayer));
        }

        [Test]
        public void PickupDetectsCrossingAndRejectsNearMiss()
        {
            Assert.That(CoinPickup.EntryTime(-2f, 0f, 2f, 0f, 0.85f),
                Is.EqualTo(0.2875f).Within(0.0001f));
            Assert.That(CoinPickup.EntryTime(-2f, 0.9f, 2f, 0.9f, 0.85f),
                Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void UnknownPlayerCannotClaimCoin()
        {
            var round = new Round(new[] { 1, 2 }, 10);

            Assert.That(round.TryCollectCoin(3, 42), Is.False);
            Assert.That(round.TryCollectCoin(1, 42), Is.True);
        }

        [Test]
        public void ClockStopsAtZeroAndRejectsLatePickups()
        {
            var round = new Round(new[] { 1, 2 }, 10);

            round.Advance(9);
            Assert.That(round.RemainingTicks, Is.EqualTo(1));
            Assert.That(round.IsFinished, Is.False);
            Assert.Throws<InvalidOperationException>(() => round.GetWinners());

            round.Advance(3);
            Assert.That(round.RemainingTicks, Is.Zero);
            Assert.That(round.IsFinished, Is.True);
            Assert.That(round.TryCollectCoin(1, 42), Is.False);
        }

        [Test]
        public void HighestScoreWins()
        {
            var round = new Round(new[] { 1, 2, 3, 4 }, 10);
            round.TryCollectCoin(1, 41);
            round.TryCollectCoin(2, 42);
            round.TryCollectCoin(2, 43);
            round.Advance(10);

            Assert.That(round.GetWinners(), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void DrawIncludesOnlyPlayersWithTheHighestScore()
        {
            var round = new Round(new[] { 3, 2, 1 }, 10);
            round.TryCollectCoin(3, 42);
            round.TryCollectCoin(1, 43);
            round.Advance(10);

            Assert.That(round.GetWinners(), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void RematchResetsClockScoresAndCoinClaims()
        {
            var round = new Round(new[] { 1, 2 }, 10);
            round.TryCollectCoin(1, 42);
            round.Advance(10);

            var rematch = round.CreateRematch();

            Assert.That(rematch.RemainingTicks, Is.EqualTo(10));
            Assert.That(rematch.IsFinished, Is.False);
            Assert.That(rematch.GetScore(1), Is.Zero);
            Assert.That(rematch.GetScore(2), Is.Zero);
            Assert.That(rematch.TryCollectCoin(2, 42), Is.True);
            Assert.That(round.GetWinners(), Is.EqualTo(new[] { 1 }));
        }
    }
}
