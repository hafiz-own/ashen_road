using AshenRoad.Combat;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>The bottom of every pit: anything with health that falls in dies.</summary>
    public class KillZone : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null && target.IsAlive)
                target.TakeDamage(DamageInfo.Lethal(DamageType.Fall, other.transform.position));
        }
    }
}
