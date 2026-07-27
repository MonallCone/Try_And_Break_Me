using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A small unread-count badge for the Email desktop icon. Attach to the email icon; it creates a
// red circle with the unread number in the icon's top-right and updates whenever the Mailbox
// changes. Hides itself when there are no unread emails.
public class EmailBadge : MonoBehaviour
{
    private GameObject _badge;
    private TMP_Text _count;

    private void Awake()
    {
        BuildBadge();
    }

    private void OnEnable()
    {
        Mailbox.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Mailbox.Changed -= Refresh;
    }

    private void BuildBadge()
    {
        _badge = new GameObject("UnreadBadge", typeof(RectTransform), typeof(Image));
        var rt = _badge.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(26, 26);
        rt.anchoredPosition = new Vector2(6, 6);   // pokes out the top-right corner
        _badge.GetComponent<Image>().color = new Color(0.85f, 0.15f, 0.15f);

        var txtGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.SetParent(_badge.transform, false);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
        _count = txtGo.GetComponent<TextMeshProUGUI>();
        _count.fontSize = 15f; _count.color = Color.white; _count.fontStyle = FontStyles.Bold;
        _count.alignment = TextAlignmentOptions.Center;
    }

    private void Refresh()
    {
        int n = Mailbox.UnreadCount;
        if (_badge != null) _badge.SetActive(n > 0);
        if (_count != null) _count.text = n > 9 ? "9+" : n.ToString();
    }
}
