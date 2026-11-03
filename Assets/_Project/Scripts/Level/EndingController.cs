using System.Collections;
using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>After the boss falls: takes control of the knight, walks him to the princess, plays the reunion, ends the game.</summary>
    public class EndingController : MonoBehaviour
    {
        [SerializeField] float reunionTime = 4.5f;

        PlayerController knight;
        Transform princess;
        Transform cage;
        bool running;

        public bool Running => running;
        public bool Done { get; private set; }

        public void Setup(PlayerController player, Vector2 princessPosition, GameObject princessPrefab)
        {
            knight = player;
            if (princessPrefab != null)
            {
                GameObject p = Instantiate(princessPrefab, princessPosition, Quaternion.identity, transform);
                princess = p.transform;
                cage = p.transform.Find("Cage");
            }
        }

        void OnEnable() => GameEvents.BossDefeated += Begin;

        void OnDisable() => GameEvents.BossDefeated -= Begin;

        public void Begin()
        {
            if (running || knight == null || knight.IsDead) return;
            running = true;
            GameManager.Instance?.BeginEnding();
            StartCoroutine(Sequence());
        }

        IEnumerator Sequence()
        {
            knight.InputLocked = true;
            knight.SetScriptedMove(0f);
            GameEvents.RaiseSound("ending_music");
            yield return new WaitForSeconds(1.2f);

            if (cage != null)
            {
                GameEvents.RaiseSound("gate_open");
                float t = 0f;
                Vector3 start = cage.localPosition;
                while (t < 1f)
                {
                    t += Time.deltaTime * 1.2f;
                    cage.localPosition = start + Vector3.up * (t * 3f);
                    yield return null;
                }
                cage.gameObject.SetActive(false);
            }

            if (princess != null)
            {
                float targetX = princess.position.x - 1.2f;
                float giveUpAt = Time.time + 12f;
                while (Mathf.Abs(knight.Position.x - targetX) > 0.25f && Time.time < giveUpAt)
                {
                    knight.SetScriptedMove(Mathf.Sign(targetX - knight.Position.x) * 0.5f);
                    yield return null;
                }
                knight.SetScriptedMove(0f);
                knight.Movement.SetFacing(1);
            }

            GameEvents.RaiseHint("The princess is safe. The Ashfall is broken.");
            for (int i = 0; i < 8; i++)
            {
                if (princess != null)
                    GameEvents.RaiseEffect(EffectType.Sparks, (Vector2)princess.position + Vector2.up * 1.2f + Random.insideUnitCircle * 0.6f, Vector2.up);
                yield return new WaitForSeconds(reunionTime / 8f);
            }
            Done = true;
            GameManager.Instance?.FinishGame();
        }
    }
}
