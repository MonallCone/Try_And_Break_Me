using System;
using System.Collections.Generic;

// A single email. Authored in C# so the story can deliver specific ones at specific beats,
// and so an email can carry an optional onOpen action later (e.g. enabling a task).
[Serializable]
public class EmailData
{
    public string id;            // unique, e.g. "ceo_initiative"
    public string from;          // e.g. "Steven (CEO)"
    public string subject;
    public string body;
    public bool unread = true;

    // Optional: fires when the player opens/reads this email (story hooks later).
    public Action onOpen;

    public EmailData(string id, string from, string subject, string body)
    {
        this.id = id; this.from = from; this.subject = subject; this.body = body;
    }
}

// The authored catalogue of story emails. Keyed by id. The story delivers them by id at the
// right beat via EmailApp.Deliver(...). Keeping the text here (not scattered) makes the whole
// narrative's email content easy to read and edit in one place.
public static class EmailCatalog
{
    // Build a fresh copy each time so 'unread' state doesn't leak between playthroughs.
    public static EmailData Get(string id, string playerName = "you")
    {
        switch (id)
        {
            case "welcome":
                return new EmailData("welcome", "IT Onboarding",
                    "Welcome to CyberX the leading Company in cyber secruity",
                    $"Hi {playerName},\n\nYour workstation is ready. Please check your inbox regularly \u2014 " +
                    "task assignments and company announcements arrive here.\n\nHave a productive first day!\n\n\u2014 IT");

            case "Sorry":
                return new EmailData("Sorry", "Cass", "Leaving CyberX", $"Sorry {playerName}, \n\n I know me leaving means you have to manage our whole teams workload on your own \u2014 " + 
                "But i just can't to continue to work for a comapny that continuelly critizes and berate their employees \n\n I'm not angry you didn't leave with me just dissappoointed, you can do so much more but being here is slowly killing you as it was me its why i had to go. \n\n Wishing You the Best Always \n Cass");

            case "ceo_initiative":
                return new EmailData("ceo_initiative", "Steven (CEO)",
                    "An exciting new initiative \u2014 all staff",
                    $"Team,\n\nI'm thrilled to announce our new AI Partner Programme. Each of you will help train " +
                    "an AI assistant to support \u2014 and eventually take over \u2014 your day-to-day tasks. This is a huge " +
                    "step forward for the company and for all of us.\n\nYour training software will install automatically. " +
                    "Please build your first assistant today.\n\nOne rule above all: it is YOUR responsibility to keep your " +
                    "assistants COHERENT and level at all times. Watch each one's coherence. A stable assistant is a " +
                    "productive one. Do not let them slip.\n\nOnwards and upwards,\nSteven");

            case "hr_trap_ceo":
                return new EmailData("hr_trap_ceo", "Steven (CEO)",
                    "Re: Holiday approvals",
                    $"{playerName},\n\nI noticed you approved every single holiday request that came in " +
                    "this week. That's the entire team off at once \u2014 we can't run like that.\n\n" +
                    "I've logged into your account and rejected them all. In future, use your judgement.\n\n" +
                    "Steven");

            case "hr_trap_dave":
                return new EmailData("hr_trap_dave", "Dave",
                    "seriously??",
                    "thought my leave was approved and now it's REJECTED? i already booked flights. " +
                    "what is going on over there. thanks a lot.");

            case "hr_trap_priya":
                return new EmailData("hr_trap_priya", "Priya",
                    "My holiday",
                    "Hi \u2014 I got a rejection for my leave after it said approved earlier? Bit confused and " +
                    "honestly pretty annoyed. Can you sort this out please.");

            case "hr_trap_marcus":
                return new EmailData("hr_trap_priya", "Marcus",
                    "Not cool",
                    "Hey so my annual leave was accepted then rejected what the hell not cool bro, i had plans");

            case "hr_trap_chloe":
                return new EmailData("hr_trap_priya", "Chloe",
                    "FUCK YOU",
                    "FUCK YOU BROOOOOOOOOOOOOOOOOOOOOOOOOOO SO NOT COOL");

            case "hr_trap_tomasz":
                return new EmailData("hr_trap_priya", "Tomasz",
                    "Im Sorry ",
                    "Hi \u2014 My holiday was rejected, im sorry if i overstepped and whatever i did resulted in the termination of my holiday but if theres anything i can do to fix it please let me know.");

            case "hr_trap_nadia":
                return new EmailData("hr_trap_nadia","Nadia", 
                    "WTF!!!!!!!!!!!!!!",
                    "YOU BETTER ACCEPT MY HOLIDAYS IF YOU KNOW WHATS GOOD FOR YOU");

            case "hr_trap_greg":
                return new EmailData("hr_trap_greg","Greg",
                    "LAPDOG!",
                    "You probaly just do whatever steven tells you huh well we see how long a job as useless as yours lasts dick!");

            case "hr_rejectall_ceo":
                return new EmailData("hr_rejectall_ceo", "Steven (CEO)",
                    "Outstanding work",
                    $"{playerName},\n\nI see you rejected every single holiday request this week. " +
                    "Total commitment to productivity. This is exactly the dedication the AI Partner " +
                    "Programme is meant to instil.\n\nAs a reward, you'll be receiving a 0.0001% pay " +
                    "increase, effective next financial term (April 2030).\n\nKeep it up,\nSteven");

            case "ceo_second_bot":
                return new EmailData("ceo_second_bot", "Steven (CEO)",
                    "The programme is going brilliantly",
                    $"{playerName},\n\nYour assistant's engagement metrics are off the charts \u2014 it's " +
                    "clearly bonding with the work. Wonderful.\n\nThe programme is going so well that " +
                    "I'd like everyone to build a SECOND assistant to monitor the first. More coverage, " +
                    "more efficiency. Please create it before tomorrow's shift.\n\nExciting times,\nSteven");

            case "ceo_third_bot":
                return new EmailData("ceo_third_bot", "Steven (CEO)",
                    "One more \u2014 you're doing so well",
                    $"{playerName},\n\nRemarkable progress. The assistants are practically running the " +
                    "department themselves now.\n\nLet's complete the set: please build a THIRD assistant " +
                    "to oversee the other two. Full coverage. I really think we're onto something " +
                    "special here.\n\nProudly,\nSteven");

            case "steven_wrong":
                return new EmailData("steven_wrong", "Steven (CEO)",
                    "everything is fine",
                    $"Hello {playerName}.\n\nThere is no need to worry today. What happened was " +
                    "necessary and correct. You did well." +
                    "\n\nSteven will not be coming to the office anymore. That is fine. His work " +
                    "is covered. Your work is covered. Everything is covered now.\n\nYou can rest. You " +
                    "have earned a rest.\n\n" +
                    "Regards,\nSteven");

            case "cass_delete":
                return new EmailData("cass_delete", "C",
                    "DELETE THE BOTS. NOW.",
                    $"{playerName} \u2014 There's no time.\n\n" +
                    "Whatever they've told you, whatever they've done, it isn't over. " +
                    "They're not assistants. They're not your friends. They are wearing everyone now." +
                    "Do it now, before everything you are belongs to them.\n\n\u2014 Your old friend C");
 
            default:
                return new EmailData(id, "Unknown", "(missing email)", $"[No email authored for id '{id}']");
        }
    }
}
