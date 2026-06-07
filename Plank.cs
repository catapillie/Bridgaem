using Box2D.NET;
using Bridgaem.Utility;
using Foster.Framework;
using System.Numerics;

namespace Bridgaem
{
    public class Plank : PhysicsEntity
    {
        private Vector2[] positions;
        private float angularVelocity;
        private float movementTime;

        private int currentPosIndex = 0;
        private float currentTime = 0f;

        public Plank(Vector2[] pos, float w, float h, float rotation, float angularVelocity = 0, float timeBetweenMovements = 1)
        {
            positions = pos;
            this.angularVelocity = angularVelocity;
            movementTime = timeBetweenMovements;

            B2Bodies.b2Body_SetTransform(BodyId, new B2Vec2(pos[0].X, pos[0].Y), B2MathFunction.b2MakeRot(rotation));
            B2Bodies.b2Body_SetType(BodyId, B2BodyType.b2_kinematicBody);
            AddDefaultBox(w / 2, h / 2, friction: 0.8f);
        }

        public override void Update()
        {
            base.Update();

            B2Bodies.b2Body_SetAngularVelocity(BodyId, angularVelocity);

            if (positions.Length == 1)
                return;

            if (currentTime > movementTime)
            {
                B2Bodies.b2Body_SetTransform(BodyId, positions[(currentPosIndex + 1) % positions.Length].ToB2V2(), B2Bodies.b2Body_GetRotation(BodyId));
                currentPosIndex = (currentPosIndex + 1) % positions.Length;
                currentTime = 0f;
            }

            Vector2 position = B2Bodies.b2Body_GetPosition(BodyId).ToVector2();
            Vector2 target = Ease.Quad.InOut(currentTime / movementTime) * (positions[(currentPosIndex + 1) % positions.Length] - positions[currentPosIndex]) + positions[currentPosIndex];
            currentTime += Game.Dt;
            B2Bodies.b2Body_SetLinearVelocity(BodyId, ((target - position) / Game.Dt).ToB2V2());
        }
    }
}
