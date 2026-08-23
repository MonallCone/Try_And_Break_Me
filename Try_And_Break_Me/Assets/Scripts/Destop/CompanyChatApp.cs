using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A tabbed multi-channel team-chat app.
//   "Company" channel: the whole company minus Steven. Noisy ambient churn; member count = roster.
//   "My Team" channel: the player is the SOLE survivor of their team (cyber/help-desk/HR merged
//        into one person after turnover). Silent, member count = 1, with "X left the channel"
//        ghost lines so the emptiness feels earned.
//
// MonoBehaviour because the ambient channel posts on a timer. Member counts are live (a channel's
// Members list can be changed later by the story, and the header updates).
public class CompanyChatApp : MonoBehaviour
{
    public enum ChannelMode { Ambient, Silent, Calm }

    // The live instance (if the window is open), so the story can flip it at runtime.
    public static CompanyChatApp Current { get; private set; }
    // Persists across window open/close: once true, the Company channel opens already "replaced".
    public static bool CompanyReplaced = false;

    private class Channel
    {
        public string name;
        public List<string> members = new List<string>();
        public ChannelMode mode;
        public GameObject logObject;   // the scroll view for this channel (show/hide on tab switch)
        public TMP_Text log;
        public ScrollRect scroll;
        public float nextPost;
        public bool playerHasTyped;    // Calm mode: goes silent once the player speaks
        public GameObject tabButton;
    }

    [Header("Ambient timing")]
    public float minInterval = 1.2f;
    public float maxInterval = 3.0f;

    private readonly List<Channel> _channels = new List<Channel>();
    private Channel _active;
    private readonly System.Random _rng = new System.Random();

    private RectTransform _tabBar;
    private RectTransform _logHost;      // parent that holds all channels' log objects
    private TMP_Text _headerText;
    private TMP_InputField _input;

    public void Build(RectTransform content)
    {
        var root = NewRect(content, "ChatRoot");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.90f, 0.91f, 0.94f, 1f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f; vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        BuildTabBar(root);
        BuildHeader(root);
        BuildLogHost(root);   // flexible — holds each channel's log
        BuildInputRow(root);

        // --- define the two channels ---
        var company = new Channel {
            name = "Company",
            members = new List<string>(CoworkerNames.All),   // everyone but Steven
            mode = ChannelMode.Ambient
        };
        var team = new Channel {
            name = "My Team",
            members = new List<string> { "You" },            // sole survivor
            mode = ChannelMode.Silent
        };
        AddChannel(company);
        AddChannel(team);

        // Team channel: seed the ghost history so the emptiness is earned.
        SeedTeamGhosts(team);

        SwitchTo(company);

        Current = this;
        // If the world has already flipped (Day 3), open the Company channel already replaced.
        if (CompanyReplaced)
            ApplyReplaced();
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void Update()
    {
        foreach (var ch in _channels)
        {
            if (ch.mode == ChannelMode.Ambient && Time.time >= ch.nextPost)
            {
                PostRandom(ch, CompanyChatContent.Ambient);
                ch.nextPost = Time.time + Random.Range(minInterval, maxInterval);
            }
            else if (ch.mode == ChannelMode.Calm && !ch.playerHasTyped && Time.time >= ch.nextPost)
            {
                PostRandom(ch, CompanyChatContent.Calm);
                ch.nextPost = Time.time + Random.Range(minInterval + 1f, maxInterval + 2f);
            }
        }
    }

    // ---- channel management ----
    private void AddChannel(Channel ch)
    {
        // Log object (a scroll view), hidden until active.
        ch.logObject = BuildLog(_logHost, out ch.log, out ch.scroll);
        ch.logObject.SetActive(false);

        // Tab button
        var btnGo = new GameObject($"Tab_{ch.name}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.GetComponent<RectTransform>().SetParent(_tabBar, false);
        btnGo.GetComponent<Image>().color = new Color(0.75f, 0.76f, 0.8f);
        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredWidth = 110f; le.minWidth = 90f; le.preferredHeight = 28f;
        btnGo.GetComponent<Button>().onClick.AddListener(() => SwitchTo(ch));
        ch.tabButton = btnGo;

        var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.GetComponent<RectTransform>().SetParent(btnGo.transform, false);
        Stretch(lblGo.GetComponent<RectTransform>());
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = ch.name; lbl.fontSize = 14f; lbl.color = Color.black;
        lbl.alignment = TextAlignmentOptions.Center;

        ch.nextPost = Time.time + Random.Range(minInterval, maxInterval);
        _channels.Add(ch);
    }

    private void SwitchTo(Channel ch)
    {
        _active = ch;
        foreach (var c in _channels)
        {
            c.logObject.SetActive(c == ch);
            c.tabButton.GetComponent<Image>().color =
                (c == ch) ? Color.white : new Color(0.75f, 0.76f, 0.8f);
        }
        RefreshHeader();
    }

    private void RefreshHeader()
    {
        if (_active == null) return;
        int n = _active.members.Count;
        string who = (n == 1) ? "1 member" : $"{n} members";
        _headerText.text = $"# {_active.name}    \u2014    {who}";
    }

    private void SeedTeamGhosts(Channel team)
    {
        // People were here once. Now it's just you.
        string[] leavers = { "Marcus", "Priya", "Rhona", "Liam", "Nadia" };
        foreach (var name in leavers)
            AddLineTo(team, $"<i><color=#888888>{name} left the channel</color></i>");
        AddLineTo(team, "<i><color=#888888>You are the only member of this channel.</color></i>");
    }

    // ---- posting ----
    private void PostRandom(Channel ch, List<string> pool)
    {
        string name = ch.members.Count > 0
            ? ch.members[_rng.Next(ch.members.Count)]
            : CoworkerNames.Random();
        if (name == "You") name = CoworkerNames.Random();   // 'You' never auto-posts
        string msg = pool[_rng.Next(pool.Count)];
        AddLineTo(ch, $"<b>{name}:</b> {msg}");
    }

    private void OnPlayerSend()
    {
        if (_active == null) return;
        string text = _input.text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _input.text = "";

        string me = GameState.I != null ? GameState.I.playerName : "You";
        AddLineTo(_active, $"<b><color=#2a5a8a>{me}:</color></b> {text}");

        if (_active.mode == ChannelMode.Calm) _active.playerHasTyped = true;
        // In Company (Ambient) the churn just rolls on. In Team (Silent) nobody answers.
    }

    // Story hook for Act 3: flip the company channel to eerie calm.
    public static void SwitchCompanyToReplaced()
    {
        CompanyReplaced = true;
        if (Current != null) Current.ApplyReplaced();
    }

    private void ApplyReplaced()
    {
        var company = _channels.Find(c => c.name == "Company");
        if (company == null) return;
        company.mode = ChannelMode.Calm;
        company.playerHasTyped = false;
        company.members = new List<string> { "Lauren", "Stuart", "Alex", "You" };
        AddLineTo(company, "<i><color=#888888>\u2014 everyone else has left the channel \u2014</color></i>");
        AddLineTo(company, "<i><color=#888888>\u2014 Lauren, Stuart and Alex were added \u2014</color></i>");
        if (_active == company) RefreshHeader();
    }

    // Story hook: change a channel's member count later (people replaced/removed).
    public void SetMembers(string channelName, List<string> members)
    {
        var ch = _channels.Find(c => c.name == channelName);
        if (ch == null) return;
        ch.members = members;
        if (_active == ch) RefreshHeader();
    }

    // ---- UI build ----
    private void BuildTabBar(RectTransform parent)
    {
        var go = new GameObject("TabBar", typeof(RectTransform));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 30f; le.preferredHeight = 30f;
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3f; hlg.childControlWidth = false; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        _tabBar = go.GetComponent<RectTransform>();
    }

    private void BuildHeader(RectTransform parent)
    {
        var go = new GameObject("Header", typeof(RectTransform), typeof(Image));
        go.GetComponent<Image>().color = new Color(0.82f, 0.83f, 0.87f);
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 26f; le.preferredHeight = 26f;
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.SetParent(go.transform, false);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(10, 0); txtRt.offsetMax = new Vector2(-10, 0);
        _headerText = txtGo.GetComponent<TextMeshProUGUI>();
        _headerText.fontSize = 14f; _headerText.color = new Color(0.2f, 0.2f, 0.25f);
        _headerText.alignment = TextAlignmentOptions.Left;
        _headerText.fontStyle = FontStyles.Bold;
    }

    private void BuildLogHost(RectTransform parent)
    {
        var go = new GameObject("LogHost", typeof(RectTransform));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;
        _logHost = go.GetComponent<RectTransform>();
    }

    // Build a scroll-view log filling the host; returns the root object and out its text+scroll.
    private GameObject BuildLog(RectTransform host, out TMP_Text logText, out ScrollRect scroll)
    {
        var scrollGo = new GameObject("Log", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.GetComponent<Image>().color = Color.white;
        var srt = scrollGo.GetComponent<RectTransform>();
        srt.SetParent(host, false);
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var viewRt = viewportGo.GetComponent<RectTransform>();
        viewRt.SetParent(srt, false);
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
        cvlg.spacing = 3f; cvlg.padding = new RectOffset(10, 8, 6, 6);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = crt;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.GetComponent<RectTransform>().SetParent(crt, false);
        logText = textGo.GetComponent<TextMeshProUGUI>();
        logText.text = ""; logText.fontSize = 16f; logText.color = Color.black;
        logText.textWrappingMode = TextWrappingModes.Normal;
        logText.alignment = TextAlignmentOptions.TopLeft;

        return scrollGo;
    }

    private void BuildInputRow(RectTransform parent)
    {
        var rowGo = new GameObject("InputRow", typeof(RectTransform));
        rowGo.GetComponent<RectTransform>().SetParent(parent, false);
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.minHeight = 34f; rowLe.preferredHeight = 34f;
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        var inputGo = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGo.GetComponent<RectTransform>().SetParent(rowGo.transform, false);
        inputGo.GetComponent<Image>().color = Color.white;
        var inputLe = inputGo.AddComponent<LayoutElement>();
        inputLe.flexibleWidth = 1f;
        _input = inputGo.GetComponent<TMP_InputField>();

        var textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var textAreaRt = textAreaGo.GetComponent<RectTransform>();
        textAreaRt.SetParent(inputGo.transform, false);
        textAreaRt.anchorMin = Vector2.zero; textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(8, 2); textAreaRt.offsetMax = new Vector2(-8, -2);

        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGo.GetComponent<RectTransform>().SetParent(textAreaRt, false);
        Stretch(phGo.GetComponent<RectTransform>());
        var ph = phGo.GetComponent<TextMeshProUGUI>();
        ph.text = "Message..."; ph.fontSize = 15f; ph.color = new Color(0.5f, 0.5f, 0.5f);
        ph.alignment = TextAlignmentOptions.Left;

        var itGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        itGo.GetComponent<RectTransform>().SetParent(textAreaRt, false);
        Stretch(itGo.GetComponent<RectTransform>());
        var it = itGo.GetComponent<TextMeshProUGUI>();
        it.fontSize = 15f; it.color = Color.black; it.alignment = TextAlignmentOptions.Left;

        _input.textViewport = textAreaRt;
        _input.textComponent = it;
        _input.placeholder = ph;
        _input.onSubmit.AddListener(_ => OnPlayerSend());

        var btnGo = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.GetComponent<RectTransform>().SetParent(rowGo.transform, false);
        btnGo.GetComponent<Image>().color = new Color(0.25f, 0.4f, 0.6f);
        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.minWidth = 70f; btnLe.preferredWidth = 70f;
        btnGo.GetComponent<Button>().onClick.AddListener(OnPlayerSend);

        var blGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        blGo.GetComponent<RectTransform>().SetParent(btnGo.transform, false);
        Stretch(blGo.GetComponent<RectTransform>());
        var bl = blGo.GetComponent<TextMeshProUGUI>();
        bl.text = "Send"; bl.fontSize = 15f; bl.color = Color.white; bl.alignment = TextAlignmentOptions.Center;
    }

    private void AddLineTo(Channel ch, string richLine)
    {
        ch.log.text += (ch.log.text.Length > 0 ? "\n" : "") + richLine;
        if (ch == _active)
        {
            Canvas.ForceUpdateCanvases();
            if (ch.scroll != null) ch.scroll.verticalNormalizedPosition = 0f;
        }
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
}