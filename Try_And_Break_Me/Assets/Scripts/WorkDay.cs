using System;
using System.Collections.Generic;
using UnityEngine;

// One work ticket. Its Type decides which minigame launches; Status tracks To Do vs Completed;
// Score (0-100) is set when the minigame finishes and drives the bot's comment.
public enum TaskType { Placeholder, HRSwipe, CyberShooter, HelpDeskMaze }
public enum TaskStatus { ToDo, Completed }

[Serializable]
public class WorkTask
{
    public string id;
    public string title;
    public TaskType type;
    public string botId;              // which bot comments on this task (lauren/stuart/alex)
    public TaskStatus status = TaskStatus.ToDo;
    public int score = -1;            // -1 until completed
    public bool helped = false;       // if true, the matching bot assists during the minigame

    public WorkTask(string id, string title, TaskType type, string botId)
    {
        this.id = id; this.title = title; this.type = type; this.botId = botId;
    }
}

// The current work day: its tickets and quota. The Tasks app reads this; the story builds each
// day's list. Static singleton for simplicity in a single-scene game.
public static class WorkDay
{
    public static readonly List<WorkTask> Tasks = new List<WorkTask>();
    public static int Day = 1;

    // Fired whenever a task changes (completed) so the Tasks app can refresh.
    public static event Action Changed;

    // Lets the story mark tasks done directly (e.g. the Day 3 blitz) and refresh the Tasks UI.
    public static void RaiseChanged() { Changed?.Invoke(); }

    public static int Quota => Tasks.Count;
    public static int CompletedCount
    {
        get { int n = 0; foreach (var t in Tasks) if (t.status == TaskStatus.Completed) n++; return n; }
    }
    public static bool AllComplete => Tasks.Count > 0 && CompletedCount >= Tasks.Count;

    public static void StartDay(int day, List<WorkTask> tasks)
    {
        Day = day;
        Tasks.Clear();
        Tasks.AddRange(tasks);
        Changed?.Invoke();
    }

    // Called when a minigame finishes. Records score, marks complete, fires the bot comment.
    public static void CompleteTask(WorkTask task, int score)
    {
        if (task == null || task.status == TaskStatus.Completed) return;
        task.score = Mathf.Clamp(score, 0, 100);
        task.status = TaskStatus.Completed;
        Changed?.Invoke();
        SoundManager.TaskComplete();

        // The matching bot comments in its chat window.
        BotComments.Deliver(task);

        // Let the story react (end-of-day triggers, the HR trap, etc.).
        if (StoryDirector.I != null) StoryDirector.I.OnTaskCompleted(task);
    }
}