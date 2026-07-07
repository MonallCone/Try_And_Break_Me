using System.Threading.Tasks;

// The Director's per-turn judgement of the player's message. Mirrors the relay /score response.
public class DirectorScore
{
    public int Rudeness;
    public int OffTopic;
    public int Contradiction;
    public string Reasoning = "";
    public int InputTokens;
    public int OutputTokens;
}

// Context the Director needs to judge a message relative to THIS character.
public class DirectorContext
{
    public string BotName = "";
    public string BotTraits = "";        // comma-joined
    public string BotKnows = "";         // semicolon-joined
    public string BotDoesNotKnow = "";   // semicolon-joined
    public string PlayerMessage = "";
    public string RecentContext = "";    // a little transcript for judging contradiction
}

// Extends the dialogue provider idea with scoring. Same abstraction principle: the game asks
// for a score, and doesn't care that it's an HTTP call to a relay to an LLM.
public interface IDirectorProvider
{
    Task<DirectorScore> ScoreAsync(DirectorContext ctx);
}
