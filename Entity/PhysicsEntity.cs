using Box2D.NET;
using Foster.Framework;
using System.Numerics;

namespace Bridgaem
{
    public class PhysicsEntity : Entity
    {
        public B2BodyId BodyId;

        private B2ShapeId[] shapes = { };

        public PhysicsEntity()
        {
            B2BodyDef bodyDef = B2Types.b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.rotation = new B2Rot(1.0f, 0.0f);
            BodyId = B2Bodies.b2CreateBody(Game.WorldId, bodyDef);
        }

        public B2ShapeId AddDefaultBox(float halfWidth, float halfHeight, float friction = 0.3f, float density = 1.0f)
        {
            B2ShapeDef shapeDef = B2Types.b2DefaultShapeDef();
            shapeDef.density = density;
            shapeDef.material.friction = friction;
            B2Polygon box = B2Geometries.b2MakeBox(halfWidth, halfHeight);
            return B2Shapes.b2CreatePolygonShape(BodyId, shapeDef, box);
        }

        public B2ShapeId AddDefaultCircle(float radius, float friction = 0.3f, float density = 1.0f)
        {
            B2ShapeDef shapeDef = B2Types.b2DefaultShapeDef();
            shapeDef.density = density;
            shapeDef.material.friction = friction;
            B2Circle circle = new B2Circle();
            circle.radius = radius;
            return B2Shapes.b2CreateCircleShape(BodyId, shapeDef, circle);
        }

        public void RenderBoxShape(B2ShapeId shapeId)
        {
            B2Polygon polygon = B2Shapes.b2Shape_GetPolygon(shapeId);


            object userdata = B2Shapes.b2Shape_GetUserData(shapeId);
            Color color = Color.Red;
            if (userdata is Color c)
                color = c;

            Game.Batch.RectLine(new Rect(polygon.vertices[0].X, polygon.vertices[0].Y, polygon.vertices[2].X - polygon.vertices[0].X, polygon.vertices[2].Y - polygon.vertices[0].Y), 0.2f, color);
        }

        public void RenderCircleShape(B2ShapeId shapeId)
        {
            B2Circle circle = B2Shapes.b2Shape_GetCircle(shapeId);

            object userdata = B2Shapes.b2Shape_GetUserData(shapeId);
            Color color = Color.Red;
            if (userdata is Color c)
                color = c;

            Game.Batch.Circle(new Circle(0, 0, circle.radius), 300, color);
        }

        public override void Render()
        {
            base.Render();

            int shapeCount = B2Bodies.b2Body_GetShapeCount(BodyId);
            if (shapeCount > shapes.Length)
                shapes = new B2ShapeId[shapeCount];

            B2Bodies.b2Body_GetShapes(BodyId, shapes, shapeCount);

            B2Vec2 bodyPos = B2Bodies.b2Body_GetPosition(BodyId);
            B2Rot bodyRot = B2Bodies.b2Body_GetRotation(BodyId);
            float bodyAngle = float.Atan2(bodyRot.s, bodyRot.c);
            Game.Batch.PushMatrix(new(bodyPos.X, bodyPos.Y), Vector2.One, bodyAngle);

            foreach (B2ShapeId shapeId in shapes)
            {
                B2ShapeType type = B2Shapes.b2Shape_GetType(shapeId);
                switch (type)
                {
                    case B2ShapeType.b2_polygonShape:
                        RenderBoxShape(shapeId);
                        break;
                    case B2ShapeType.b2_circleShape:
                        RenderCircleShape(shapeId);
                        break;
                    default:
                        throw new Exception("Drawing of shape of type {type} is not yet supported");
                }
            }

            Game.Batch.PopMatrix();
        }

        public override void Destroy()
        {
            base.Destroy();
            B2Bodies.b2DestroyBody(BodyId);
        }
    }
}
