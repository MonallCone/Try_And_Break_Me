using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Dark minigame 3/3: "Supplies." An approve/reject card game reskinned from the HR swipe \u2014 the
// player vets items to "help" with Steven (crowbar, rope, etc.). If they reject EVERYTHING, the
// bots refuse to let them finish ("that won't do \u2014 we need to use something") and loop them back.
// Lauren speaks. Completing (with >=1 approved) calls onComplete.
public class DarkSupplies : MonoBehaviour
{
    private System.Action _onComplete;
    private WindowManager _manager;
    private DraggableWindow _window;

    private static readonly (string name, string note)[] Items =
    {
        ("Crowbar", "For the door. Or whatever else."),
        ("Rope", "Enough of it."),
        ("Wrench", "For those stubborn problems"),
        ("Pertrol Can", "Things might get hot"),
        ("A Lighter", "The perfect partner in crime"),
        ("Pliers", "I hope the tooth fairy comes"),
        ("Car Battery", "Electric Idea"),
        ("Jumper Cables", "Always good when you need them"),
        ("Glock 17", "Funs over")
    };

    private int _index, _approved;
    private TMP_Text _progress, _name, _details, _hud;
    private bool _spoke;

    public static void Launch(WindowManager manager, System.Action onComplete)
    {
        var win = manager.OpenWindow("Supplies", new Vector2(420f, 460f));
        var game = win.ContentArea.gameObject.AddComponent<DarkSupplies>();
        game._manager = manager; game._window = win; game._onComplete = onComplete;
        game.Build(win.ContentArea);
    }

    private void Build(RectTransform content)
    {
        var root = NewRect(content, "Root");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12); vlg.spacing = 10f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        _progress = MakeText(root, "", 14, FontStyles.Bold, 22, new Color(0.8f, 0.8f, 0.85f));

        var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
        cardGo.GetComponent<RectTransform>().SetParent(root, false);
        cardGo.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);
        var cardLe = cardGo.AddComponent<LayoutElement>();
        cardLe.flexibleHeight = 1f; cardLe.minHeight = 200f;
        var cvlg = cardGo.AddComponent<VerticalLayoutGroup>();
        cvlg.padding = new RectOffset(16, 16, 16, 16); cvlg.spacing = 10f;
        cvlg.childControlWidth = true; cvlg.childControlHeight = true;
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
        cvlg.childAlignment = TextAnchor.UpperCenter;

        _name = MakeText(cardGo.GetComponent<RectTransform>(), "", 22, FontStyles.Bold, 34, new Color(0.95f, 0.9f, 0.9f));
        _details = MakeText(cardGo.GetComponent<RectTransform>(), "", 15, FontStyles.Normal, 80, new Color(0.8f, 0.8f, 0.82f));
        _details.textWrappingMode = TextWrappingModes.Normal;

        var rowGo = new GameObject("Buttons", typeof(RectTransform));
        rowGo.GetComponent<RectTransform>().SetParent(root, false);
        var rowLe = rowGo.AddComponent<LayoutElement>(); rowLe.minHeight = 48f; rowLe.preferredHeight = 48f;
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        MakeButton(rowGo.GetComponent<RectTransform>(), "Disapprove", new Color(0.5f, 0.25f, 0.25f), () => Decide(false));
        MakeButton(rowGo.GetComponent<RectTransform>(), "Approve", new Color(0.3f, 0.45f, 0.3f), () => Decide(true));

        _hud = MakeText(root, "", 13, FontStyles.Italic, 20, new Color(0.7f, 0.6f, 0.6f));

        if (!_spoke)
        {
            _spoke = true;
            var chat = ChatRegistry.FindByBotId("lauren");
            chat?.InjectBotLine("Pick what we'll need. Don't be squeamish. This is for all of us.", ominous: true);
        }

        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (_index >= Items.Length) { EndRound(); return; }
        var it = Items[_index];
        _progress.text = $"Item {_index + 1} of {Items.Length}";
        _name.text = it.name;
        _details.text = it.note;
    }

    private void Decide(bool approve)
    {
        if (approve) _approved++;
        _index++;
        ShowCurrent();
    }

    private void EndRound()
    {
        // Must approve at least one \u2014 the bots won't take no for an answer.
        if (_approved == 0)
        {
            var chat = ChatRegistry.FindByBotId("lauren");
            chat?.InjectBotLine("That won't do. We need to use something. Look again.", ominous: true);
            _index = 0;
            _hud.text = "\"We need to use something.\"";
            ShowCurrent();
            return;
        }

        var done = ChatRegistry.FindByBotId("lauren");
        done?.InjectBotLine("Good choices. We're ready now. Thank you for helping us.", ominous: true);
        StartCoroutine(CloseAfter(1.8f));
    }

    private System.Collections.IEnumerator CloseAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (_manager != null && _window != null) _manager.CloseWindow(_window);
        _onComplete?.Invoke();
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
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, FontStyles style, float h, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = TextAlignmentOptions.Center;
        var le = go.AddComponent<LayoutElement>(); le.minHeight = h; le.preferredHeight = h;
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
        lbl.text = label; lbl.fontSize = 16f; lbl.color = Color.white; lbl.alignment = TextAlignmentOptions.Center;
    }
}
