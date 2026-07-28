using System.Collections.Generic;

// The company's fake coworkers. Shared so the SAME people who post in Company Chat also show up
// later in the HR holiday-swipe and help-desk minigames — making the world feel small and real
// (the 'Dave' complaining in chat is the 'Dave' whose holiday you approve).
public static class CoworkerNames
{
    public static readonly List<string> All = new List<string>
    {
        "Dave", "Priya", "Marcus", "Chloe", "Tomasz", "Nadia",
        "Greg", "Aisha", "Liam", "Fenwick", "Sam", "Rhona", "Pete", "Mark", "Samuel", "Rick", "Grace", "Rebecca",
    };

    private static readonly System.Random _rng = new System.Random();

    public static string Random()
    {
        return All[_rng.Next(All.Count)];
    }
}
