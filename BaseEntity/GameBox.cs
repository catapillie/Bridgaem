using Box2D.NET;
using System.Numerics;

namespace Bridgaem.BaseEntity
{
    public class GameBox : PhysicsEntity
    {
        public GameBox(Vector2 pos, float w, float h, float rotation, B2BodyType bodyType = B2BodyType.b2_dynamicBody)
        {
            B2Bodies.b2Body_SetTransform(BodyId, new B2Vec2(pos.X, pos.Y), B2MathFunction.b2MakeRot(rotation));
            B2Bodies.b2Body_SetType(BodyId, bodyType);
            AddDefaultBox(w / 2, h / 2);
        }
    }
}
