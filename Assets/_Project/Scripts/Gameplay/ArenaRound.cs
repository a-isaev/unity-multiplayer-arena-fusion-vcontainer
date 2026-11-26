using System;
using System.Collections.Generic;
using System.Linq;
using CoinRush.Core;
using Fusion;
using UnityEngine;

namespace CoinRush.Gameplay
{
    public struct RoundScore : INetworkStruct
    {
        public int PlayerId;
        public int ConnectedPlayerId;
        public int Coins;
    }

    public sealed class ArenaRound : NetworkBehaviour, IAfterTick
    {
        private const int MaxPlayers = 4;
        private const int CoinCount = 25;
        private const int AllCoins = (1 << CoinCount) - 1;
        private const float PickupRadius = 0.85f;

        [Networked] private int PlayerCount { get; set; }
        [Networked] private int SecondsLeft { get; set; }
        [Networked] private NetworkBool Finished { get; set; }
        [Networked] private int CoinsOnFloor { get; set; }
        [Networked] private int WinnerCount { get; set; }
        [Networked] private int WinnerId { get; set; }
        [Networked, Capacity(MaxPlayers)] private NetworkArray<RoundScore> Scores => default;

        [SerializeField] private Material _surface;

        private readonly GameObject[] _coins = new GameObject[CoinCount];
        private Round _round;
        private List<PlayerRef> _participants;
        private readonly Dictionary<Guid, int> _reservedSlots = new();
        private float _durationSeconds;
        private int _coinWave;
        private int _renderedCoins;
        private int _roundNumber;
        private bool _startRequested;
        private TickTimer _nextWave;
        private readonly ArenaPlayer[] _pickupPlayers = new ArenaPlayer[MaxPlayers];

        internal void SetSurface(Material surface) => _surface = surface;

        internal void SetDuration(float seconds) => _durationSeconds = seconds;

        internal bool IsRunning => _round != null && !_round.IsFinished;

        internal bool CanReconnect(byte[] token) =>
            TryGetReservedSlot(token, out var slot) && _participants[slot] == PlayerRef.None;

        internal void Reconnect(PlayerRef player, byte[] token)
        {
            if (!IsRunning || !TryGetReservedSlot(token, out var slot))
                return;

            _participants[slot] = player;
            var score = Scores[slot];
            score.ConnectedPlayerId = player.PlayerId;
            Scores.Set(slot, score);
        }

        internal void Disconnect(PlayerRef player)
        {
            if (!IsRunning)
                return;

            var slot = _participants.IndexOf(player);
            if (slot < 0)
                return;

            _participants[slot] = PlayerRef.None;
            var score = Scores[slot];
            score.ConnectedPlayerId = 0;
            Scores.Set(slot, score);
        }

        private bool TryGetReservedSlot(byte[] token, out int slot)
        {
            slot = -1;
            return token?.Length == 16 && _reservedSlots.TryGetValue(new Guid(token), out slot);
        }

        public override void Spawned()
        {
            _renderedCoins = 0;
            var gold = new MaterialPropertyBlock();
            gold.SetColor("_BaseColor", new Color(1.8f, 1.23f, 0.24f));
            for (var i = 0; i < CoinCount; i++)
            {
                var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = $"Coin {i + 1}";
                coin.transform.SetParent(transform, false);
                coin.transform.localPosition = CoinPosition(i);
                coin.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);
                if (coin.TryGetComponent<CapsuleCollider>(out var collider))
                    collider.enabled = false;
                var renderer = coin.GetComponent<Renderer>();
                renderer.sharedMaterial = _surface;
                renderer.SetPropertyBlock(gold);
                coin.SetActive(false);
                _coins[i] = coin;
            }
        }

        public void AfterTick()
        {
            if (!HasStateAuthority || !Runner.IsForward)
                return;

            if (_startRequested)
            {
                _startRequested = false;
                if (_round == null || _round.IsFinished)
                    StartRound();
            }

            if (_round == null || _round.IsFinished)
                return;

            CollectCoins();

            _round.Advance(1);
            SecondsLeft = Mathf.CeilToInt(_round.RemainingTicks * Runner.DeltaTime);
            if (_round.IsFinished)
            {
                var winners = _round.GetWinners();
                WinnerCount = winners.Count;
                WinnerId = winners[0];
                Finished = true;
                CoinsOnFloor = 0;
            }
        }

        public override void Render()
        {
            var visibleCoins = CoinsOnFloor;
            var changed = visibleCoins ^ _renderedCoins;
            if (changed == 0)
                return;

            for (var i = 0; i < CoinCount; i++)
            {
                var flag = 1 << i;
                if ((changed & flag) != 0)
                    _coins[i].SetActive((visibleCoins & flag) != 0);
            }

            _renderedCoins = visibleCoins;
        }

        private void StartRound()
        {
            _participants = Runner.CommittedPlayers
                .Where(player => Runner.TryGetPlayerObject(player, out _))
                .OrderBy(player => player.PlayerId)
                .ToList();
            if (_participants.Count < 2 || _participants.Count > MaxPlayers)
                return;

            var durationTicks = Mathf.Max(1, Mathf.RoundToInt(_durationSeconds / Runner.DeltaTime));
            _round = new Round(_participants.Select(player => player.PlayerId), durationTicks);
            _reservedSlots.Clear();
            for (var i = 0; i < _participants.Count; i++)
            {
                var player = _participants[i];
                var id = player.PlayerId;
                Scores.Set(i, new RoundScore { PlayerId = id, ConnectedPlayerId = id });
                var token = Runner.GetPlayerConnectionToken(player);
                if (token?.Length == 16)
                    _reservedSlots.TryAdd(new Guid(token), i);
            }

            _roundNumber++;
            _coinWave = 0;
            _nextWave = TickTimer.None;
            PlayerCount = _participants.Count;
            SecondsLeft = Mathf.CeilToInt(_durationSeconds);
            Finished = false;
            CoinsOnFloor = AllCoins;
        }

        private void CollectCoins()
        {
            var available = CoinsOnFloor;
            if (available == 0)
            {
                if (_nextWave.Expired(Runner))
                {
                    _coinWave++;
                    CoinsOnFloor = AllCoins;
                }

                return;
            }

            for (var slot = 0; slot < _participants.Count; slot++)
            {
                _pickupPlayers[slot] = _participants[slot] != PlayerRef.None &&
                    Runner.TryGetPlayerObject(_participants[slot], out var avatar)
                    ? avatar.GetComponent<ArenaPlayer>()
                    : null;
            }

            for (var i = 0; i < CoinCount; i++)
            {
                var flag = 1 << i;
                if ((available & flag) == 0)
                    continue;

                var coin = CoinPosition(i);
                var firstContact = float.PositiveInfinity;
                var winner = -1;
                for (var slot = 0; slot < _participants.Count; slot++)
                {
                    var player = _pickupPlayers[slot];
                    if (player == null)
                        continue;

                    var start = player.GetPickupStart() - coin;
                    var end = player.transform.position - coin;
                    var contact = CoinPickup.EntryTime(start.x, start.z, end.x, end.z, PickupRadius);
                    if (contact >= firstContact)
                        continue;

                    firstContact = contact;
                    winner = slot;
                }

                // Participants are sorted by ID, which breaks exact same-tick ties consistently.
                if (winner < 0)
                    continue;

                var entry = Scores[winner];
                var playerId = entry.PlayerId;
                if (!_round.TryCollectCoin(playerId, _coinWave * CoinCount + i))
                    continue;

                available &= ~flag;
                var score = _round.GetScore(playerId);
                entry.Coins = score;
                Scores.Set(winner, entry);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Coin Rush] Pickup round={_roundNumber} tick={Runner.Tick} wave={_coinWave + 1} coin={i + 1} player={playerId} score={score}");
#endif
            }

            CoinsOnFloor = available;
            if (available == 0)
                _nextWave = TickTimer.CreateFromSeconds(Runner, 1f);
        }

        private void OnGUI()
        {
            if (Object == null || !Object.IsValid)
                return;

            ArenaScreenLayout.GetRects(out _, out _, out var scorePanel, out _);
            GUILayout.BeginArea(scorePanel, GUI.skin.box);
            if (PlayerCount == 0)
            {
                GUILayout.Label("Waiting for the host to start the round");
            }
            else
            {
                GUILayout.Label(Finished
                    ? WinnerCount > 1 ? "Draw" : $"Player {WinnerId} wins"
                    : $"Time: {SecondsLeft / 60:00}:{SecondsLeft % 60:00}");
                for (var i = 0; i < PlayerCount; i++)
                {
                    var score = Scores[i];
                    var name = $"Player {score.PlayerId}";
                    if (score.ConnectedPlayerId == Runner.LocalPlayer.PlayerId)
                        name += " (you)";
                    else if (!Finished && score.ConnectedPlayerId == 0)
                        name += " (disconnected)";
                    GUILayout.Label($"{name}: {score.Coins}");
                }
            }

            if (HasStateAuthority && (PlayerCount == 0 || Finished))
            {
                GUI.enabled = Runner.CommittedPlayers.Count() >= 2;
                if (GUILayout.Button(Finished ? "Play again" : "Start round"))
                    _startRequested = true;
                GUI.enabled = true;
            }
            GUILayout.EndArea();
        }

        private static Vector3 CoinPosition(int index) =>
            new Vector3((index % 5 - 2) * 3f, 0.14f, (index / 5 - 2) * 3f);
    }
}
