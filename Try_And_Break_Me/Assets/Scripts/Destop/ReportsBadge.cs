using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A "new" badge for the Reports desktop icon. Once Alex fixes Reports (the "reports_fixed" flag),
// a red count appears in the icon's top-right to draw the player's eye to the now-working app.
// It clears once the player has opened Reports (the "reports_opened" flag). Attach to the Reports
// icon. Polls GameState each frame (cheap) since flags don't raise events.
public class ReportsBadge : MonoBehaviour
{
    public int count = 1;   // what the badge shows when active

    private GameObject _badge;
    private TMP_Text _count;

    private void Awake() { BuildBadge(); }

    private void BuildBadge()
    {
        _badge = new GameObject("ReportsBadge", typeof(RectTransform), typeof(Image));
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
        _count.text = count.ToString();

        _badge.SetActive(false);
    }

    private void Update()
    {
        if (GameState.I == null) { if (_badge.activeSelf) _badge.SetActive(false); return; }
        // Show the badge only when Reports is fixed AND hasn't been opened yet.
        bool show = GameState.I.HasFlag("reports_fixed") && !GameState.I.HasFlag("reports_opened");
        if (_badge.activeSelf != show) _badge.SetActive(show);
    }
}