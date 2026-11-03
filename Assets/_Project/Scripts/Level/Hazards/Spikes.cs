using System.Collections;
using UnityEngine;

namespace AshenRoad.Level.Hazards
{
    /// <summary>Always-on spikes. The cycle has no telegraph and never ends.</summary>
    public class Spikes : Hazard
    {
        [SerializeField] float rehitInterval = 0.5f;

        protected override IEnumerator Telegraph() { yield break; }

        protected override void Activate() => hitbox?.Open();

        protected override IEnumerator WhileActive()
        {
            var wait = new WaitForSeconds(rehitInterval);
            while (true)
            {
                yield return wait;
                hitbox?.ResetHits();
            }
        }

        protected override void Deactivate() => hitbox?.Close();
    }
}
