using System.Collections;
using UnityEngine;

namespace AshenRoad.UI
{
    /// <summary>Full-screen black fade used between scenes. Runs on unscaled time so it works while paused.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] float duration = 0.3f;

        CanvasGroup group;
        Coroutine running;

        public float Alpha => Group.alpha;

        CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());

        public IEnumerator FadeOutRoutine()
        {
            if (running != null) StopCoroutine(running);
            yield return Fade(1f);
        }

        public void FadeIn()
        {
            if (!isActiveAndEnabled) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Fade(0f));
        }

        IEnumerator Fade(float target)
        {
            Group.blocksRaycasts = true;
            float start = Group.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                Group.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            Group.alpha = target;
            Group.blocksRaycasts = target > 0.5f;
            running = null;
        }

        public void SetInstant(float alpha)
        {
            Group.alpha = alpha;
            Group.blocksRaycasts = alpha > 0.5f;
        }
    }
}
