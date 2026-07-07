using UnityEngine;
using UnityEngine.EventSystems;

// A corner grip (bottom-right) that resizes its window on drag. Mirrors WindowDragHandle, but
// changes sizeDelta instead of position. Enforces a minimum size so windows stay usable.
public class WindowResizeHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    private DraggableWindow _window;
    private RectTransform _parentSpace;
    private Vector2 _startSize;
    private Vector2 _startPointerLocal;

    public Vector2 minSize = new Vector2(260f, 220f);

    public void Init(DraggableWindow window)
    {
        _window = window;
        _parentSpace = window.RectTransform.parent as RectTransform;
    }

    // Focus the window when you grab its resize grip.
    public void OnPointerDown(PointerEventData e) { }

    public void OnBeginDrag(PointerEventData e)
    {
        _startSize = _window.RectTransform.sizeDelta;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentSpace, e.position, e.pressEventCamera, out _startPointerLocal);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_parentSpace == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentSpace, e.position, e.pressEventCamera, out var nowLocal))
            return;

        // Pointer delta in parent space. Window pivot is centre (0.5, 0.5), so dragging the
        // bottom-right: width grows with +x movement, height grows with -y movement.
        Vector2 delta = nowLocal - _startPointerLocal;
        float newW = Mathf.Max(minSize.x, _startSize.x + delta.x);
        float newH = Mathf.Max(minSize.y, _startSize.y - delta.y);
        _window.RectTransform.sizeDelta = new Vector2(newW, newH);
    }
}
