using Foster.Framework;

namespace Bridgaem.BaseEntity;

public class Parallax(float factor, float scroll, Subtexture tex, Color color) : Entity
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Scale { get; set; } = 1.0f;

    private readonly float factor = factor, scroll = scroll;
    private readonly Subtexture tex = tex;
    private readonly Color color = color;

    public override void Update()
    {
        X += scroll * Game.Dt;
    }

    public override void Render()
    {
        Game.Batch.ImageJustified(tex, new(X, Y), new(0.5f, 1.0f), 0.5f * Scale, color);
    }
}