using AshenRoad.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AshenRoad.UI
{
    /// <summary>Plays a tick when a menu item is selected with the keyboard, gamepad or mouse.</summary>
    public class ButtonSelectSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public void OnSelect(BaseEventData eventData) => GameEvents.RaiseSound("ui_move");

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
