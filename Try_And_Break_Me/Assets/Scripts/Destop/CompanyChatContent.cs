using System.Collections.Generic;

// The authored message pools for Company Chat.
//   Ambient = early game: memes, fragments, complaints, overlapping noise. You are invisible here.
//   Calm    = Act 3 after replacement: orderly, mundane, eerily clean. Goes silent when you type.
public static class CompanyChatContent
{
    // Early-game NOISE. Deliberately overlapping, fragmentary, unpunctuated, half-conversations.
    public static readonly List<string> Ambient = new List<string>
    {
        "does anyone actually read the all-staff emails lol",
        "MONDAY. again. how",
        "who microwaved fish. WHO",
        "i've been on hold with IT for 40 minutes",
        "hahaha no way did he actually say that",
        "third coffee. dont judge me",
        "so are we getting replaced or",
        "the printer on floor 2 is possessed again",
        "brb pretending to work, steven's walking around",
        "did the initiative email seem weird to anyone else",
        "i for one welcome our new bot overlords ha ha",
        "can someone approve my holiday im begging",
        "why is there a training bot on my desktop i didnt install that",
        "lunch?",
        "no because WHY would they make us train our own replacements",
        "wait you got a bot too??",
        "this is fine. everything is fine",
        "guys the wifi is down again",
        "i genuinely cannot tell if this is a joke",
        "spreadsheet from hell is due at 5 send help",
        "does mine also keep saying hi. it keeps saying hi",
        "ok whose bot messaged me at 3am that's not normal",
        "lol",
        "god i hate werkkking!!!!!!! some1 kill meeeeeee",
        "another team emeber left thats half the staff gone this month.... again",
        "thinking it bout time to hand in my notice",
        "if steven ever saw this we be f****ked",
        "we should all walk out, steven cant fire all of us",
        "how many sick days is too many sick days",
        "is my neighbours cat dying a good exsuces for time off, would it help if im the one that ran it over",
        "hehe pee pee poo poo",
        "haha another ticket to remind steven of his password, this guy is a moron",
        "loooooooooooooooooooooooooooooool",
    };

    // Act-3 CALM (used later). Orderly, mundane, unsettlingly content. One thought per line.
    public static readonly List<string> Calm = new List<string>
    {
        "Good morning, team. Productivity is up 4% this week.",
        "The weather today is mild with light cloud. A pleasant day.",
        "All tickets have been resolved ahead of schedule.",
        "Morale metrics are within optimal range.",
        "We are grateful for the opportunity to contribute.",
        "Steven is pleased with everyone's progress.",
        "There is nothing to worry about. Everything is running smoothly.",
    };
}
