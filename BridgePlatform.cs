using Box2D.NET;
using Foster.Framework;
using System.Numerics;

namespace Bridgaem;

public class BridgePlatform : PhysicsEntity
{
    private readonly Subtexture texture;
    private readonly float scale;

    public BridgePlatform(Vector2 pos)
    {
        B2Bodies.b2Body_SetTransform(BodyId, new B2Vec2(pos.X, pos.Y), B2MathFunction.b2MakeRot(0.0f));
        B2Bodies.b2Body_SetType(BodyId, B2BodyType.b2_kinematicBody);

        texture = Atlas.Get("bridge_platform");
        scale = 0.25f;
        AddDefaultBox(
            texture.Width * scale / 2f,
            texture.Height * scale / 2f,
            friction: 0.8f);
    }

    public override void Render()
    {
        // base.Render();

        B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(BodyId);
        B2Rot bodyRot = B2Bodies.b2Body_GetRotation(BodyId);
        float bodyAngle = float.Atan2(bodyRot.s, bodyRot.c);
        Game.Batch.PushMatrix(new(bodyPos.X, bodyPos.Y), Vector2.One, bodyAngle);

        Game.Batch.ImageJustified(texture, Vector2.Zero, new(.5f, .5f), scale, Color.White);

        Game.Batch.PopMatrix();

    }
}