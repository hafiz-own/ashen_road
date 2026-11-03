using AshenRoad.Player;
using AshenRoad.Player.States;
using UnityEngine;

namespace AshenRoad.Visual
{
    /// <summary>
    /// Code-driven animation for the single-frame knight sprite: run bob, squash on landing, stretch in the air,
    /// a spin for the roll, the sword arc, a blink while invulnerable and a fall on death. Purely visual.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] Transform sword;

        Vector3 squash = Vector3.one;
        float deathTilt;

        void Reset() => player = GetComponentInParent<PlayerController>();

        void LateUpdate()
        {
            if (player == null || body == null) return;
            PlayerMovement m = player.Movement;
            var state = player.Machine.Current;
            Vector2 v = m.Velocity;

            if (m.JustLanded) squash = new Vector3(1.3f, 0.7f, 1f);
            squash = Vector3.Lerp(squash, Vector3.one, Time.deltaTime * 12f);

            Vector3 scale = squash;
            Vector3 pos = Vector3.zero;
            float angle = 0f;

            if (state is PlayerGroundedState && Mathf.Abs(v.x) > 0.5f)
            {
                pos.y = Mathf.Abs(Mathf.Sin(Time.time * 16f)) * 0.07f;
                angle = -Mathf.Clamp(Mathf.Abs(v.x) * 0.8f, 0f, 6f);
            }
            else if (state is PlayerAirborneState)
            {
                float stretch = Mathf.Clamp(Mathf.Abs(v.y) / 20f, 0f, 0.2f);
                scale = new Vector3(scale.x * (1f - stretch * 0.5f), scale.y * (1f + stretch), 1f);
            }
            else if (state is PlayerRollingState)
            {
                angle = -360f * m.RollProgress;
                scale = new Vector3(0.85f, 0.85f, 1f);
                pos.y = 0.05f;
            }
            else if (state is PlayerAttackingState)
            {
                angle = Mathf.Lerp(8f, -10f, player.Combat.SwingProgress);
            }
            else if (state is PlayerHurtState)
            {
                angle = 12f;
            }

            if (state is PlayerDeadState)
            {
                deathTilt = Mathf.MoveTowards(deathTilt, 90f, Time.deltaTime * 360f);
                angle = deathTilt;
                pos.y = 0.15f;
            }
            else deathTilt = 0f;

            // rotate about the body's centre, not its feet
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 pivot = new Vector3(0f, 0.5f, 0f);
            body.localPosition = pos + pivot - rot * pivot;
            body.localRotation = rot;
            body.localScale = scale;

            if (bodyRenderer != null)
            {
                bool blink = player.Health.IsInvulnerable && !(state is PlayerRollingState) && !(state is PlayerDeadState);
                Color c = state is PlayerDeadState ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;
                c.a = blink && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 1f;
                bodyRenderer.color = c;
            }

            if (sword != null)
            {
                bool show = !(state is PlayerRollingState) && !(state is PlayerDeadState);
                sword.gameObject.SetActive(show);
                float swingAngle = player.Combat.IsSwinging ? Mathf.Lerp(130f, -80f, Mathf.SmoothStep(0f, 1f, player.Combat.SwingProgress * 1.4f)) : -30f;
                sword.localRotation = Quaternion.Euler(0f, 0f, swingAngle);
                sword.localPosition = new Vector3(player.Combat.IsSwinging ? 0.25f : 0.3f, 0.42f + pos.y, 0f);
            }
        }
    }
}
