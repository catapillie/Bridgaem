using Box2D.NET;
using Foster.Framework;
using System.Numerics;

namespace Bridgaem;

public class Bridge : Entity
{
    private readonly Subtexture tileTexture;

    private float frictionTorque;
    private float constraintHertz;
    private float constraintDampingRatio;
    private float springHertz;
    private float springDampingRatio;

    private readonly B2BodyId[] bodyIds;
    private readonly B2JointId[] jointIds;

    public Bridge(Vector2 left, Vector2 right, int count)
    {
        tileTexture = Atlas.Get("bridge_tile");

        bodyIds = new B2BodyId[count];
        jointIds = new B2JointId[count + 1];

        constraintHertz = 60.0f;
        constraintDampingRatio = 0.0f;
        springHertz = 2.0f;
        springDampingRatio = 0.7f;
        frictionTorque = 200.0f;

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = B2Types.b2DefaultBodyDef();
            groundId = B2Bodies.b2CreateBody(Game.WorldId, bodyDef);
        }

        float hw = 0.5f;
        B2Polygon box = B2Geometries.b2MakeBox(hw, 0.125f);

        B2ShapeDef shapeDef = B2Types.b2DefaultShapeDef();
        shapeDef.density = 20.0f;


        B2RevoluteJointDef jointDef = B2Joints.b2DefaultRevoluteJointDef();
        jointDef.enableMotor = true;
        jointDef.maxMotorTorque = frictionTorque;
        jointDef.enableSpring = true;
        jointDef.hertz = springHertz;
        jointDef.dampingRatio = springDampingRatio;

        int jointIndex = 0;
        B2BodyId prevBodyId = groundId;

        for (int i = 0; i < count; ++i)
        {
            float t = (float)i / count;

            B2BodyDef bodyDef = B2Types.b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new(Calc.Lerp(left.X, right.X, t) + hw, Calc.Lerp(left.Y, right.Y, t));
            bodyDef.linearDamping = 0.1f;
            bodyDef.angularDamping = 0.1f;
            bodyIds[i] = B2Bodies.b2CreateBody(Game.WorldId, bodyDef);
            B2Shapes.b2CreatePolygonShape(bodyIds[i], shapeDef, box);

            B2Vec2 pivot = new(Calc.Lerp(left.X, right.X, t), Calc.Lerp(left.Y, right.Y, t));
            jointDef.@base.bodyIdA = prevBodyId;
            jointDef.@base.bodyIdB = bodyIds[i];
            jointDef.@base.localFrameA.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointIds[jointIndex++] = B2Joints.b2CreateRevoluteJoint(Game.WorldId, jointDef);

            prevBodyId = bodyIds[i];
        }

        {
            B2Vec2 pivot = new(right.X, right.Y);
            jointDef.@base.bodyIdA = prevBodyId;
            jointDef.@base.bodyIdB = groundId;
            jointDef.@base.localFrameA.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointIds[jointIndex++] = B2Joints.b2CreateRevoluteJoint(Game.WorldId, jointDef);
        }
    }


    public override void Destroy()
    {
        base.Destroy();
        Game.Instance.GrantPlacement(Game.PlacementKind.Bridge, 1);

        for (int i = 0; i < bodyIds.Length; ++i)
            B2Joints.b2DestroyJoint(jointIds[i], false);

        for (int i = 0; i < bodyIds.Length; ++i)
            B2Bodies.b2DestroyBody(bodyIds[i]);
    }

    public override void Render()
    {
        base.Render();

        foreach (var bodyId in bodyIds)
        {
            B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(bodyId);
            B2Rot bodyRot = B2Bodies.b2Body_GetRotation(bodyId);
            float bodyAngle = float.Atan2(bodyRot.s, bodyRot.c);
            Game.Batch.PushMatrix(new(bodyPos.X, bodyPos.Y), Vector2.One, bodyAngle);

            Game.Batch.ImageJustified(tileTexture, Vector2.Zero, new(.5f, .5f), 0.1f, Color.White);

            Game.Batch.PopMatrix();
        }
    }
}
