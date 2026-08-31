using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A how-to-play pop-up shown the first time each task TYPE is opened on Day 1. Explains the task,
// then a "Start" button launches the actual minigame. Purely onboarding \u2014 no scoring, no story.
public static class TaskTutorial
{
    // Returns the title + body text for a task type.
    private static (string title, string body) TextFor(TaskType type)
    {
        switch (type)
        {
            case TaskType.HRSwipe:
                return ("HR \u2014 Holiday Requests",
                    "Review each employee's holiday request.\n\n" +
                    "\u2022 Click APPROVE to grant the leave.\n" +
                    "\u2022 Click REJECT to deny it.\n\n" +
                    "Use your judgement \u2014 approving or rejecting absolutely everything tends to cause problems.");

            case TaskType.HelpDeskMaze:
                return ("Help Desk \u2014 Navigation",
                    "Guide the account (orange) through the maze to the reset point (green).\n\n" +
                    "\u2022 Move with the ARROW KEYS or WASD, or the on-screen arrows.\n" +
                    "\u2022 Walls block you; find the shortest route.\n\n" +
                    "The fewer wasted moves, the higher your score.");

            case TaskType.CyberShooter:
                return ("Cyber \u2014 Defend the Core",
                    "Protect the green core from incoming threats.\n\n" +
                    "\u2022 Threats close in from all directions and speed up over time.\n" +
                    "\u2022 CLICK a threat to destroy it before it reaches the core.\n\n" +
                    "Stop as many as you can \u2014 breaches damage the core.");

            default:
                return ("Task", "Complete the task to continue.");
        }
    }

    // Show the tutorial, then call onStart when the player clicks Start.
    public static void Show(WindowManager manager, TaskType type, System.Action onStart)
    {
        var (title, body) = TextFor(type);
        var win = manager.OpenWindow("How to play", new Vector2(420f, 340f));
        var root = win.ContentArea;
        root.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.98f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 16, 16); vlg.spacing = 12f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.GetComponent<RectTransform>().SetParent(root, false);
        var tt = titleGo.GetComponent<TextMeshProUGUI>();
        tt.text = title; tt.fontSize = 20f; tt.fontStyle = FontStyles.Bold; tt.color = Color.black;
        tt.alignment = TextAlignmentOptions.Center;
        titleGo.AddComponent<LayoutElement>().minHeight = 30f;

        var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGo.GetComponent<RectTransform>().SetParent(root, false);
        var bt = bodyGo.GetComponent<TextMeshProUGUI>();
        bt.text = body; bt.fontSize = 16f; bt.color = new Color(0.15f, 0.15f, 0.18f);
        bt.alignment = TextAlignmentOptions.TopLeft; bt.textWrappingMode = TextWrappingModes.Normal;
        var ble = bodyGo.AddComponent<LayoutElement>(); ble.flexibleHeight = 1f; ble.minHeight = 160f;

        var btnGo = new GameObject("Start", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.GetComponent<RectTransform>().SetParent(root, false);
        btnGo.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.3f);
        btnGo.AddComponent<LayoutElement>().minHeight = 44f;
        var blGo = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
        var blRt = blGo.GetComponent<RectTransform>();
        blRt.SetParent(btnGo.transform, false);
        blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one; blRt.offsetMin = Vector2.zero; blRt.offsetMax = Vector2.zero;
        var bl = blGo.GetComponent<TextMeshProUGUI>();
        bl.text = "Start task"; bl.fontSize = 17f; bl.color = Color.white; bl.alignment = TextAlignmentOptions.Center;

        btnGo.GetComponent<Button>().onClick.AddListener(() =>
        {
            manager.CloseWindow(win);
            onStart?.Invoke();
        });
    }
}