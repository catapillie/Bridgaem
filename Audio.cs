using MiniAudioEx.Core.StandardAPI;

namespace Bridgaem;

public static class Audio
{
    public const uint SAMPLE_RATE = 44100;
    public const uint NUM_CHANNELS = 2;

    private const string soundsPrefix = "./assets/sound/";
    private static readonly Dictionary<string, AudioClip> sounds = [];
    private static AudioSource globalSource = null!;

    internal static void Load()
    {
        AudioContext.Initialize(SAMPLE_RATE, NUM_CHANNELS);
        globalSource = CreateSource();

        foreach (var path in Directory.EnumerateFiles(soundsPrefix, "*.*", searchOption: SearchOption.AllDirectories))
        {
            string name = Path.ChangeExtension(path[soundsPrefix.Length..^4], null).Replace('\\', '/');
            AudioClip clip = new(path, false);
            sounds.Add(name, clip);
        }
    }

    internal static void Unload()
    {
        AudioContext.Deinitialize();
    }

    public static void Oneshot(this AudioSource source, string name)
    {
        if (!sounds.TryGetValue(name, out var clip))
            return;
        source.PlayOneShot(clip);
    }

    public static void Oneshot(string name)
        => globalSource.Oneshot(name);

    public static void Loop(this AudioSource source, string name)
    {
        if (!sounds.TryGetValue(name, out var clip))
            return;
        source.Loop = true;
        source.Play(clip);
    }

    public static AudioSource CreateSource()
        => new()
        {
            Volume = 0.25f,
        };
}