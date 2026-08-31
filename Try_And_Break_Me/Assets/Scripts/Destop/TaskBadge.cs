using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A small remaining-tasks badge for the Tasks desktop icon. Attach to the Tasks icon; it creates a
// red circle with the number of To Do tasks in the icon's top-right and updates whenever the work
// day changes. Hides itself when there are no outstanding tasks.
public class TaskBadge : MonoBehaviour
{
    private GameObject _badge;
    private TMP_Text _count;

    private void Awake()
    {
        BuildBadge();
    }

    private void OnEnable()
    {
        WorkDay.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        WorkDay.Changed -= Refresh;
    }

    private void BuildBadge()
    {
        _badge = new GameObject("TaskBadge", typeof(RectTransform), typeof(Image));
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
        // Remaining To Do tasks = quota minus completed.
        int n = WorkDay.Quota - WorkDay.CompletedCount;
        if (n < 0) n = 0;
        if (_badge != null) _badge.SetActive(n > 0);
        if (_count != null) _count.text = n > 9 ? "9+" : n.ToString();
    }
}