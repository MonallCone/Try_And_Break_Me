using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The Tasks app: a ticket queue. Header shows the daily quota (e.g. "Day 1 - 1/3 completed").
// A To Do list and a Completed list. Clicking a To Do ticket launches its minigame via the
// MinigameLauncher; when the game finishes it reports a score and the ticket moves to Completed.
public class TasksApp
{
    public static TasksApp Current { get; private set; }

    private System.Action<WorkTask> _onLaunch;   // how a ticket opens its minigame
    private RectTransform _todoContent;
    private RectTransform _doneContent;
    private TMP_Text _header;

    public void Build(RectTransform content, System.Action<WorkTask> onLaunch)
    {
        Current = this;
        _onLaunch = onLaunch;

        var root = NewRect(content, "TasksRoot");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.93f, 0.95f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f; vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        _header = MakeText(root, "", 20, FontStyles.Bold, 30);

        MakeText(root, "To do", 17, FontStyles.Bold, 24);
        _todoContent = BuildListSection(root, flexible: true);

        MakeText(root, "Completed", 17, FontStyles.Bold, 24);
        _doneContent = BuildListSection(root, flexible: true);

        WorkDay.Changed += Refresh;
        var relay = root.gameObject.AddComponent<DestroyRelay>();
        relay.onDestroy = () => { WorkDay.Changed -= Refresh; if (Current == this) Current = null; };

        Refresh();
    }

    private void Refresh()
    {
        if (_header != null)
            _header.text = $"Day {WorkDay.Day}    \u2014    {WorkDay.CompletedCount}/{WorkDay.Quota} completed";

        RebuildList(_todoContent, TaskStatus.ToDo, clickable: true);
        RebuildList(_doneContent, TaskStatus.Completed, clickable: false);
    }

    private void RebuildList(RectTransform container, TaskStatus status, bool clickable)
    {
        if (container == null) return;
        foreach (Transform child in container) Object.Destroy(child.gameObject);

        foreach (var task in WorkDay.Tasks)
        {
            if (task.status != status) continue;
            AddTicketRow(container, task, clickable);
        }
    }

    private void AddTicketRow(RectTransform parent, WorkTask task, bool clickable)
    {
        var rowGo = new GameObject($"Ticket_{task.id}", typeof(RectTransform), typeof(Image));
        rowGo.GetComponent<RectTransform>().SetParent(parent, false);
        rowGo.GetComponent<Image>().color = clickable ? Color.white : new Color(0.9f, 0.92f, 0.9f);
        var le = rowGo.AddComponent<LayoutElement>();
        le.minHeight = 40f; le.preferredHeight = 40f;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(rowGo.transform, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(10, 0); labelRt.offsetMax = new Vector2(-10, 0);
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.fontSize = 16f; label.color = Color.black; label.alignment = TextAlignmentOptions.Left;
        label.textWrappingMode = TextWrappingModes.Normal;

        if (task.status == TaskStatus.Completed)
            label.text = $"\u2713 {task.title}   <color=#666666>(score {task.score})</color>";
        else
            label.text = task.title;

        if (clickable)
        {
            var btn = rowGo.AddComponent<Button>();
            btn.onClick.AddListener(() => _onLaunch?.Invoke(task));
        }
    }

    // ---- helpers ----
    private RectTransform BuildListSection(RectTransform parent, bool flexible)
    {
        var scrollGo = new GameObject("List", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.88f);
        scrollGo.GetComponent<RectTransform>().SetParent(parent, false);
        var le = scrollGo.AddComponent<LayoutElement>();
        if (flexible) le.flexibleHeight = 1f;
        le.minHeight = 80f;
        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var viewRt = viewportGo.GetComponent<RectTransform>();
        viewRt.SetParent(scrollGo.transform, false);
        viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = new Vector2(2, 0); viewRt.offsetMax = new Vector2(-2, 0);
        viewRt.pivot = new Vector2(0, 1);
        viewportGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        scroll.viewport = viewRt;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        var crt = contentGo.GetComponent<RectTransform>();
        crt.SetParent(viewRt, false);
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0, crt.sizeDelta.y);
        var cvlg = contentGo.AddComponent<VerticalLayoutGroup>();
        cvlg.childControlWidth = true; cvlg.childControlHeight = true;
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
        cvlg.spacing = 3f; cvlg.padding = new RectOffset(4, 4, 4, 4);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = crt;

        return crt;
    }

    private static RectTransform NewRect(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, FontStyles style, float h)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = Color.black;
        t.alignment = TextAlignmentOptions.Left;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h;
        return t;
    }
}