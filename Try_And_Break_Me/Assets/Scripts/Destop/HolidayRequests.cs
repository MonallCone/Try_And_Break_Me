using System.Collections.Generic;
using UnityEngine;

// A single holiday request the player approves or rejects in the HR swipe game.
public class HolidayRequest
{
    public string name;      // coworker (from the shared roster)
    public int days;
    public string dates;
    public string reason;

    public HolidayRequest(string name, int days, string dates, string reason)
    {
        this.name = name; this.days = days; this.dates = dates; this.reason = reason;
    }
}

// Builds a shuffled set of holiday requests for the HR minigame, using the SAME coworker names
// that appear in the company chat (so it's the same people).
public static class HolidayRequests
{
    private static readonly (int days, string dates, string reason)[] Pool =
    {
        (3,  "12th\u201314th",     "Family wedding out of town."),
        (1,  "next Friday",     "Doctor's appointment."),
        (10, "1st\u201310th next month", "Backpacking trip."),
        (5,  "the 20th\u201324th",  "Half-term with the kids."),
        (2,  "Mon\u2013Tue",        "Moving house."),
        (14, "all of August",   "Extended holiday abroad."),
        (1,  "Wednesday",       "Feeling burnt out, need a day."),
        (4,  "end of month",    "Wedding anniversary trip."),
        (7,  "next week",       "Just really need a break."),
        (14,  "all of september",        "my cat died"),
        (4,  "1st\u20134th",        "wanna sleep in for the week"),
        (60,  "All of Aug and Sept",        "killed my neighbours cat need to leave for a bit cause its awkward rn"),
        (2,  "Mon\u2013Tues",        "looking for other jobs"),
        (3,  "Wed\u2013Fri",        "Working my second job to pay off my loans"),
    };

    public static List<HolidayRequest> Build(int count)
    {
        var names = new List<string>(CoworkerNames.All);
        Shuffle(names);
        var pool = new List<(int, string, string)>(Pool);
        Shuffle(pool);

        var list = new List<HolidayRequest>();
        int n = Mathf.Min(count, Mathf.Min(names.Count, pool.Count));
        for (int i = 0; i < n; i++)
        {
            var (days, dates, reason) = pool[i];
            list.Add(new HolidayRequest(names[i], days, dates, reason));
        }
        return list;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
