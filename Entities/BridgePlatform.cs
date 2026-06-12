using Box2D.NET;
using Foster.Framework;
using System.Diagnostics;
using System.Numerics;

namespace Bridgaem.Entities;

public class BridgePlatform : PhysicsEntity
{
    private readonly Subtexture texture;
    private readonly float scale;
    private readonly float triggerHeight;
    public float Width { get; private set; }
    public float Height { get; private set; }

    public Vector2 TargetPos { get; set; }

    public BridgePlatform(Vector2 pos, Game.Direction dir)
    {
        TargetPos = pos;

        B2Bodies.b2Body_SetTransform(BodyId, new B2Vec2(pos.X, pos.Y), B2MathFunction.b2MakeRot(0.0f));
        B2Bodies.b2Body_SetType(BodyId, B2BodyType.b2_kinematicBody);

        texture = Atlas.Get(dir switch
        {
            Game.Direction.Left => "bridge_platform_left",
            Game.Direction.Right => "bridge_platform_right",
            _ => throw new UnreachableException(),
        });
        scale = 0.25f;

        Width = texture.Width * scale;
        Height = texture.Height * scale;
        triggerHeight = Height; // {F} azy on fait égale à la vraie height
                                // {L} a wise man once said ^

        AddDefaultBox(Width / 2f, Height / 2f, friction: 0.8f);
    }

    public override void Update()
    {
        B2Vec2 b2pos = B2Bodies.b2Body_GetPosition(BodyId);
        Vector2 pos = new(b2pos.X, b2pos.Y);
        Vector2 vel = (TargetPos - pos) * 0.5f;
        B2Bodies.b2Body_SetLinearVelocity(BodyId, new(vel.X, vel.Y));
        base.Update();
    }

    public override void Render()
    {
        B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(BodyId);
        B2Rot bodyRot = B2Bodies.b2Body_GetRotation(BodyId);
        float bodyAngle = float.Atan2(bodyRot.s, bodyRot.c);
        Game.Batch.PushMatrix(new(bodyPos.X, bodyPos.Y), Vector2.One, bodyAngle);

        Game.Batch.ImageJustified(texture, Vector2.Zero, new(.5f, .5f), scale, Color.White);

        Game.Batch.PopMatrix();
    }

    public bool IsDetected(Player player)
    {
        B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(BodyId);

        const float widthPercent = 0.75f;
        float w = Width * widthPercent;
        Rect rect = new(bodyPos.X - w / 2, bodyPos.Y - triggerHeight, w, triggerHeight);

        return rect.Contains(player.ChassisPos);
    }
}