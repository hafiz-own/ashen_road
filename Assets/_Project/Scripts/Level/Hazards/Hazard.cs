using System.Collections;
using AshenRoad.Combat;
using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Level.Hazards
{
    /// <summary>
    /// Template Method pattern: every hazard runs the same fixed cycle (telegraph, activate, stay active,
    /// deactivate, rest) in RunCycle; subclasses fill in each step. Hazards can hurt enemies too, which is how
    /// Brutes (immune to the sword) are killed.
    /// </summary>
    public abstract class Hazard : MonoBehaviour
    {
        [SerializeField] protected Hitbox hitbox;
        [SerializeField] protected int damage = 1;
        [SerializeField] protected float knockback = 6f;
        [SerializeField] protected bool harmsEnemies = true;

        Coroutine cycle;

        public bool IsActive { get; private set; }
        public Hitbox Hitbox => hitbox;

        protected virtual void Awake()
        {
            ConfigureHitbox();
        }

        protected void ConfigureHitbox()
        {
            if (hitbox == null) return;
            hitbox.SetOwner(gameObject);
            hitbox.Damage = damage;
            hitbox.Type = DamageType.Hazard;
            hitbox.Knockback = knockback;
            hitbox.Targets = Layers.PlayerMask | (harmsEnemies ? Layers.EnemyMask : 0);
        }

        protected virtual bool StartsAutomatically => true;
        /// <summary>False for one-shot hazards like falling debris.</summary>
        protected virtual bool Repeats => true;

        protected virtual void OnEnable()
        {
            if (StartsAutomatically) Begin();
        }

        protected virtual void OnDisable()
        {
            cycle = null;
            IsActive = false;
            hitbox?.Close();
        }

        public void Begin()
        {
            if (cycle == null && isActiveAndEnabled) cycle = StartCoroutine(RunCycle());
        }

        /// <summary>The template method. The order of the steps is fixed here and nowhere else.</summary>
        IEnumerator RunCycle()
        {
            yield return Initial();
            do
            {
                yield return Telegraph();
                IsActive = true;
                Activate();
                yield return WhileActive();
                Deactivate();
                IsActive = false;
                yield return Rest();
            } while (Repeats);
            cycle = null;
            Finished();
        }

        protected virtual IEnumerator Initial() { yield break; }
        protected abstract IEnumerator Telegraph();
        protected abstract void Activate();
        protected abstract IEnumerator WhileActive();
        protected abstract void Deactivate();
        protected virtual IEnumerator Rest() { yield break; }
        protected virtual void Finished() { }

        protected bool NearCamera(float range = 14f)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            return cam != null && Mathf.Abs(cam.transform.position.x - transform.position.x) < range;
        }
    }
}
