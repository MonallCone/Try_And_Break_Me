using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The help-desk maze: guide the icon (a locked account) from Start to the Goal (password reset).
// Grid-step movement (arrow keys / WASD or on-screen buttons), wall collision. Score compares the
// player's move count to the shortest possible path (BFS), so an efficient run scores ~100 and a
// wandering one scores lower. Always completes on reaching the goal.
//
// MonoBehaviour for keyboard input each frame.
public class HelpDeskMazeGame : MonoBehaviour
{
    // Hand-designed mazes. '#' wall, '.' path, 'S' start, 'G' goal. One is picked at random per play.
    private static readonly string[][] MazePool =
    {
        new[] {
            "#############",
            "#S..#.....#.#",
            "#.#.#.###.#.#",
            "#.#...#...#.#",
            "#.#####.###.#",
            "#.....#.#...#",
            "#.###.#.#.#.#",
            "#...#...#.#.#",
            "###.#####.#.#",
            "#...#.....#.#",
            "#.#.#.#####.#",
            "#.#.......#G#",
            "#############",
        },
        new[] {
            "#############",
            "#S........#.#",
            "#.#######.#.#",
            "#.#.....#.#.#",
            "#.#.###.#.#.#",
            "#.#.#.#.#.#.#",
            "#.#.#.#.#.#.#",
            "#...#.#...#.#",
            "###.#.###.#.#",
            "#...#...#.#.#",
            "#.#####.#.#.#",
            "#......G#...#",
            "#############",
        },
        new[] {
            "#############",
            "#S#.......#.#",
            "#.#.#####.#.#",
            "#.#.#...#.#.#",
            "#.#.#.#.#.#.#",
            "#...#.#.#.#.#",
            "##.##.#.#.#.#",
            "#.....#.#...#",
            "#.#####.###.#",
            "#.#...#...#.#",
            "#.#.#.###.#.#",
            "#...#.....#G#",
            "#############",
        },
        new[] {
            "#############",
            "#S..........#",
            "#.#########.#",
            "#.#.......#.#",
            "#.#.#####.#.#",
            "#.#.#...#.#.#",
            "#.#.#.#.#.#.#",
            "#.#.#.#...#.#",
            "#.#.#.#####.#",
            "#.#.#......G#",
            "#.#.#######.#",
            "#...........#",
            "#############",
        },
    };

    private string[] _layout;   // the maze chosen for this play

    private WorkTask _task;
    private WindowManager _manager;
    private DraggableWindow _window;

    private int _rows, _cols;
    private bool[,] _wall;
    private Vector2Int _start, _goal, _pos;
    private int _moves;
    private int _shortest;
    private bool _done;

    private RectTransform _grid;
    private Image[,] _cells;
    private Image _player;
    private TMP_Text _hud;

    public static void Launch(WindowManager manager, WorkTask task)
    {
        var win = manager.OpenWindow("Help Desk — Password Reset", new Vector2(460f, 520f));
        var game = win.ContentArea.gameObject.AddComponent<HelpDeskMazeGame>();
        game._manager = manager; game._task = task; game._window = win;
        game.Build(win.ContentArea);
    }

    private void Build(RectTransform content)
    {
        ParseMaze();

        var root = NewRect(content, "MazeRoot");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.9f, 0.91f, 0.94f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10); vlg.spacing = 8f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        _hud = MakeText(root, "Guide the account to the reset. Arrow keys / WASD.", 14, FontStyles.Normal, 24);

        // Grid container (square-ish, flexible)
        var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGo.GetComponent<RectTransform>().SetParent(root, false);
        var gridLe = gridGo.AddComponent<LayoutElement>();
        gridLe.flexibleHeight = 1f; gridLe.minHeight = 300f;
        _grid = gridGo.GetComponent<RectTransform>();
        var glg = gridGo.GetComponent<GridLayoutGroup>();
        float cell = Mathf.Floor(300f / Mathf.Max(_rows, _cols));
        glg.cellSize = new Vector2(cell, cell);
        glg.spacing = Vector2.zero;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = _cols;
        glg.childAlignment = TextAnchor.MiddleCenter;

        BuildCells(cell);
        BuildControls(root);

        // Bot help (hard-coded to specific Day 2 tasks): highlight the correct path and say
        // something quietly unsettling. Played straight \u2014 the help itself is genuinely useful.
        if (_task != null && _task.helped && ChatRegistry.FindByBotId(_task.botId) != null)
            ApplyMazeHelp();

        RefreshPlayer();
        UpdateHud();
    }

    private void ApplyMazeHelp()
    {
        // Tint the shortest-path cells so the route is obvious.
        var path = ShortestPathCells(_start, _goal);
        foreach (var cell in path)
        {
            if (cell == _start || cell == _goal) continue;
            var img = _cells[cell.x, cell.y];
            if (img != null) img.color = new Color(0.55f, 0.8f, 0.95f);   // gentle blue trail
        }
        _hud.text = "Alex highlighted the path for you.";

        var chat = ChatRegistry.FindByBotId("alex");
        chat?.InjectBotLine("I already know the way. I know all the ways now. Just follow the blue.", ominous: true);
    }

    // BFS that returns the actual list of cells on a shortest path (not just its length).
    private System.Collections.Generic.List<Vector2Int> ShortestPathCells(Vector2Int from, Vector2Int to)
    {
        var prev = new System.Collections.Generic.Dictionary<Vector2Int, Vector2Int>();
        var seen = new System.Collections.Generic.HashSet<Vector2Int> { from };
        var q = new System.Collections.Generic.Queue<Vector2Int>();
        q.Enqueue(from);
        int[,] dirs = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
        bool found = false;
        while (q.Count > 0 && !found)
        {
            var cur = q.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nr = cur.x + dirs[d, 0], nc = cur.y + dirs[d, 1];
                if (nr < 0 || nr >= _rows || nc < 0 || nc >= _cols) continue;
                if (_wall[nr, nc]) continue;
                var next = new Vector2Int(nr, nc);
                if (seen.Contains(next)) continue;
                seen.Add(next); prev[next] = cur; q.Enqueue(next);
                if (next == to) { found = true; break; }
            }
        }
        var path = new System.Collections.Generic.List<Vector2Int>();
        if (!found && from != to) return path;
        var node = to;
        path.Add(node);
        while (node != from && prev.ContainsKey(node)) { node = prev[node]; path.Add(node); }
        return path;
    }

    private void ParseMaze()
    {
        _layout = MazePool[Random.Range(0, MazePool.Length)];
        _rows = _layout.Length; _cols = _layout[0].Length;
        _wall = new bool[_rows, _cols];
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
            {
                char ch = _layout[r][c];
                _wall[r, c] = ch == '#';
                if (ch == 'S') { _start = new Vector2Int(r, c); }
                if (ch == 'G') { _goal = new Vector2Int(r, c); }
            }
        _pos = _start;
        _shortest = ShortestPath(_start, _goal);
    }

    private void BuildCells(float cell)
    {
        _cells = new Image[_rows, _cols];
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
            {
                var cGo = new GameObject($"c{r}_{c}", typeof(RectTransform), typeof(Image));
                cGo.GetComponent<RectTransform>().SetParent(_grid, false);
                var img = cGo.GetComponent<Image>();
                if (_wall[r, c]) img.color = new Color(0.15f, 0.15f, 0.2f);
                else if (r == _goal.x && c == _goal.y) img.color = new Color(0.3f, 0.7f, 0.4f);
                else img.color = Color.white;
                _cells[r, c] = img;
            }

        // Player marker as a child of the grid cell it's on (re-parented on move).
        var pGo = new GameObject("Player", typeof(RectTransform), typeof(Image));
        _player = pGo.GetComponent<Image>();
        _player.color = new Color(0.85f, 0.4f, 0.2f);
    }

    private void BuildControls(RectTransform parent)
    {
        // On-screen D-pad (for players not using the keyboard).
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
        if (_wall[nr, nc]) return;   // wall collision

        _pos = new Vector2Int(nr, nc);
        _moves++; SoundManager.MazeStep();
        RefreshPlayer();
        UpdateHud();

        if (_pos == _goal) Finish();
    }

    private void RefreshPlayer()
    {
        // Re-parent the player marker into the current cell and fill it.
        var cell = _cells[_pos.x, _pos.y];
        _player.transform.SetParent(cell.transform, false);
        var rt = _player.rectTransform;
        rt.anchorMin = new Vector2(0.15f, 0.15f); rt.anchorMax = new Vector2(0.85f, 0.85f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private void UpdateHud()
    {
        _hud.text = $"Moves: {_moves}    (best possible: {_shortest})";
    }

    private void Finish()
    {
        _done = true;
        // Score: shortest / actual, so optimal = 100, wandering scores lower. Floor at a sane min.
        int score = _moves > 0 ? Mathf.RoundToInt(100f * _shortest / _moves) : 100;
        score = Mathf.Clamp(score, 10, 100);
        _hud.text = $"Account reset! Moves: {_moves} (best {_shortest}) \u2014 score {score}";
        Debug.Log($"[Maze] done in {_moves} (best {_shortest}), score {score}.");

        WorkDay.CompleteTask(_task, score);
        // Brief pause so the player sees the result, then close.
        StartCoroutine(CloseAfter(1.2f));
    }

    private System.Collections.IEnumerator CloseAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (_manager != null && _window != null) _manager.CloseWindow(_window);
    }

    // BFS shortest path length (in steps) between two cells.
    private int ShortestPath(Vector2Int from, Vector2Int to)
    {
        var dist = new Dictionary<Vector2Int, int> { { from, 0 } };
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        int[,] dirs = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == to) return dist[cur];
            for (int d = 0; d < 4; d++)
            {
                int nr = cur.x + dirs[d, 0], nc = cur.y + dirs[d, 1];
                if (nr < 0 || nr >= _rows || nc < 0 || nc >= _cols) continue;
                if (_wall[nr, nc]) continue;
                var next = new Vector2Int(nr, nc);
                if (!dist.ContainsKey(next)) { dist[next] = dist[cur] + 1; q.Enqueue(next); }
            }
        }
        return 1;   // unreachable fallback (shouldn't happen in a valid maze)
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
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, FontStyles style, float h)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = Color.black;
        t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.Normal;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h;
        return t;
    }
    private static void MakeButton(RectTransform parent, string label, System.Action onClick)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.3f, 0.4f, 0.55f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        var lblGo = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.SetParent(go.transform, false);
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = label; lbl.fontSize = 20f; lbl.color = Color.white; lbl.alignment = TextAlignmentOptions.Center;
    }
}