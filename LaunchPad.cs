using Box2D.NET;
using Bridgaem.Utility;
using Foster.Framework;
using System.Numerics;

namespace Bridgaem
{
    public class LaunchPad : PhysicsEntity
    {
        Vector2 originalPos;
        B2ShapeId shape;
        private float displacement = 5f;
        private float time = 0.3f;

        B2ContactData[] contacts;


        bool moving = false;
        bool forward = true;
        private float currentTime = 0f;

        public LaunchPad(Vector2 pos, float w, float h)
        {
            originalPos = pos;
            B2Bodies.b2Body_SetTransform(BodyId, new B2Vec2(pos.X, pos.Y), B2MathFunction.b2MakeRot(0f));
            B2Bodies.b2Body_SetType(BodyId, B2BodyType.b2_kinematicBody);
            shape = AddDefaultBox(w / 2, h / 2, friction: 0.8f);
            B2Shapes.b2Shape_EnableContactEvents(shape, true);

            contacts = new B2ContactData[256];
        }

        public override void Update()
        {
            base.Update();

            if (!moving)
            {
                int contactCount = B2Bodies.b2Body_GetContactData(BodyId, contacts, contacts.Length);
                B2ShapeId[] s = new B2ShapeId[1];
                B2Bodies.b2Body_GetShapes(Game.Instance.Player.Chassis, s, 1);
                B2ShapeId[] playerShapes = new B2ShapeId[3];
                playerShapes[0] = s[0];
                B2Bodies.b2Body_GetShapes(Game.Instance.Player.FrontWheel, s, 1);
                playerShapes[1] = s[0];
                B2Bodies.b2Body_GetShapes(Game.Instance.Player.BackWheel, s, 1);
                playerShapes[2] = s[0];

                for (int i = 0; i < contactCount; i++)
                {
                    B2ContactData c = contacts[i];

                    foreach (B2ShapeId pS in playerShapes)
                    {
                        if (c.shapeIdB.index1 == pS.index1 || c.shapeIdA.index1 == pS.index1)
                        {
                            moving = true;
                            forward = true;
                            currentTime = 0;
                            goto gotoLabel;
                        }
                    }
                }

            gotoLabel: { }

            }

            B2Bodies.b2Body_SetLinearVelocity(BodyId, new B2Vec2(0, 0));


            if (moving)
            {
                currentTime += Game.Dt;
                Vector2 target;
                if (forward)
                    target = -Vector2.UnitY * Ease.Cube.In(currentTime / time) * displacement + originalPos;
                else
                    target = -Vector2.UnitY * (1 - Ease.Cube.In(currentTime / (time * 3))) * displacement + originalPos;

                Vector2 currentPos = B2Bodies.b2Body_GetPosition(BodyId).ToVector2();
                B2Bodies.b2Body_SetLinearVelocity(BodyId, ((target - currentPos) / Game.Dt).ToB2V2());

                if ((forward && currentTime > time) || (!forward && currentTime > 3 * time))
                {
                    if (forward) forward = false;
                    else moving = false;
                    currentTime = 0;
                }
            }

        }
    }
}
