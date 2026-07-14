using UnityEngine;
using UnityEngine.EventSystems;

// Sits on the title bar. Dragging it moves the parent window.
public class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    private DraggableWindow _window;
    private Vector2 _pointerOffset;
    private RectTransform _parentSpace;

    public void Init(DraggableWindow window)
    {
        _window = window;
        _parentSpace = window.RectTransform.parent as RectTransform;
    }

    public void OnPointerDown(PointerEventData e)
    {
        // Focusing is handled by the window itself; nothing needed here beyond enabling drag.
    }

    public void OnBeginDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _window.RectTransform, e.position, e.pressEventCamera, out _pointerOffset);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_parentSpace == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentSpace, e.position, e.pressEventCamera, out var localPoint))
        {
            _window.RectTransform.anchoredPosition = localPoint - _pointerOffset;
        }
    }
}
