using Box2D.NET;
using Bridgaem.Utility;
using ImGuiNET;
using System.Numerics;

namespace Bridgaem
{
    public class Player : Entity
    {
        private float scale = 2.0f;
        private float speed = 250;
        private float torque = 100f;
        private float hertz = 1.6f;
        private float dampingRatio = 1.5f;
        private float friction = 1.5f;
        private float density = 0.2f;
        private float gravityScale = 1.0f;

        private B2BodyId Chassis;
        private B2BodyId FrontWheel;
        private B2BodyId BackWheel;
        private B2JointId frontwheelJointId;
        private B2JointId backwheelJointId;

        public Player(Vector2 position)
        {
            B2Vec2[] vertices = new B2Vec2[6] { new B2Vec2(-1.5f, -0.5f), new B2Vec2(1.5f, -0.5f), new B2Vec2(1.5f, 0.0f), new B2Vec2(0.0f, 0.9f), new B2Vec2(-1.15f, 0.9f), new B2Vec2(-1.5f, 0.2f) };

            for (int i = 0; i < 6; ++i)
            {
                vertices[i].X *= 0.85f * scale;
                vertices[i].Y *= 0.85f * scale;
            }

            B2Hull hull = B2Hulls.b2ComputeHull(vertices, 6);
            B2Polygon chassis = B2Geometries.b2MakePolygon(hull, 0.15f * scale);

            B2ShapeDef shapeDef = B2Types.b2DefaultShapeDef();
            shapeDef.density = density / scale;
            shapeDef.material.friction = 0.2f;

            B2Circle circle = new B2Circle(new B2Vec2(0f, 0f), 0.4f * scale);

            B2BodyDef bodyDef = B2Types.b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f + position.X, -1.0f * scale + position.Y);
            Chassis = B2Bodies.b2CreateBody(Game.WorldId, bodyDef);
            B2Shapes.b2CreatePolygonShape(Chassis, shapeDef, chassis);

            shapeDef.density = 2.0f / scale;
            shapeDef.material.friction = friction;
            shapeDef.material.rollingResistance = 0.1f;

            bodyDef.position = new B2Vec2(-1.0f * scale + position.X, -0.35f * scale + position.Y);
            bodyDef.allowFastRotation = true;
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
        }

        public override void Update()
        {
            base.Update();

            float coef = 0.0f;
            B2WheelJoints.b2WheelJoint_EnableMotor(frontwheelJointId, false);
            B2WheelJoints.b2WheelJoint_EnableMotor(backwheelJointId, false);
            if (Game.Instance.Input.Keyboard.Down(Foster.Framework.Keys.A))
            {
                coef = -1f;
                B2WheelJoints.b2WheelJoint_EnableMotor(frontwheelJointId, true);
                B2WheelJoints.b2WheelJoint_SetMotorSpeed(frontwheelJointId, -speed);
            }
            if (Game.Instance.Input.Keyboard.Down(Foster.Framework.Keys.D))
            {
                coef = 1.0f;
                B2WheelJoints.b2WheelJoint_EnableMotor(backwheelJointId, true);
                B2WheelJoints.b2WheelJoint_SetMotorSpeed(backwheelJointId, speed);
            }



            B2Joints.b2Joint_WakeBodies(backwheelJointId);

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
            ImGui.End();
        }

        public override void Render()
        {
            base.Render();

            Utils.RenderB2Body(Chassis);
            Utils.RenderB2Body(FrontWheel);
            Utils.RenderB2Body(BackWheel);
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
}
