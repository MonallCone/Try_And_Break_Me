using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The Email app: inbox list on the left, reading pane on the right. Built in code inside a
// spawned window. The story delivers emails via Deliver(...) at the right beats. Registers
// itself statically so story code can reach the open inbox from anywhere.
public class EmailApp
{
    public static EmailApp Current { get; private set; }   // the open email window, if any

    private RectTransform _listContent;
    private TMP_Text _readFrom;
    private TMP_Text _readSubject;
    private TMP_Text _readBody;
    private readonly Dictionary<EmailData, TextMeshProUGUI> _rowLabels = new Dictionary<EmailData, TextMeshProUGUI>();

    public void Build(RectTransform content)
    {
        Current = this;

        var root = NewRect(content, "EmailRoot");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.92f, 0.92f, 0.94f, 1f);
        var hlg = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f; hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        BuildInboxList(root);   // left
        BuildReadingPane(root); // right

        // Populate from the persistent mailbox, and refresh when it changes while open.
        foreach (var email in Mailbox.Emails) AddRow(email);
        Mailbox.Changed += OnMailboxChanged;

        // Clean up the subscription when this window's content is destroyed.
        var relay = root.gameObject.AddComponent<DestroyRelay>();
        relay.onDestroy = () => { Mailbox.Changed -= OnMailboxChanged; if (Current == this) Current = null; };
    }

    // Rebuild the row list to match the mailbox (simple + safe for the low email volume here).
    private void OnMailboxChanged()
    {
        foreach (Transform child in _listContent) Object.Destroy(child.gameObject);
        _rowLabels.Clear();
        foreach (var email in Mailbox.Emails) AddRow(email);
    }

    // ---- left: inbox list ----
    private void BuildInboxList(RectTransform parent)
    {
        var scrollGo = new GameObject("Inbox", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.88f, 1f);
        var le = scrollGo.AddComponent<LayoutElement>();
        le.preferredWidth = 210f; le.minWidth = 210f;
        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scrollGo.GetComponent<RectTransform>().SetParent(parent, false);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var viewRt = viewportGo.GetComponent<RectTransform>();
        viewRt.SetParent(scrollGo.transform, false);
        viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one; viewRt.offsetMin = new Vector2(2, 0); viewRt.offsetMax = new Vector2(-2, 0);
        viewRt.pivot = new Vector2(0, 1);
        viewportGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        scroll.viewport = viewRt;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        _listContent = contentGo.GetComponent<RectTransform>();
        _listContent.SetParent(viewRt, false);
        _listContent.anchorMin = new Vector2(0, 1); _listContent.anchorMax = new Vector2(1, 1);
        _listContent.pivot = new Vector2(0.5f, 1);
        _listContent.anchoredPosition = Vector2.zero;   // don't let the stretched rect drift sideways
        _listContent.sizeDelta = new Vector2(0, _listContent.sizeDelta.y);
        var cvlg = contentGo.AddComponent<VerticalLayoutGroup>();
        cvlg.childControlWidth = true; cvlg.childControlHeight = true;
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
        cvlg.spacing = 2f; cvlg.padding = new RectOffset(4, 4, 2, 2);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _listContent;
    }

    private void AddRow(EmailData email)
    {
        var rowGo = new GameObject($"Row_{email.id}", typeof(RectTransform), typeof(Image), typeof(Button));
        rowGo.GetComponent<RectTransform>().SetParent(_listContent, false);
        rowGo.GetComponent<Image>().color = Color.white;
        var le = rowGo.AddComponent<LayoutElement>();
        le.minHeight = 62f; le.preferredHeight = 62f;
        rowGo.GetComponent<Button>().onClick.AddListener(() => OpenEmail(email));

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(rowGo.transform, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(12, 2); labelRt.offsetMax = new Vector2(-10, -2);
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.fontSize = 17f; label.color = Color.black;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        _rowLabels[email] = label;
        RefreshRowLabel(email);
    }

    private void RefreshRowLabel(EmailData email)
    {
        if (!_rowLabels.TryGetValue(email, out var label)) return;
        string dot = email.unread ? "\u25CF " : "";
        string weight = email.unread ? "<b>" : "";
        string weightEnd = email.unread ? "</b>" : "";
        label.text = $"{weight}{dot}{email.from}{weightEnd}\n<size=13>{email.subject}</size>";
    }

    // ---- right: reading pane ----
    private void BuildReadingPane(RectTransform parent)
    {
        var paneGo = new GameObject("Reading", typeof(RectTransform), typeof(Image));
        paneGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        var le = paneGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        var paneRt = paneGo.GetComponent<RectTransform>();
        paneRt.SetParent(parent, false);
        var vlg = paneGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f; vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        _readFrom = MakeText(paneRt, "", 18, FontStyles.Bold);
        _readSubject = MakeText(paneRt, "Select an email to read.", 21, FontStyles.Bold);
        _readBody = MakeText(paneRt, "", 18f, FontStyles.Normal);
    }

    private void OpenEmail(EmailData email)
    {
        if (email.unread) { Mailbox.MarkRead(email); RefreshRowLabel(email); }
        _readFrom.text = $"From: {email.from}";
        _readSubject.text = email.subject;
        _readBody.text = email.body;
        if (email.repliedByLauren)
            _readBody.text += "\n\n<i><color=#4a7a4a>\u2014\u2014\u2014\nLauren: Don't worry, I already replied to this one for you. You're welcome! \u2665</color></i>";
        email.onOpen?.Invoke();
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
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, FontStyles style)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = Color.black;
        t.textWrappingMode = TextWrappingModes.Normal;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = size + 6f;
        return t;
    }
}