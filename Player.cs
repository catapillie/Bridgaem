using Box2D.NET;
using Bridgaem.Utility;
using Foster.Framework;
using ImGuiNET;
using System.Numerics;

namespace Bridgaem;

public class Player : Entity
{
    public Vector2 RespawnPos;
    private readonly Subtexture chassisTexture, wheelTexture;

    private float scale = 2.0f;
    private float speed = 90f;
    private float torque = 180f;
    private float hertz = 1.6f;
    private float dampingRatio = 1.5f;
    private float friction = 6f;
    private float density = 0.475f;
    private float gravityScale = 4.16f;

    public B2BodyId Chassis;
    public B2BodyId FrontWheel;
    public B2BodyId BackWheel;
    private readonly B2JointId frontwheelJointId;
    private readonly B2JointId backwheelJointId;

    public bool Flipped;

    public Vector2 ChassisPos
    {
        get
        {
            var b2Vec = B2Bodies.b2Body_GetPosition(Chassis);
            return new(b2Vec.X, b2Vec.Y);
        }
    }

    public Player(Vector2 position)
    {
        chassisTexture = Atlas.Get("car/chassis");
        wheelTexture = Atlas.Get("car/wheel");

        const float halfHeight = 0.5f;
        B2Vec2[] vertices = [
            new B2Vec2(-1.50f, -halfHeight - 0.5f),
                new B2Vec2( 1.50f, -halfHeight - 0.5f),
                new B2Vec2( 1.50f,  halfHeight - 0.5f),
                new B2Vec2(-1.50f,  halfHeight - 0.5f),
            ];

        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i].X *= 0.85f * scale;
            vertices[i].Y *= 0.85f * scale;
        }

        B2Hull hull = B2Hulls.b2ComputeHull(vertices, vertices.Length);
        B2Polygon chassis = B2Geometries.b2MakePolygon(hull, 0.15f * scale);

        B2ShapeDef shapeDef = B2Types.b2DefaultShapeDef();
        shapeDef.density = density / scale;
        shapeDef.material.friction = 0.2f;

        B2Circle circle = new(new B2Vec2(0f, 0f), 0.4f * scale);

        B2BodyDef bodyDef = B2Types.b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(0.0f + position.X, -1.0f * scale + position.Y);
        bodyDef.isBullet = true;
        RespawnPos = bodyDef.position.ToVector2();
        Chassis = B2Bodies.b2CreateBody(Game.WorldId, bodyDef);
        B2Shapes.b2CreatePolygonShape(Chassis, shapeDef, chassis);

        shapeDef.density = 2.0f / scale;
        shapeDef.material.friction = friction;
        shapeDef.material.rollingResistance = 0.1f;

        bodyDef.position = new B2Vec2(-1.0f * scale + position.X, -0.35f * scale + position.Y);
        bodyDef.allowFastRotation = true;
        bodyDef.isBullet = false;
        BackWheel = B2Bodies.b2CreateBody(Game.WorldId, bodyDef);
        B2Shapes.b2CreateCircleShape(BackWheel, shapeDef, circle);

        bodyDef.position = new B2Vec2(1.0f * scale + position.X, -0.4f * scale + position.Y);
        bodyDef.allowFastRotation = true;
        FrontWheel = B2Bodies.b2CreateBody(Game.WorldId, bodyDef);
        B2Shapes.b2CreateCircleShape(FrontWheel, shapeDef, circle);


        B2Vec2 pivot = B2Bodies.b2Body_GetPosition(FrontWheel);

        B2WheelJointDef jointDef = B2Joints.b2DefaultWheelJointDef();
        jointDef.@base.bodyIdA = Chassis;
        jointDef.@base.bodyIdB = FrontWheel;
        jointDef.@base.localFrameA.q = B2MathFunction.b2MakeRot(0.5f * (float)Math.PI);
        jointDef.@base.localFrameA.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
        jointDef.@base.localFrameB.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
        jointDef.motorSpeed = 0.0f;
        jointDef.maxMotorTorque = torque;
        jointDef.enableMotor = true;
        jointDef.hertz = hertz;
        jointDef.dampingRatio = dampingRatio;
        jointDef.lowerTranslation = -0.25f * scale;
        jointDef.upperTranslation = 0.25f * scale;
        jointDef.enableLimit = true;
        frontwheelJointId = B2Joints.b2CreateWheelJoint(Game.WorldId, jointDef);

        pivot = B2Bodies.b2Body_GetPosition(BackWheel);
        jointDef.@base.bodyIdA = Chassis;
        jointDef.@base.bodyIdB = BackWheel;
        jointDef.@base.localFrameA.q = B2MathFunction.b2MakeRot(0.5f * (float)Math.PI);
        jointDef.@base.localFrameA.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
        jointDef.@base.localFrameB.p = B2Bodies.b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
        backwheelJointId = B2Joints.b2CreateWheelJoint(Game.WorldId, jointDef);

        ApplyPhysicsParameters();
    }

    private void ApplyPhysicsParameters()
    {

        B2WheelJoints.b2WheelJoint_SetMaxMotorTorque(frontwheelJointId, torque);
        B2WheelJoints.b2WheelJoint_SetSpringHertz(frontwheelJointId, hertz);
        B2WheelJoints.b2WheelJoint_SetSpringDampingRatio(frontwheelJointId, dampingRatio);
        B2WheelJoints.b2WheelJoint_SetMaxMotorTorque(backwheelJointId, torque);
        B2WheelJoints.b2WheelJoint_SetSpringHertz(backwheelJointId, hertz);
        B2WheelJoints.b2WheelJoint_SetSpringDampingRatio(backwheelJointId, dampingRatio);
        B2ShapeId[] shape = new B2ShapeId[1];
        B2Bodies.b2Body_GetShapes(FrontWheel, shape, 1);
        B2Shapes.b2Shape_SetFriction(shape[0], friction);
        B2Bodies.b2Body_GetShapes(BackWheel, shape, 1);
        B2Shapes.b2Shape_SetFriction(shape[0], friction);
        B2Bodies.b2Body_GetShapes(Chassis, shape, 1);
        B2Shapes.b2Shape_SetDensity(shape[0], density, true);
        B2Bodies.b2Body_SetGravityScale(Chassis, gravityScale);
    }

    public override void Update()
    {
        base.Update();

        bool canInput = Game.Instance.CurrentState is Game.State.Playing;
        if (Game.Instance.Input.Keyboard.Down(Keys.A) && canInput)
        {
            B2WheelJoints.b2WheelJoint_EnableMotor(frontwheelJointId, true);
            B2WheelJoints.b2WheelJoint_SetMotorSpeed(frontwheelJointId, -speed);
            B2WheelJoints.b2WheelJoint_EnableMotor(backwheelJointId, false);
            B2WheelJoints.b2WheelJoint_SetMaxMotorTorque(frontwheelJointId, torque);
        }
        else if (Game.Instance.Input.Keyboard.Down(Keys.D) && canInput)
        {
            B2WheelJoints.b2WheelJoint_EnableMotor(backwheelJointId, true);
            B2WheelJoints.b2WheelJoint_SetMotorSpeed(backwheelJointId, speed);
            B2WheelJoints.b2WheelJoint_EnableMotor(frontwheelJointId, false);
            B2WheelJoints.b2WheelJoint_SetMaxMotorTorque(backwheelJointId, torque);
        }
        else
        {
            B2WheelJoints.b2WheelJoint_EnableMotor(frontwheelJointId, true);
            B2WheelJoints.b2WheelJoint_EnableMotor(backwheelJointId, true);
            B2WheelJoints.b2WheelJoint_SetMaxMotorTorque(backwheelJointId, torque / 5);
            B2WheelJoints.b2WheelJoint_SetMaxMotorTorque(frontwheelJointId, torque / 5);
            B2WheelJoints.b2WheelJoint_SetMotorSpeed(backwheelJointId, 0.0f);
            B2WheelJoints.b2WheelJoint_SetMotorSpeed(frontwheelJointId, 0.0f);
        }

        B2Rot r = B2Bodies.b2Body_GetRotation(Chassis);

        Vector2 up = new Vector2(-r.s, r.c);
        B2ContactData[] contactData = new B2ContactData[1];
        int contactCount = B2Bodies.b2Body_GetContactData(Chassis, contactData, 1);

        if (canInput && Vector2.Dot(up, Vector2.UnitY) < 0.2f && contactCount > 0)
        {
            Flipped = true;

            if (Game.Instance.Input.Keyboard.Pressed(Keys.Space))
            {
                B2Bodies.b2Body_ApplyLinearImpulseToCenter(Chassis, new B2Vec2(0, -120), true);
                B2Bodies.b2Body_ApplyAngularImpulse(Chassis, 200f, true);

                // Game.Instance.PlaySound("flip.wav");
            }
        }
        else
            Flipped = false;

        B2Joints.b2Joint_WakeBodies(backwheelJointId);
        B2Joints.b2Joint_WakeBodies(frontwheelJointId);

        if (ChassisPos.Y >= 100)
        {
            // Game.Instance.PlaySound("crash.wav");
            Respawn();
        }

#if DEBUG
        ImGui.Begin("Hello");
        bool changed = ImGui.SliderFloat("scale", ref scale, 0.1f, 10f);
        changed |= ImGui.SliderFloat("speed", ref speed, 10f, 500f);
        changed |= ImGui.SliderFloat("torque", ref torque, 10f, 500f);
        changed |= ImGui.SliderFloat("hertz", ref hertz, 1f, 15f);
        changed |= ImGui.SliderFloat("dampingRatio", ref dampingRatio, 0.1f, 1.5f);
        changed |= ImGui.SliderFloat("friction", ref friction, 0.1f, 10f);
        changed |= ImGui.SliderFloat("density", ref density, 0.01f, 1f);
        changed |= ImGui.SliderFloat("gravity", ref gravityScale, 1f, 5f);
        if (changed)
            ApplyPhysicsParameters();
        ImGui.End();
#endif
    }

    private void RenderChassis()
    {
        B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(Chassis);
        B2Rot bodyRot = B2Bodies.b2Body_GetRotation(Chassis);
        float bodyAngle = float.Atan2(bodyRot.s, bodyRot.c);
        float xScale = Game.Instance.CurrentDirection is Game.Direction.Right ? +1f : -1f;
        Game.Batch.PushMatrix(new(bodyPos.X, bodyPos.Y), new(xScale, 1.0f), bodyAngle);
        {
            Game.Batch.ImageJustified(chassisTexture, new(0f, -.4f), new(.5f, .6f), 0.2f, Color.White);
        }
        Game.Batch.PopMatrix();
    }

    private void RenderWheel(B2BodyId bodyId)
    {
        B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(bodyId);
        B2Rot bodyRot = B2Bodies.b2Body_GetRotation(bodyId);
        float bodyAngle = float.Atan2(bodyRot.s, bodyRot.c);
        Game.Batch.PushMatrix(new(bodyPos.X, bodyPos.Y), new(1.0f, 1.0f), bodyAngle);
        Game.Batch.ImageJustified(wheelTexture, new(+.1f, 0f), new(.5f, .5f), 0.2f, Color.White);
        Game.Batch.PopMatrix();
    }

    public override void Render()
    {
        base.Render();

        RenderWheel(FrontWheel);
        RenderWheel(BackWheel);
        RenderChassis();

        if (Game.Instance.Input.Keyboard.Down(Keys.Tab))
        {
            Utils.RenderB2Body(Chassis);
            Utils.RenderB2Body(FrontWheel);
            Utils.RenderB2Body(BackWheel);
        }
    }

    public void Respawn()
    {
        B2Bodies.b2Body_SetTransform(Chassis, RespawnPos.ToB2V2(), B2MathFunction.b2MakeRot(0f));
        B2Bodies.b2Body_SetTransform(FrontWheel, RespawnPos.ToB2V2(), B2MathFunction.b2MakeRot(0f));
        B2Bodies.b2Body_SetTransform(BackWheel, RespawnPos.ToB2V2(), B2MathFunction.b2MakeRot(0f));
        B2Bodies.b2Body_SetLinearVelocity(Chassis, new B2Vec2(0f, 0f));
        B2Bodies.b2Body_SetAngularVelocity(Chassis, 0f);
        B2Bodies.b2Body_SetLinearVelocity(BackWheel, new B2Vec2(0f, 0f));
        B2Bodies.b2Body_SetAngularVelocity(BackWheel, 0f);
        B2Bodies.b2Body_SetLinearVelocity(FrontWheel, new B2Vec2(0f, 0f));
        B2Bodies.b2Body_SetAngularVelocity(FrontWheel, 0f);
    }

    public override void Destroy()
    {
        base.Destroy();

        B2Joints.b2DestroyJoint(frontwheelJointId, false);
        B2Joints.b2DestroyJoint(backwheelJointId, false);
        B2Bodies.b2DestroyBody(Chassis);
        B2Bodies.b2DestroyBody(FrontWheel);
        B2Bodies.b2DestroyBody(BackWheel);
    }
}
