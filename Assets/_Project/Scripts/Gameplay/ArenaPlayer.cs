using Fusion;
using UnityEngine;

namespace CoinRush.Gameplay
{
    public struct ArenaInput : INetworkInput
    {
        public const int DashButton = 0;
        public Vector2 Move;
        public NetworkButtons Buttons;
    }

    public sealed class ArenaPlayer : NetworkBehaviour
    {
        private const float Speed = 5f;
        private const float Limit = 7f;
        private const float DashSpeed = 17f;
        private const float DashDuration = 0.18f;
        private const float DashCooldown = 1.5f;
        private const float PushSpeed = 12f;
        private const float PushDuration = 0.20f;
        private const float ContactRadius = 0.8f;

        [Networked] private NetworkButtons PreviousButtons { get; set; }
        [Networked] private Vector2 Facing { get; set; }
        [Networked] private Vector2 DashDirection { get; set; }
        [Networked] private TickTimer DashEnd { get; set; }
        [Networked] private TickTimer DashReady { get; set; }
        [Networked] private NetworkBool HitDuringDash { get; set; }
        [Networked] private Vector2 PushDirection { get; set; }
        [Networked] private TickTimer PushEnd { get; set; }

        private Vector3 _pickupStart;
        private Tick _pickupTick;

        internal Vector3 GetPickupStart() => _pickupTick == Runner.Tick ? _pickupStart : transform.position;

        public float DashCooldownLeft => DashReady.RemainingTime(Runner) ?? 0f;

        private Renderer _renderer;
        private MaterialPropertyBlock _colorBlock;
        private Color _baseColor;
        private bool _wasGlowing;

        public override void Spawned()
        {
            _baseColor = HasInputAuthority ? new Color(0.15f, 0.8f, 1f) : new Color(1f, 0.5f, 0.17f);
            _renderer = GetComponent<Renderer>();
            _colorBlock = new MaterialPropertyBlock();
            ApplyColor(false);
        }

        public override void Render()
        {
            var glowing = !DashEnd.ExpiredOrNotRunning(Runner);
            if (glowing != _wasGlowing)
                ApplyColor(glowing);
        }

        private void ApplyColor(bool glowing)
        {
            var color = _baseColor;
            if (glowing)
                color = new Color(color.r * 2.2f, color.g * 2.2f, color.b * 2.2f);
            _colorBlock.SetColor("_BaseColor", color);
            _renderer.SetPropertyBlock(_colorBlock);
            _wasGlowing = glowing;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority && !HasInputAuthority)
                return;

            var move = Vector2.zero;
            if (GetInput<ArenaInput>(out var input))
            {
                if (float.IsFinite(input.Move.x) && float.IsFinite(input.Move.y))
                    move = Vector2.ClampMagnitude(input.Move, 1f);

                if (move.sqrMagnitude > 0f)
                    Facing = move.normalized;

                var pressedDash = input.Buttons.WasPressed(PreviousButtons, ArenaInput.DashButton);
                PreviousButtons = input.Buttons;
                if (pressedDash && DashReady.ExpiredOrNotRunning(Runner) && PushEnd.ExpiredOrNotRunning(Runner))
                {
                    DashDirection = Facing.sqrMagnitude > 0f ? Facing : Vector2.up;
                    DashEnd = TickTimer.CreateFromSeconds(Runner, DashDuration);
                    DashReady = TickTimer.CreateFromSeconds(Runner, DashCooldown);
                    HitDuringDash = false;
                }
            }

            var isDashing = !DashEnd.ExpiredOrNotRunning(Runner);
            var velocity = isDashing ? DashDirection * DashSpeed : move * Speed;
            if (!PushEnd.ExpiredOrNotRunning(Runner))
                velocity = PushDirection * PushSpeed;

            var start = transform.position;
            _pickupStart = start;
            _pickupTick = Runner.Tick;
            var end = start;
            end.x = Mathf.Clamp(start.x + velocity.x * Runner.DeltaTime, -Limit, Limit);
            end.z = Mathf.Clamp(start.z + velocity.y * Runner.DeltaTime, -Limit, Limit);
            transform.position = end;

            if (HasStateAuthority && Runner.IsForward && isDashing && !HitDuringDash)
                TryPushOpponent(start, end);
        }

        private void TryPushOpponent(Vector3 start, Vector3 end)
        {
            var origin = new Vector2(start.x, start.z);
            var segment = new Vector2(end.x - start.x, end.z - start.z);
            if (segment.sqrMagnitude <= Mathf.Epsilon)
                return;

            ArenaPlayer closest = null;
            var firstContact = float.PositiveInfinity;
            foreach (var player in Runner.CommittedPlayers)
            {
                if (player == Object.InputAuthority || !Runner.TryGetPlayerObject(player, out var avatar))
                    continue;

                var target = avatar.transform.position;
                var offset = new Vector2(target.x, target.z) - origin;
                if (Vector2.Dot(offset, DashDirection) < 0f)
                    continue;

                var along = Mathf.Clamp01(Vector2.Dot(offset, segment) / segment.sqrMagnitude);
                if (along >= firstContact || (offset - segment * along).sqrMagnitude > ContactRadius * ContactRadius)
                    continue;

                closest = avatar.GetComponent<ArenaPlayer>();
                firstContact = along;
            }

            if (closest == null)
                return;

            closest.PushDirection = DashDirection;
            closest.PushEnd = TickTimer.CreateFromSeconds(Runner, PushDuration);
            closest.DashEnd = TickTimer.None;
            HitDuringDash = true;
        }
    }
}
