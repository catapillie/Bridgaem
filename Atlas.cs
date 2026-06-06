using Foster.Framework;

namespace Bridgaem;

public static class Atlas
{
    private const string atlasPrefix = "./assets/img/";
    private static readonly Dictionary<string, Subtexture> images = [];

    internal static void Load(GraphicsDevice device)
    {
        foreach (var path in Directory.EnumerateFiles(atlasPrefix, "*.*", searchOption: SearchOption.AllDirectories))
        {
            string name = Path.ChangeExtension(path[atlasPrefix.Length..^4], null).Replace('\\', '/');
            images.Add(name, new Subtexture(new Texture(device, new Image(path))));
        }
    }

    public static Subtexture Get(string path)
    {
        return images[path];
    }
}