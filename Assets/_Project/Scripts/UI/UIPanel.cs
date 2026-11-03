using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Base for every screen: builds its widgets once from code, then is shown and hidden by UIFlowController.</summary>
    public abstract class UIPanel : MonoBehaviour
    {
        protected UIStyle style;
        protected UIFlowController flow;
        protected Selectable firstSelected;
        bool built;

        public RectTransform Root => (RectTransform)transform;
        public bool IsShown => gameObject.activeSelf;

        public void Init(UIStyle uiStyle, UIFlowController controller)
        {
            style = uiStyle;
            flow = controller;
            if (!built)
            {
                built = true;
                Build();
            }
        }

        protected abstract void Build();

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            OnShow();
            if (firstSelected != null && firstSelected.isActiveAndEnabled && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf) OnHide();
            gameObject.SetActive(false);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected Core.GameManager Game => Core.GameManager.Instance;

        /// <summary>Creates a vertical stack of buttons centred at the given offset.</summary>
        protected RectTransform ButtonColumn(Vector2 offset, float width, int count, float spacing = 14f)
        {
            RectTransform col = UIFactory.Rect("Buttons", transform);
            UIFactory.Place(col, new Vector2(0.5f, 0.5f), offset, new Vector2(width, count * (64f + spacing)));
            UIFactory.Column(col, spacing);
            return col;
        }
    }
}
