using Box2D.NET;
using ImGuiNET;
using System.Numerics;

namespace Bridgaem
{
    public class Fan : PhysicsEntity
    {
        private B2AABB windBox;
        private B2Vec2 windDir;
        private float windSpeed = 15f;
        public Fan(Vector2 pos, float w, float h, float range)
        {
            B2Bodies.b2Body_SetTransform(BodyId, new B2Vec2(pos.X, pos.Y), B2MathFunction.b2MakeRot(0f));
            B2Bodies.b2Body_SetType(BodyId, B2BodyType.b2_kinematicBody);
            AddDefaultBox(w / 2, h / 2, friction: 0.8f);

            windBox = new B2AABB(new B2Vec2(pos.X - w / 2, pos.Y - range), new B2Vec2(pos.X + w / 2, pos.Y));
            windDir = new B2Vec2(0f, -1);
        }

        public override void Update()
        {
            base.Update();

            //Find all b2shapes that are in front of the fan (so if it's rotation is 0, everything overlapping above it, if rotation is pi / 2 then to its left etc), then apply wind to them

            int i = 0;
            B2TreeStats result = B2Worlds.b2World_OverlapAABB(Game.WorldId, windBox, new B2QueryFilter(0xFFFFFFFF, 0xFFFFFFFF), (shapeid, context) =>
            {
                B2Shapes.b2Shape_ApplyWind(shapeid, windDir, 1f, 1f, true);
                return ++i > 64;
            }, null);

            ImGui.Begin("Hello");
            ImGui.SliderFloat("windSpeed", ref windSpeed, 1, 30);
            windDir = new B2Vec2(0f, -windSpeed);
            ImGui.End();
        }
    }
}
