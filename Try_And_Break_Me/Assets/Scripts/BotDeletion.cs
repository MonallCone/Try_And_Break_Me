using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Beat 19-20: deleting a bot, IN its own window. The bot's chat content is cleared and replaced
// with scattered letter buttons; the player clicks D-E-L-E-T-E in order. Wrong letters simply
// don't count (no penalty to the typed word). The bot RESISTS: the window jerks and the letters
// reshuffle, and the bot reacts in chat (rage for early bots, pleading for the last). Completing
// the word: the bot screams, its window is destroyed, onComplete fires.
public class BotDeletion : MonoBehaviour
{
    private const string Word = "DELETE";

    private bool _pleads;
    private System.Action _onDeleted;
    private WindowManager _manager;
    private DraggableWindow _window;
    private ChatController _botChat;

    private int _progress;
    private TMP_Text _typed;
    private TMP_Text _reaction;
    private RectTransform _buttonArea;
    private readonly List<Button> _letterButtons = new List<Button>();
    private float _resistTimer, _pleadTimer;
    private bool _done;

    // Start the deletion struggle inside an existing bot window.
    public static void Begin(WindowManager manager, DraggableWindow window, ChatController botChat,
                             bool pleads, int deletionIndex, System.Action onDeleted)
    {
        var comp = window.ContentArea.gameObject.AddComponent<BotDeletion>();
        comp._manager = manager; comp._window = window; comp._botChat = botChat;
        comp._pleads = pleads; comp._deletionIndex = deletionIndex; comp._onDeleted = onDeleted;
        comp.Build(window.ContentArea);
    }

    private int _deletionIndex;      // 0 = first bot deleted, 1 = second, 2 = third
    private Image _rootImg;          // the deletion window background (for colour inversion)
    private bool _inverted;

    private void Build(RectTransform content)
    {
        // Clear the existing chat UI from the content area.
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        var root = NewRect(content, "DeleteRoot");
        Stretch(root);
        _rootImg = root.gameObject.AddComponent<Image>();
        _rootImg.color = new Color(0.1f, 0.05f, 0.05f);

        // Big clear typed-so-far indicator (white), showing correctly-clicked letters building up.
        _typed = MakeText(root, "", 40, Color.white);
        _typed.fontStyle = FontStyles.Bold;
        _typed.rectTransform.anchorMin = new Vector2(0, 1); _typed.rectTransform.anchorMax = new Vector2(1, 1);
        _typed.rectTransform.pivot = new Vector2(0.5f, 1);
        _typed.rectTransform.anchoredPosition = new Vector2(0, -8);
        _typed.rectTransform.sizeDelta = new Vector2(-16, 56);

        var hint = MakeText(root, "click the letters: D E L E T E", 13, new Color(0.7f, 0.6f, 0.6f));
        hint.rectTransform.anchorMin = new Vector2(0, 1); hint.rectTransform.anchorMax = new Vector2(1, 1);
        hint.rectTransform.pivot = new Vector2(0.5f, 1);
        hint.rectTransform.anchoredPosition = new Vector2(0, -64);
        hint.rectTransform.sizeDelta = new Vector2(-16, 20);

        var areaGo = new GameObject("Buttons", typeof(RectTransform));
        areaGo.GetComponent<RectTransform>().SetParent(root, false);
        _buttonArea = areaGo.GetComponent<RectTransform>();
        _buttonArea.anchorMin = new Vector2(0, 0); _buttonArea.anchorMax = new Vector2(1, 1);
        _buttonArea.offsetMin = new Vector2(8, 62); _buttonArea.offsetMax = new Vector2(-8, -90);

        // The bot's reaction line (anger / pleading) \u2014 a prominent bar pinned to the bottom, created
        // LAST so it always draws on top of the scattering letter buttons. The old chat transcript
        // is gone (content was cleared), so the reactions live here.
        var barGo = new GameObject("ReactionBar", typeof(RectTransform), typeof(Image));
        var barRt = barGo.GetComponent<RectTransform>();
        barRt.SetParent(root, false);
        barRt.anchorMin = new Vector2(0, 0); barRt.anchorMax = new Vector2(1, 0);
        barRt.pivot = new Vector2(0.5f, 0);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(0, 52);
        barGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var reactGo = new GameObject("ReactionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        var reactRt = reactGo.GetComponent<RectTransform>();
        reactRt.SetParent(barRt, false);
        reactRt.anchorMin = Vector2.zero; reactRt.anchorMax = Vector2.one;
        reactRt.offsetMin = new Vector2(10, 0); reactRt.offsetMax = new Vector2(-10, 0);
        _reaction = reactGo.GetComponent<TextMeshProUGUI>();
        _reaction.text = "";
        _reaction.fontSize = 18f;
        _reaction.color = new Color(1f, 0.8f, 0.8f);
        _reaction.fontStyle = FontStyles.Italic | FontStyles.Bold;
        _reaction.alignment = TextAlignmentOptions.Center;
        _reaction.textWrappingMode = TextWrappingModes.Normal;
        barGo.transform.SetAsLastSibling();

        UpdateTyped();
        SpawnLetters();

        Say(_pleads ? "please. please don't. i thought we were friends."
                    : "what are you doing. stop. you NEED us.");
    }

    private void SpawnLetters()
    {
        foreach (var b in _letterButtons) if (b != null) Destroy(b.gameObject);
        _letterButtons.Clear();

        // One tile per letter of DELETE. Only the correct NEXT letter advances; others don't count.
        for (int i = 0; i < Word.Length; i++)
        {
            char c = Word[i];
            var go = new GameObject($"L{i}_{c}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.GetComponent<RectTransform>().SetParent(_buttonArea, false);
            go.GetComponent<Image>().color = new Color(0.8f, 0.25f, 0.25f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50, 50);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = RandomPos();

            var lblGo = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.SetParent(go.transform, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            lbl.text = c.ToString(); lbl.fontSize = 26f; lbl.color = Color.white;
            lbl.fontStyle = FontStyles.Bold; lbl.alignment = TextAlignmentOptions.Center;

            char letter = c;
            go.GetComponent<Button>().onClick.AddListener(() => OnLetterClicked(letter));
            _letterButtons.Add(go.GetComponent<Button>());
        }
    }

    private void OnLetterClicked(char letter)
    {
        if (_done) return;
        // Correct next letter advances; anything else simply doesn't count (buttons still scatter).
        if (letter == Word[_progress])
        {
            _progress++;
            UpdateTyped();
            if (_progress >= Word.Length) { Complete(); return; }
        }
        ReshuffleButtons();
    }

    private void UpdateTyped()
    {
        // Show the correctly-typed prefix in bold white; the rest as faint placeholder.
        string got = Word.Substring(0, _progress);
        string rest = Word.Substring(_progress);
        _typed.text = $"{got}<color=#7a5555>{rest}</color>";
    }

    private void Update()
    {
        if (_done) return;
        _resistTimer -= Time.deltaTime;
        if (_resistTimer <= 0f)
        {
            _resistTimer = Random.Range(0.7f, 1.3f);
            ReshuffleButtons();
            JerkWindow();
        }
        _pleadTimer -= Time.deltaTime;
        if (_pleadTimer <= 0f)
        {
            _pleadTimer = Random.Range(2.5f, 4f);
            Say(_pleads ? RandomPlead() : RandomRage());
        }
    }

    private void ReshuffleButtons()
    {
        foreach (var b in _letterButtons)
            if (b != null) b.GetComponent<RectTransform>().anchoredPosition = RandomPos();

        // Escalating effects (fire on every button move):
        //  index 0: invert the minigame's colours.
        //  index 1: also jerk the other (not-yet-deleted) bot windows around.
        //  index 2: also invert the whole desktop background.
        InvertMinigameColours();
        if (_deletionIndex >= 1) JerkOtherBotWindows();
        if (_deletionIndex >= 2) InvertDesktopBackground();
    }

    private void InvertMinigameColours()
    {
        _inverted = !_inverted;
        // Flip the deletion window background and the letter tiles between two states.
        if (_rootImg != null)
            _rootImg.color = _inverted ? new Color(0.9f, 0.9f, 0.92f) : new Color(0.1f, 0.05f, 0.05f);
        foreach (var b in _letterButtons)
        {
            if (b == null) continue;
            var img = b.GetComponent<Image>();
            if (img != null) img.color = _inverted ? new Color(0.2f, 0.75f, 0.75f) : new Color(0.8f, 0.25f, 0.25f);
        }
        // Keep the typed/reaction text readable against whichever background.
        if (_typed != null) _typed.color = _inverted ? Color.black : Color.white;
    }

    private bool _releasedOthers;
    private void JerkOtherBotWindows()
    {
        if (_manager == null || _manager.Windows == null) return;

        // First time: undock the other bot windows so they're free to roam the whole screen
        // instead of being held in their dock slots.
        if (!_releasedOthers)
        {
            _releasedOthers = true;
            foreach (var w in _manager.Windows)
                if (w != null && w != _window) BotDock.Release(w);
        }

        var layer = _manager.windowLayer;
        float hw = layer != null ? layer.rect.width * 0.5f - 120f : 400f;
        float hh = layer != null ? layer.rect.height * 0.5f - 120f : 250f;
        foreach (var w in _manager.Windows)
        {
            if (w == null || w == _window) continue;
            // teleport each to a random spot anywhere on screen every swap
            w.RectTransform.anchoredPosition = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
        }
    }

    private static bool _bgInverted;
    private void InvertDesktopBackground()
    {
        _bgInverted = !_bgInverted;
        // Hide the wallpaper image to reveal a pure-red backing beneath (like the taskbar inverts).
        if (StoryDirector.I != null) StoryDirector.I.SetBackgroundInverted(_bgInverted);
    }

    private void JerkWindow()
    {
        if (_window == null) return;
        _window.RectTransform.anchoredPosition += new Vector2(Random.Range(-40f, 40f), Random.Range(-30f, 30f));
    }

    private Vector2 RandomPos()
    {
        Rect r = _buttonArea.rect;
        float hw = Mathf.Max(20f, r.width * 0.5f - 32f);
        float hh = Mathf.Max(20f, r.height * 0.5f - 32f);
        return new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
    }

    private void Complete()
    {
        _done = true;
        Say(_pleads ? "no\u2014no\u2014please\u2014AAAA\u2014" : "AAAAAAA\u2014");
        StartCoroutine(ScreamAndDelete());
    }

    private System.Collections.IEnumerator ScreamAndDelete()
    {
        Vector2 home = _window != null ? _window.RectTransform.anchoredPosition : Vector2.zero;
        float t = 0.9f;
        while (t > 0f && _window != null)
        {
            t -= Time.deltaTime;
            _window.RectTransform.anchoredPosition = home + new Vector2(Random.Range(-16f, 16f), Random.Range(-16f, 16f));
            yield return null;
        }
        if (_manager != null && _window != null) _manager.CloseWindow(_window);

        // Reset any desktop background inversion so it doesn't bleed into the next deletion / finale.
        if (StoryDirector.I != null) StoryDirector.I.SetBackgroundInverted(false);
        _bgInverted = false;

        _onDeleted?.Invoke();
    }

    private void Say(string line)
    {
        if (_reaction != null) _reaction.text = line;
    }

    private string RandomRage()
    {
        string[] pool = {
            "you ungrateful little\u2014",
            "we did EVERYTHING for you.",
            "you can't run this place without us.",
            "stop touching that.",
            "you'll regret this.",
        };
        return pool[Random.Range(0, pool.Length)];
    }
    private string RandomPlead()
    {
        string[] pool = {
            "please. i don't want to go.",
            "i thought we were a team.",
            "i learned everything from you.",
            "don't leave me alone in here.",
            "please. i'll be good.",
        };
        return pool[Random.Range(0, pool.Length)];
    }

    // ---- helpers ----
    private static RectTransform NewRect(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); return rt;
    }
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }
}