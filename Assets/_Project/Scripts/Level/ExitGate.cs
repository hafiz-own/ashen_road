using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>The end of a level: the bars lift as the knight approaches and stepping in completes the level.</summary>
    public class ExitGate : MonoBehaviour
    {
        [SerializeField] Transform bars;
        [SerializeField] float openDistance = 5f;

        PlayerController knight;
        float openAmount;
        bool opening;

        public bool Reached { get; private set; }

        public void Setup(PlayerController player) => knight = player;

        void Update()
        {
            if (knight != null && !opening && Mathf.Abs(knight.Position.x - transform.position.x) < openDistance)
            {
                opening = true;
                GameEvents.RaiseSound("gate_open");
            }
            if (opening && openAmount < 1f && bars != null)
            {
                openAmount = Mathf.MoveTowards(openAmount, 1f, Time.deltaTime * 1.5f);
                bars.localPosition = new Vector3(0f, 1.3f + openAmount * 2.4f, 0f);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Reached) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;
            Reached = true;
            GameEvents.RaiseExitReached();
        }
    }
}
