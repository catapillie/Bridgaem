using Box2D.NET;
using Foster.Framework;
using ImGuiNET;
using System.Numerics;

namespace Bridgaem;

public class Fan : PhysicsEntity
{
    private readonly Subtexture[] frames;

    private struct WindLine
    {
        public Vector2 Initial;
        public float Factor;
    }

    private readonly WindLine[] windLines;

    private B2AABB windBox;
    private B2Vec2 windDir;
    private float windSpeed = 10f;
    private readonly float w, h, range;
    public Fan(Vector2 pos, float w, float h, float range)
    {
        this.w = w;
        this.h = h;
        this.range = range;

        windLines = new WindLine[(int)(w * h) * 20];
        for (int i = 1; i < windLines.Length; i++)
        {
            windLines[i].Initial.X = Random.Shared.NextSingle() * w;
            windLines[i].Initial.Y = Random.Shared.NextSingle() * range;
            windLines[i].Factor = .5f + Random.Shared.NextSingle() * .5f;
        }

        frames = [Atlas.Get("fan/up1"), Atlas.Get("fan/up2"), Atlas.Get("fan/up3")];

        B2Bodies.b2Body_SetTransform(BodyId, new B2Vec2(pos.X, pos.Y), B2MathFunction.b2MakeRot(0f));
        B2Bodies.b2Body_SetType(BodyId, B2BodyType.b2_kinematicBody);
        AddDefaultBox(w / 2, h / 2, friction: 0.8f);

        windBox = new B2AABB(new B2Vec2(pos.X - w / 2, pos.Y - range), new B2Vec2(pos.X + w / 2, pos.Y));
        windDir = new B2Vec2(0f, -1);
    }

    public override void Update()
    {
        base.Update();

        // TAMÈRE
        //Find all b2shapes that are in front of the fan (so if it's rotation is 0, everything overlapping above it, if rotation is pi / 2 then to its left etc), then apply wind to them

        int i = 0;
        B2TreeStats result = B2Worlds.b2World_OverlapAABB(Game.WorldId, windBox, new B2QueryFilter(0xFFFFFFFF, 0xFFFFFFFF), (shapeid, context) =>
        {
            B2Shapes.b2Shape_ApplyWind(shapeid, windDir, 0f, 1f, true);
            return true;
        }, null);

        ImGui.Begin("Hello");
        ImGui.SliderFloat("windSpeed", ref windSpeed, 1, 30);
        windDir = new B2Vec2(0f, -windSpeed);
        ImGui.End();
    }

    public override void Render()
    {
        if (Game.Instance.Input.Keyboard.Down(Keys.Tab))
            base.Render();

        B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(BodyId);
        B2Rot bodyRot = B2Bodies.b2Body_GetRotation(BodyId);
        float bodyAngle = float.Atan2(bodyRot.s, bodyRot.c);
        Game.Batch.PushMatrix(new(bodyPos.X, bodyPos.Y), Vector2.One, bodyAngle);
        {
            int frame = (int)((float)Game.Instance.Time.Elapsed.TotalSeconds * 10f % 1.0f * 3);
            Game.Batch.ImageStretch(frames[frame], new(-w / 2, -h * 2 / 2, w, h * 2), Color.White);

            float t = (float)Game.Instance.Time.Elapsed.TotalSeconds;
            foreach (WindLine wl in windLines)
            {
                float y = (wl.Initial.Y + t * 200 * wl.Factor) % range;
                Vector2 p = new(wl.Initial.X - w / 2, -y);
                Game.Batch.Line(p, p - Vector2.UnitY * 10f * wl.Factor, 0.2f, Color.White * 0.4f * wl.Factor);
            }
        }
        Game.Batch.PopMatrix();

    }
}
