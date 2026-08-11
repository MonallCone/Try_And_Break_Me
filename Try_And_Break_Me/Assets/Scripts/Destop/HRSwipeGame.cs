using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The HR holiday-swipe minigame. Shows holiday-request cards one at a time; the player clicks
// Approve or Reject. Score rewards a BALANCED set of decisions (approving or rejecting everything
// scores poorly). Approving every request also springs the "approve-all trap" (a story flag the
// CEO reacts to). On finish it calls WorkDay.CompleteTask(task, score).
public class HRSwipeGame
{
    private WorkTask _task;
    private WindowManager _manager;
    private DraggableWindow _window;

    private List<HolidayRequest> _requests;
    private int _index;
    private int _approved;
    private int _rejected;

    private TMP_Text _progress;
    private TMP_Text _name;
    private TMP_Text _details;
    private RectTransform _cardRoot;

    public static void Launch(WindowManager manager, WorkTask task)
    {
        var win = manager.OpenWindow("HR — Holiday Requests", new Vector2(420f, 440f));
        var game = new HRSwipeGame { _manager = manager, _task = task, _window = win };
        game._requests = HolidayRequests.Build(6);
        game.Build(win.ContentArea);
    }

    private void Build(RectTransform content)
    {
        var root = NewRect(content, "HRRoot");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.93f, 0.96f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12); vlg.spacing = 10f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        _progress = MakeText(root, "", 14, FontStyles.Bold, 22, TextAlignmentOptions.Center);

        // Card
        var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
        cardGo.GetComponent<RectTransform>().SetParent(root, false);
        cardGo.GetComponent<Image>().color = Color.white;
        var cardLe = cardGo.AddComponent<LayoutElement>();
        cardLe.flexibleHeight = 1f; cardLe.minHeight = 200f;
        _cardRoot = cardGo.GetComponent<RectTransform>();
        var cvlg = cardGo.AddComponent<VerticalLayoutGroup>();
        cvlg.padding = new RectOffset(16, 16, 16, 16); cvlg.spacing = 10f;
        cvlg.childControlWidth = true; cvlg.childControlHeight = true;
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
        cvlg.childAlignment = TextAnchor.UpperCenter;

        _name = MakeText(_cardRoot, "", 22, FontStyles.Bold, 34, TextAlignmentOptions.Center);
        _details = MakeText(_cardRoot, "", 16, FontStyles.Normal, 120, TextAlignmentOptions.Center);
        _details.textWrappingMode = TextWrappingModes.Normal;

        // Buttons row
        var rowGo = new GameObject("Buttons", typeof(RectTransform));
        rowGo.GetComponent<RectTransform>().SetParent(root, false);
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.minHeight = 48f; rowLe.preferredHeight = 48f;
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        MakeButton(rowGo.GetComponent<RectTransform>(), "Reject", new Color(0.75f, 0.28f, 0.28f), () => Decide(false));
        MakeButton(rowGo.GetComponent<RectTransform>(), "Approve", new Color(0.28f, 0.6f, 0.35f), () => Decide(true));

        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (_index >= _requests.Count) { Finish(); return; }
        var r = _requests[_index];
        _progress.text = $"Request {_index + 1} of {_requests.Count}";
        _name.text = r.name;
        _details.text = $"<b>{r.days} day{(r.days == 1 ? "" : "s")}</b>   ({r.dates})\n\n\"{r.reason}\"";
    }

    private void Decide(bool approve)
    {
        if (approve) _approved++; else _rejected++;
        _index++;
        ShowCurrent();
    }

    private void Finish()
    {
        int total = _requests.Count;

        // Score = percentage of requests APPROVED, so the bot's comment reflects what actually
        // happened (lots approved = high number) rather than an abstract judgement.
        int score = total > 0 ? Mathf.RoundToInt(100f * _approved / total) : 0;

        // Approve-all trap: approved every single request (independent of the score number now).
        if (_approved == total && total > 0)
        {
            if (GameState.I) GameState.I.SetFlag("hr_approved_all");
            Debug.Log("[HR] approve-all trap sprung.");
        }
        // Reject-all: also a story branch (CEO's sardonic 'great job' email).
        if (_rejected == total && total > 0)
        {
            if (GameState.I) GameState.I.SetFlag("hr_rejected_all");
        }

        Debug.Log($"[HR] done. approved {_approved}/{total} ({score}%).");
        WorkDay.CompleteTask(_task, score);
        if (_manager != null && _window != null) _manager.CloseWindow(_window);
    }

    // ---- helpers ----
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
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, FontStyles style, float h, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = Color.black; t.alignment = align;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h;
        return t;
    }
    private static void MakeButton(RectTransform parent, string label, Color color, System.Action onClick)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        var lblGo = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.SetParent(go.transform, false);
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = label; lbl.fontSize = 17f; lbl.color = Color.white; lbl.alignment = TextAlignmentOptions.Center;
    }
}