using UnityEngine;

// Central sound system. Assign AudioClips in the Inspector; the game calls the static helpers at
// the right moments. One-shot SFX play through a shared source; looping tracks (music, end-sequence
// ambience) each have their own source so they can start/stop independently.
//
// SETUP: put this on one GameObject in the scene (e.g. an empty "SoundManager"). Assign the clips.
// Any clip left empty is simply skipped \u2014 nothing errors if you haven't added a sound yet.
public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("One-shot SFX (assign your clips)")]
    [Tooltip("1. Plays when a work task is completed.")]
    public AudioClip taskComplete;
    [Tooltip("2a. Plays when the PLAYER sends a chat message.")]
    public AudioClip messageSend;
    [Tooltip("2b. Plays when a bot's message is RECEIVED (normal days).")]
    public AudioClip messageReceive;
    [Tooltip("2c. Plays when a bot's message is received on DAY 3 (the wrong/sinister ping).")]
    public AudioClip messageReceiveDay3;
    [Tooltip("2d. Plays when a bot is created.")]
    public AudioClip botCreated;
    [Tooltip("3. Plays when a new email arrives.")]
    public AudioClip emailPing;
    [Tooltip("4. Fog-horn when the desktop background inverts to red (3rd deletion).")]
    public AudioClip fogHorn;
    [Tooltip("5. Scream when a bot speaks during the end sequence / deletion.")]
    public AudioClip scream;
    [Tooltip("7. Typewriter click for each character typed (played rapidly).")]
    public AudioClip typewriter;

    [Header("Minigame SFX (assign your clips) \u2014 normal + dark variant")]
    [Tooltip("Cyber: a threat destroyed.")]
    public AudioClip cyberHit;
    [Tooltip("Cyber (dark): a cop destroyed in Kidnap Steven.")]
    public AudioClip cyberHitDark;
    [Tooltip("Maze: a step/move.")]
    public AudioClip mazeStep;
    [Tooltip("Maze (dark): a step in Find Steven.")]
    public AudioClip mazeStepDark;
    [Tooltip("HR: approve or reject a request.")]
    public AudioClip hrDecide;
    [Tooltip("HR (dark): approve/disapprove an item in Supplies.")]
    public AudioClip hrDecideDark;

    [Header("Dark sequence extras")]
    [Tooltip("Looping heavy breathing (Steven), played from the steven.wav window during Supplies.")]
    public AudioClip stevenBreathing;
    [Tooltip("Steven's scream \u2014 a DIFFERENT scream, played each time an item is approved in Supplies.")]
    public AudioClip stevenScream;

    [Header("Looping tracks")]
    [Tooltip("6. General background music (loops during normal play).")]
    public AudioClip backgroundMusic;
    [Tooltip("8. End-sequence background effect/drone (loops during the finale).")]
    public AudioClip endSequenceAmbience;

    [Header("Levels")]
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float typeVolume = 0.4f;

    private AudioSource _sfx;      // one-shots
    private AudioSource _music;    // looping background music
    private AudioSource _ambience; // looping end-sequence track
    private AudioSource _breathing; // looping heavy breathing (steven.wav window)
    private float _lastTypeTime;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;

        _music = gameObject.AddComponent<AudioSource>();
        _music.playOnAwake = false; _music.loop = true; _music.volume = musicVolume;

        _ambience = gameObject.AddComponent<AudioSource>();
        _ambience.playOnAwake = false; _ambience.loop = true; _ambience.volume = musicVolume;

        _breathing = gameObject.AddComponent<AudioSource>();
        _breathing.playOnAwake = false; _breathing.loop = true; _breathing.volume = sfxVolume;
    }

    private void Start()
    {
        // Start background music if a clip is assigned.
        PlayMusic();
    }

    // ---- one-shot helpers (safe to call anywhere; null clips are skipped) ----
    private void OneShot(AudioClip clip, float vol)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, vol);
    }

    public static void TaskComplete() { if (I) I.OneShot(I.taskComplete, I.sfxVolume); }
    public static void MessageSend()  { if (I) I.OneShot(I.messageSend, I.sfxVolume); }
    // Bot message received \u2014 uses the Day 3 variant once the final day has begun.
    public static void MessageReceive()
    {
        if (I == null) return;
        bool day3 = GameState.I != null && GameState.I.day >= 3;
        var clip = (day3 && I.messageReceiveDay3 != null) ? I.messageReceiveDay3 : I.messageReceive;
        I.OneShot(clip, I.sfxVolume);
    }
    public static void BotCreated()   { if (I) I.OneShot(I.botCreated, I.sfxVolume); }

    // Minigame SFX. Each takes a 'dark' flag for the Day 3 reskin variant (falls back to the
    // normal clip if the dark one isn't assigned).
    public static void CyberHit(bool dark = false)
    {
        if (I == null) return;
        I.OneShot(dark && I.cyberHitDark != null ? I.cyberHitDark : I.cyberHit, I.sfxVolume);
    }
    public static void MazeStep(bool dark = false)
    {
        if (I == null) return;
        I.OneShot(dark && I.mazeStepDark != null ? I.mazeStepDark : I.mazeStep, I.sfxVolume);
    }
    public static void HrDecide(bool dark = false)
    {
        if (I == null) return;
        I.OneShot(dark && I.hrDecideDark != null ? I.hrDecideDark : I.hrDecide, I.sfxVolume);
    }
    public static void EmailPing()    { if (I) I.OneShot(I.emailPing, I.sfxVolume); }
    public static void FogHorn()      { if (I) I.OneShot(I.fogHorn, I.sfxVolume); }
    public static void Scream()       { if (I) I.OneShot(I.scream, I.sfxVolume); }

    // Typewriter: throttled so rapid characters don't stack into noise.
    public static void TypewriterTick()
    {
        if (I == null) return;
        if (Time.unscaledTime - I._lastTypeTime < 0.03f) return;
        I._lastTypeTime = Time.unscaledTime;
        I.OneShot(I.typewriter, I.typeVolume);
    }

    // ---- looping tracks ----
    public static void PlayMusic()
    {
        if (I == null || I._music == null || I.backgroundMusic == null) return;
        I._music.clip = I.backgroundMusic; I._music.volume = I.musicVolume;
        if (!I._music.isPlaying) I._music.Play();
    }
    public static void StopMusic() { if (I && I._music) I._music.Stop(); }

    public static void StartEndAmbience()
    {
        if (I == null || I._ambience == null || I.endSequenceAmbience == null) return;
        // fade out music, bring in the end drone
        StopMusic();
        I._ambience.clip = I.endSequenceAmbience; I._ambience.volume = I.musicVolume;
        if (!I._ambience.isPlaying) I._ambience.Play();
    }
    public static void StopEndAmbience() { if (I && I._ambience) I._ambience.Stop(); }

    // Looping heavy breathing from the steven.wav window.
    public static void StartBreathing()
    {
        if (I == null || I._breathing == null || I.stevenBreathing == null) return;
        I._breathing.clip = I.stevenBreathing; I._breathing.volume = I.sfxVolume;
        if (!I._breathing.isPlaying) I._breathing.Play();
    }
    public static void StopBreathing() { if (I && I._breathing) I._breathing.Stop(); }

    // Steven's scream \u2014 distinct from the general end-sequence scream. Fires on each approved item.
    public static void StevenScream() { if (I) I.OneShot(I.stevenScream, I.sfxVolume); }
}