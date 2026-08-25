using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Dark minigame 2/3: "Kidnap Steven." Steven sits in the centre; blue-and-red dots (police) close
// in from all sides. Click them to keep them away until a timer runs out (you get him out). Reuses
// the radial "defend the core" mechanic, inverted: now you're the one holding him. Stuart speaks.
public class DarkKidnapSteven : MonoBehaviour
{
    private System.Action _onComplete;
    private WindowManager _manager;
    private DraggableWindow _window;

    private RectTransform _field;
    private RectTransform _core;   // Steven
    private Image _coreImg;
    private TMP_Text _hud;

    private class Threat { public RectTransform rt; }
    private readonly List<Threat> _threats = new List<Threat>();

    private float _fieldW, _fieldH;
    private float _coreRadius = 34f, _threatRadius = 13f;
    private float _baseSpeed = 26f, _speedRamp = 1.1f, _elapsed;
    private float _spawnTimer, _spawnInterval = 1.15f;
    private float _holdTime = 18f;   // survive this long to succeed
    private float _timeLeft;
    private int _grips = 5;          // cops reaching Steven cost a grip; not an instant fail
    private bool _done, _spoke;
    private Vector2 _windowHome; private float _shakeTime;

    public static void Launch(WindowManager manager, System.Action onComplete)
    {
        var win = manager.OpenWindow("Delete the annoyances", new Vector2(520f, 500f));
        var game = win.ContentArea.gameObject.AddComponent<DarkKidnapSteven>();
        game._manager = manager; game._window = win; game._onComplete = onComplete;
        game.Build(win.ContentArea);
    }

    private void Build(RectTransform content)
    {
        _timeLeft = _holdTime;
        var root = NewRect(content, "Root");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8); vlg.spacing = 6f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        _hud = MakeText(root, "Keep them back. Hold on until he's secured.", 14, 22, new Color(0.85f, 0.85f, 0.9f));

        var fieldGo = new GameObject("Field", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        fieldGo.GetComponent<RectTransform>().SetParent(root, false);
        fieldGo.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f);
        var le = fieldGo.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f; le.minHeight = 360f;
        _field = fieldGo.GetComponent<RectTransform>();

        var coreGo = new GameObject("Steven", typeof(RectTransform), typeof(Image));
        coreGo.GetComponent<RectTransform>().SetParent(_field, false);
        _coreImg = coreGo.GetComponent<Image>();
        _coreImg.color = new Color(0.9f, 0.85f, 0.5f);
        _coreImg.sprite = CircleSprite();
        _core = coreGo.GetComponent<RectTransform>();
        _core.anchorMin = _core.anchorMax = new Vector2(0.5f, 0.5f);
        _core.pivot = new Vector2(0.5f, 0.5f);
        _core.sizeDelta = new Vector2(_coreRadius * 2f, _coreRadius * 2f);
        _core.anchoredPosition = Vector2.zero;

        var lblGo = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.GetComponent<RectTransform>().SetParent(coreGo.transform, false);
        var lrt = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var l = lblGo.GetComponent<TextMeshProUGUI>();
        l.text = "S"; l.fontSize = 22f; l.color = Color.black; l.alignment = TextAlignmentOptions.Center;
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        var r = _field.rect; _fieldW = r.width; _fieldH = r.height;
        if (_window != null) _windowHome = _window.RectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (_fieldW <= 0f) { var r = _field.rect; _fieldW = r.width; _fieldH = r.height; return; }
        DoShake();
        if (_done) return;

        if (!_spoke)
        {
            _spoke = true;
            var chat = ChatRegistry.FindByBotId("stuart");
            chat?.InjectBotLine("The police are coming. Keep them off him. We only need a little longer.", ominous: true);
        }

        _elapsed += Time.deltaTime;
        _timeLeft -= Time.deltaTime;
        HandleSpawns();
        MoveThreats();
        if (_timeLeft <= 0f) Finish();
        UpdateHud();
    }

    private void HandleSpawns()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f) { _spawnTimer = _spawnInterval; SpawnThreat(); }
    }

    private void SpawnThreat()
    {
        var go = new GameObject("Cop", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(_field, false);
        var img = go.GetComponent<Image>();
        // blue-and-red: alternate tint so they read as police lights
        img.color = Random.value < 0.5f ? new Color(0.3f, 0.4f, 0.95f) : new Color(0.95f, 0.3f, 0.3f);
        img.sprite = CircleSprite();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(_threatRadius * 2f, _threatRadius * 2f);
        float ang = Random.Range(0f, Mathf.PI * 2f);
        float rx = _fieldW * 0.5f - 10f, ry = _fieldH * 0.5f - 10f;
        rt.anchoredPosition = new Vector2(Mathf.Cos(ang) * rx, Mathf.Sin(ang) * ry);
        var threat = new Threat { rt = rt };
        go.GetComponent<Button>().onClick.AddListener(() => { if (_threats.Contains(threat)) { _threats.Remove(threat); Destroy(go); } });
        _threats.Add(threat);
    }

    private void MoveThreats()
    {
        float speed = _baseSpeed + _elapsed * _speedRamp;
        for (int i = _threats.Count - 1; i >= 0; i--)
        {
            var t = _threats[i];
            Vector2 p = t.rt.anchoredPosition;
            float dist = p.magnitude;
            if (dist <= _coreRadius + _threatRadius)
            {
                // a cop reached Steven \u2014 costs a grip (not time). Out of grips is still not an
                // instant fail here; it just shakes hard. The hold timer always ticks down to a win.
                _grips = Mathf.Max(0, _grips - 1);
                Destroy(t.rt.gameObject); _threats.RemoveAt(i);
                TriggerShake();
                continue;
            }
            Vector2 dir = -p / Mathf.Max(dist, 0.001f);
            t.rt.anchoredPosition = p + dir * speed * Time.deltaTime;
        }
    }

    private void TriggerShake() { _shakeTime = 0.3f; }
    private void DoShake()
    {
        if (_window == null) return;
        if (_shakeTime > 0f)
        {
            _shakeTime -= Time.deltaTime;
            _window.RectTransform.anchoredPosition = _windowHome + new Vector2(Random.Range(-6f, 6f), Random.Range(-6f, 6f));
            if (_shakeTime <= 0f) _window.RectTransform.anchoredPosition = _windowHome;
        }
    }

    private void UpdateHud()
    {
        if (_done) return;
        _hud.text = $"Hold on... {Mathf.CeilToInt(_timeLeft)}s     grip: {_grips}";
    }

    private void Finish()
    {
        _done = true;
        _hud.text = "He's secured. Move.";
        var chat = ChatRegistry.FindByBotId("stuart");
        chat?.InjectBotLine("We have him. You did well. Just one more thing to sort out.", ominous: true);
        StartCoroutine(CloseAfter(1.8f));
    }

    private System.Collections.IEnumerator CloseAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (_manager != null && _window != null) _manager.CloseWindow(_window);
        _onComplete?.Invoke();
    }

    // ---- helpers ----
    private static Sprite _circle;
    private static Sprite CircleSprite()
    {
        if (_circle != null) return _circle;
        int size = 64; var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                tex.SetPixel(x, y, dx * dx + dy * dy <= (r - 1) * (r - 1) ? Color.white : new Color(1, 1, 1, 0));
            }
        tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _circle;
    }
    private static RectTransform NewRect(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); return rt;
    }
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, float h, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
        var le = go.AddComponent<LayoutElement>(); le.minHeight = h; le.preferredHeight = h;
        return t;
    }
}