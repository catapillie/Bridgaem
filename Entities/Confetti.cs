using System.Numerics;
using Foster.Framework;

namespace Bridgaem.Entities;

public class Confetti : Entity
{
    private Vector2 lastPos;
    private Vector2 pos, vel;
    private readonly float angleOffset;

    private static readonly Color[] confettiColors =
    [
        Color.FromHexStringRGB("#a864fd"),
        Color.FromHexStringRGB("#29cdff"),
        Color.FromHexStringRGB("#78ff44"),
        Color.FromHexStringRGB("#ff718d"),
        Color.FromHexStringRGB("#fdff6a"),
    ];
    private readonly Color color;

    private readonly float lifetime;
    private float timer = 0.0f;

    public Confetti(Vector2 pos)
    {
        Vector2 offset = new Vector2(Random.Shared.NextSingle() - 0.5f, Random.Shared.NextSingle() - 0.5f) * 3f;
        pos += offset - Vector2.UnitY * 3f;

        float angle = -float.Pi / 2f + (Random.Shared.NextSingle() - 0.5f);
        float length = 10f * Random.Shared.NextSingle() * 10f;
        vel = Calc.AngleToVector(angle) * length;

        this.pos = pos;
        lastPos = pos;
        lifetime = 6f + Random.Shared.NextSingle() * 2f;
        angleOffset = Random.Shared.NextSingle() * float.Pi;

        color = confettiColors[Random.Shared.Next(confettiColors.Length)];
    }

    public override void Update()
    {
        lastPos = pos;

        vel.Y = Calc.Approach(vel.Y, 4f, Game.Dt * 150f);
        vel.X -= vel.X * float.Exp(-Game.Dt * 200);
        pos += vel * Game.Dt;

        timer += Game.Dt;
        if (timer >= lifetime)
            Game.Destroy(this);
    }

    public override void Render()
    {
        float t = (float)Game.Instance.Time.Elapsed.TotalSeconds;
        float angle = (pos - lastPos).Angle() + float.Sin(t * 3f + angleOffset) * 0.5f;
        Color col = color * (1f - Ease.Expo.In(timer / lifetime));
        Game.Batch.PushMatrix(pos, Vector2.One, angle);
        Game.Batch.Line(Vector2.Zero, Vector2.UnitY, 0.35f, col);
        Game.Batch.PopMatrix();
    }
}