using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Cyber-security minigame: "Defend the Core." A green core sits in the centre; malware threats
// spawn at the edges from ALL directions and drift inward toward the core, speeding up over time.
// CLICK a threat to destroy it. If one reaches the core it's a breach: the window shakes and the
// core flashes/takes damage. Score = % of threats stopped. Always completes when the wave resolves.
public class CyberShooterGame : MonoBehaviour
{
    private WorkTask _task;
    private WindowManager _manager;
    private DraggableWindow _window;

    private RectTransform _field;
    private RectTransform _core;
    private Image _coreImg;
    private TMP_Text _hud;

    private class Threat { public RectTransform rt; public Vector2 vel; }
    private readonly List<Threat> _threats = new List<Threat>();

    private float _fieldW, _fieldH;
    private float _coreRadius = 34f;
    private float _threatRadius = 13f;
    private float _baseSpeed = 42f;      // inward speed, grows over the wave
    private float _speedRamp = 3.0f;     // px/s added per second elapsed
    private float _elapsed;
    private float _spawnTimer, _spawnInterval = 0.75f;

    private int _totalThreats = 16;
    private int _spawned, _stopped, _breached;
    private int _coreHits;               // breaches the core has taken
    private bool _done;

    private Vector2 _windowHome;
    private float _shakeTime;
    private float _coreFlash;

    public static void Launch(WindowManager manager, WorkTask task)
    {
        var win = manager.OpenWindow("Cyber \u2014 Defend the Core", new Vector2(520f, 500f));
        var game = win.ContentArea.gameObject.AddComponent<CyberShooterGame>();
        game._manager = manager; game._task = task; game._window = win;
        game.Build(win.ContentArea);
    }

    private void Build(RectTransform content)
    {
        var root = NewRect(content, "ShooterRoot");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8); vlg.spacing = 6f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        _hud = MakeText(root, "Click the threats before they reach the core!", 14, 22, new Color(0.8f, 0.9f, 0.8f));

        // Playfield
        var fieldGo = new GameObject("Field", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        fieldGo.GetComponent<RectTransform>().SetParent(root, false);
        fieldGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.07f);
        var le = fieldGo.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f; le.minHeight = 360f;
        _field = fieldGo.GetComponent<RectTransform>();

        // Core (centre)
        var coreGo = new GameObject("Core", typeof(RectTransform), typeof(Image));
        coreGo.GetComponent<RectTransform>().SetParent(_field, false);
        _coreImg = coreGo.GetComponent<Image>();
        _coreImg.color = new Color(0.3f, 0.85f, 0.4f);
        _coreImg.sprite = CircleSprite();
        _core = coreGo.GetComponent<RectTransform>();
        _core.anchorMin = _core.anchorMax = new Vector2(0.5f, 0.5f);
        _core.pivot = new Vector2(0.5f, 0.5f);
        _core.sizeDelta = new Vector2(_coreRadius * 2f, _coreRadius * 2f);
        _core.anchoredPosition = Vector2.zero;
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        var rect = _field.rect;
        _fieldW = rect.width; _fieldH = rect.height;
        if (_window != null) _windowHome = _window.RectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (_fieldW <= 0f) { var r = _field.rect; _fieldW = r.width; _fieldH = r.height; return; }
        DoShake();
        DoCoreFlash();
        if (_done) return;

        _elapsed += Time.deltaTime;
        HandleSpawns();
        MoveThreats();
        HandleClicks();
        CheckEnd();
        UpdateHud();
    }

    private void HandleSpawns()
    {
        if (_spawned >= _totalThreats) return;
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = _spawnInterval;
            SpawnThreat();
        }
    }

    private void SpawnThreat()
    {
        var go = new GameObject("Threat", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(_field, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.9f, 0.3f, 0.3f);
        img.sprite = CircleSprite();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(_threatRadius * 2f, _threatRadius * 2f);

        // Spawn at a random angle on an ellipse just outside the field edge.
        float ang = Random.Range(0f, Mathf.PI * 2f);
        float rx = _fieldW * 0.5f - 10f, ry = _fieldH * 0.5f - 10f;
        Vector2 pos = new Vector2(Mathf.Cos(ang) * rx, Mathf.Sin(ang) * ry);
        rt.anchoredPosition = pos;

        var threat = new Threat { rt = rt, vel = Vector2.zero };
        // Click to destroy this threat.
        go.GetComponent<Button>().onClick.AddListener(() => DestroyThreat(threat));
        _threats.Add(threat);
        _spawned++;
    }

    private void MoveThreats()
    {
        float speed = _baseSpeed + _elapsed * _speedRamp;
        for (int i = _threats.Count - 1; i >= 0; i--)
        {
            var t = _threats[i];
            Vector2 p = t.rt.anchoredPosition;
            Vector2 toCore = (Vector2.zero - p);
            float dist = toCore.magnitude;
            if (dist <= _coreRadius + _threatRadius)
            {
                // reached the core -> breach
                _breached++;
                _coreHits++;
                Destroy(t.rt.gameObject);
                _threats.RemoveAt(i);
                TriggerShake();
                _coreFlash = 0.25f;
                continue;
            }
            Vector2 dir = toCore / Mathf.Max(dist, 0.001f);
            t.rt.anchoredPosition = p + dir * speed * Time.deltaTime;
        }
    }

    // Fallback click handling: also allow clicking near a threat (Button covers exact hits).
    private void HandleClicks() { /* Button per-threat handles clicks */ }

    private void DestroyThreat(Threat t)
    {
        if (_done || t == null || t.rt == null) return;
        if (!_threats.Contains(t)) return;
        _stopped++;
        _threats.Remove(t);
        Destroy(t.rt.gameObject);
    }

    private void TriggerShake() { _shakeTime = 0.3f; }

    private void DoShake()
    {
        if (_window == null) return;
        if (_shakeTime > 0f)
        {
            _shakeTime -= Time.deltaTime;
            Vector2 off = new Vector2(Random.Range(-7f, 7f), Random.Range(-7f, 7f));
            _window.RectTransform.anchoredPosition = _windowHome + off;
            if (_shakeTime <= 0f) _window.RectTransform.anchoredPosition = _windowHome;
        }
    }

    private void DoCoreFlash()
    {
        if (_coreImg == null) return;
        Color healthy = new Color(0.3f, 0.85f, 0.4f);
        Color hurt = new Color(0.9f, 0.7f, 0.2f);
        // Core tints toward red as it takes more hits; flashes white briefly on each breach.
        float dmg = Mathf.Clamp01(_coreHits / 8f);
        Color baseCol = Color.Lerp(healthy, new Color(0.85f, 0.25f, 0.25f), dmg);
        if (_coreFlash > 0f)
        {
            _coreFlash -= Time.deltaTime;
            _coreImg.color = Color.Lerp(baseCol, Color.white, Mathf.Clamp01(_coreFlash / 0.25f));
        }
        else _coreImg.color = baseCol;
    }

    private void CheckEnd()
    {
        if (_spawned >= _totalThreats && _threats.Count == 0)
            Finish();
    }

    private void UpdateHud()
    {
        if (_done) return;
        _hud.text = $"Stopped: {_stopped}   breached: {_breached}   incoming: {_totalThreats - _spawned + _threats.Count}";
    }

    private void Finish()
    {
        _done = true;
        int score = _totalThreats > 0 ? Mathf.RoundToInt(100f * _stopped / _totalThreats) : 100;
        _hud.text = $"Core secured. Stopped {_stopped}/{_totalThreats} \u2014 score {score}";
        Debug.Log($"[Cyber] stopped {_stopped}/{_totalThreats}, breached {_breached}, score {score}.");
        WorkDay.CompleteTask(_task, score);
        StartCoroutine(CloseAfter(1.4f));
    }

    private System.Collections.IEnumerator CloseAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (_manager != null && _window != null) _manager.CloseWindow(_window);
    }

    // ---- helpers ----
    // A runtime circle sprite so core/threats render round, not square.
    private static Sprite _circle;
    private static Sprite CircleSprite()
    {
        if (_circle != null) return _circle;
        int size = 64; var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f, cx = r, cy = r;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                bool inside = dx * dx + dy * dy <= (r - 1f) * (r - 1f);
                tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
            }
        tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _circle;
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
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, float h, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h;
        return t;
    }
}