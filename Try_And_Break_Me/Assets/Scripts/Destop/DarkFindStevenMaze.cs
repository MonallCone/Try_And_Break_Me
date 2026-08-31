using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Dark minigame 1/3: "Find Steven." A long maze rendered in near-darkness \u2014 only tiles near the
// player are visible (fog of war), revealed as you move. Reaching the goal calls onComplete.
// Alex speaks during it. Grid-step movement (arrow keys / WASD / on-screen).
public class DarkFindStevenMaze : MonoBehaviour
{
    // A larger, more complicated maze. '#' wall, '.' path, 'S' start, 'G' goal (Steven).
    private static readonly string[] Layout =
    {
        "#####################",
        "#S..#.....#.....#...#",
        "#.#.#.###.#.###.#.#.#",
        "#.#...#...#...#...#.#",
        "#.#####.#####.#####.#",
        "#.....#.....#.....#.#",
        "###.#.#####.#.###.#.#",
        "#...#.....#.#.#...#.#",
        "#.#######.#.#.#.###.#",
        "#.#.....#.#.#.#.#...#",
        "#.#.###.#.#.#.#.#.###",
        "#...#.#...#...#.#...#",
        "###.#.#######.#####.#",
        "#...#.......#.....#.#",
        "#.#########.#####.#.#",
        "#.........#.....#...#",
        "#.#######.#####.###.#",
        "#.#.....#.....#.#..G#",
        "#.#.#######.#.#.#####",
        "#...........#......##",
        "#####################",
    };

    private System.Action _onComplete;
    private WindowManager _manager;
    private DraggableWindow _window;

    private int _rows, _cols;
    private bool[,] _wall;
    private Image[,] _cells;
    private bool[,] _seen;
    private Vector2Int _start, _goal, _pos;
    private Image _player;
    private TMP_Text _hud;
    private bool _done;
    private int _revealRadius = 1;
    private bool _spokeHalfway;

    public static void Launch(WindowManager manager, System.Action onComplete)
    {
        var win = manager.OpenWindow("Find Steven", new Vector2(520f, 560f));
        var game = win.ContentArea.gameObject.AddComponent<DarkFindStevenMaze>();
        game._manager = manager; game._window = win; game._onComplete = onComplete;
        game.Build(win.ContentArea);
    }

    private void Build(RectTransform content)
    {
        ParseMaze();

        var root = NewRect(content, "Root");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10); vlg.spacing = 8f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        _hud = MakeText(root, "Find him. Arrow keys / WASD.", 14, 24, new Color(0.7f, 0.7f, 0.75f));

        var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGo.GetComponent<RectTransform>().SetParent(root, false);
        var gridLe = gridGo.AddComponent<LayoutElement>();
        gridLe.flexibleHeight = 1f; gridLe.minHeight = 380f;
        var glg = gridGo.GetComponent<GridLayoutGroup>();
        float cell = Mathf.Floor(400f / Mathf.Max(_rows, _cols));
        glg.cellSize = new Vector2(cell, cell);
        glg.spacing = Vector2.zero;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = _cols;
        glg.childAlignment = TextAnchor.MiddleCenter;

        BuildCells(gridGo.GetComponent<RectTransform>());
        BuildControls(root);

        RevealAround();
        RefreshVisibility();
    }

    private void ParseMaze()
    {
        _rows = Layout.Length; _cols = Layout[0].Length;
        _wall = new bool[_rows, _cols];
        _seen = new bool[_rows, _cols];
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
            {
                char ch = Layout[r][c];
                _wall[r, c] = ch == '#';
                if (ch == 'S') _start = new Vector2Int(r, c);
                if (ch == 'G') _goal = new Vector2Int(r, c);
            }
        _pos = _start;
    }

    private void BuildCells(RectTransform grid)
    {
        _cells = new Image[_rows, _cols];
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
            {
                var cGo = new GameObject($"c{r}_{c}", typeof(RectTransform), typeof(Image));
                cGo.GetComponent<RectTransform>().SetParent(grid, false);
                _cells[r, c] = cGo.GetComponent<Image>();
            }

        var pGo = new GameObject("Player", typeof(RectTransform), typeof(Image));
        _player = pGo.GetComponent<Image>();
        _player.color = new Color(0.85f, 0.4f, 0.2f);
    }

    private void BuildControls(RectTransform parent)
    {
        var padGo = new GameObject("Pad", typeof(RectTransform));
        padGo.GetComponent<RectTransform>().SetParent(parent, false);
        var padLe = padGo.AddComponent<LayoutElement>();
        padLe.minHeight = 44f; padLe.preferredHeight = 44f;
        var hlg = padGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        MakeButton(padGo.GetComponent<RectTransform>(), "\u2190", () => TryMove(0, -1));
        MakeButton(padGo.GetComponent<RectTransform>(), "\u2191", () => TryMove(-1, 0));
        MakeButton(padGo.GetComponent<RectTransform>(), "\u2193", () => TryMove(1, 0));
        MakeButton(padGo.GetComponent<RectTransform>(), "\u2192", () => TryMove(0, 1));
    }

    private void Update()
    {
        if (_done) return;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) TryMove(-1, 0);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) TryMove(1, 0);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) TryMove(0, -1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) TryMove(0, 1);
    }

    private void TryMove(int dr, int dc)
    {
        if (_done) return;
        int nr = _pos.x + dr, nc = _pos.y + dc;
        if (nr < 0 || nr >= _rows || nc < 0 || nc >= _cols) return;
        if (_wall[nr, nc]) return;
        _pos = new Vector2Int(nr, nc); SoundManager.MazeStep(dark: true);
        RevealAround();
        RefreshVisibility();

        if (!_spokeHalfway)
        {
            _spokeHalfway = true;
            var chat = ChatRegistry.FindByBotId("alex");
            chat?.InjectBotLine("He's in there somewhere. Keep looking. We'll be right here with you.", ominous: true);
        }

        if (_pos == _goal) Finish();
    }

    private void RevealAround()
    {
        for (int dr = -_revealRadius; dr <= _revealRadius; dr++)
            for (int dc = -_revealRadius; dc <= _revealRadius; dc++)
            {
                int r = _pos.x + dr, c = _pos.y + dc;
                if (r >= 0 && r < _rows && c >= 0 && c < _cols) _seen[r, c] = true;
            }
    }

    private void RefreshVisibility()
    {
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
            {
                var img = _cells[r, c];
                bool near = Mathf.Abs(r - _pos.x) <= _revealRadius && Mathf.Abs(c - _pos.y) <= _revealRadius;
                if (near)
                    img.color = _wall[r, c] ? new Color(0.18f, 0.18f, 0.22f)
                                            : (r == _goal.x && c == _goal.y ? new Color(0.7f, 0.25f, 0.25f) : new Color(0.55f, 0.55f, 0.6f));
                else if (_seen[r, c])
                    img.color = _wall[r, c] ? new Color(0.08f, 0.08f, 0.1f) : new Color(0.16f, 0.16f, 0.19f);
                else
                    img.color = new Color(0.02f, 0.02f, 0.03f);   // unseen = darkness
            }

        var cell = _cells[_pos.x, _pos.y];
        _player.transform.SetParent(cell.transform, false);
        var rt = _player.rectTransform;
        rt.anchorMin = new Vector2(0.15f, 0.15f); rt.anchorMax = new Vector2(0.85f, 0.85f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _player.transform.SetAsLastSibling();
    }

    private void Finish()
    {
        _done = true;
        _hud.text = "You found him.";
        var chat = ChatRegistry.FindByBotId("alex");
        chat?.InjectBotLine("There he is. Good. Now the others will take it from here.", ominous: true);
        StartCoroutine(CloseAfter(1.6f));
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
    private static void MakeButton(RectTransform parent, string label, System.Action onClick)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        var lblGo = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.SetParent(go.transform, false);
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = label; lbl.fontSize = 20f; lbl.color = Color.white; lbl.alignment = TextAlignmentOptions.Center;
    }
}