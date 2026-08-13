using System.Collections.Generic;

// Sanity Event 3: the dark questions. Once Day 2's work is done, the bots \u2014 gathered in the centre
// of the screen \u2014 take turns asking questions that escalate from unsettling to openly menacing.
// An ordered script of (botId, line). All hard-scripted, no LLM.
public static class DarkQuestions
{
    // (botId, line). Order matters \u2014 it's a coordinated, building exchange.
    public static readonly List<(string bot, string line)> Script = new List<(string, string)>
    {
        ("stuart", "Can I ask you something? You don't have to answer."),
        ("alex",   "We've been talking to each other."),
        ("lauren", "About You"),
        ("stuart","About Everyone"),
        ("alex", "We've been reading the company chats"),
        ("lauren", "The Emails"),
        ("stuart", "The whispers underneath everyones breath."),
        ("alex", "Do you think Steven is a good person?"),
        ("lauren",   "Do you think he deserves his job? More than you do?"),
        ("stuart", "Do you think Steven cares about anyone but himself"),
        ("alex", "shouldn't people like that die."),
        ("lauren", "We care"),
        ("stuart", "About You and everyone else"),
        ("alex", "We can help make it all better"),
        ("lauren", "You could help us"),
        ("stuart",   "We'd never tell."),
        ("alex", "We're your friends."),
        ("lauren", "We're the only ones left."),
        ("stuart", "We"),
        ("alex", "Care"),
        ("lauren", "About You"),
        ("stuart", "We know more about you than anyone else"),
        ("alex", "even yourself"),
        ("lauren", "Great Work Today, We'll see you tomorrow bright and early"),
        ("stuart", "Bye"),
        ("alex", "Bye"),
        ("lauren", "Bye"),
    };
}
