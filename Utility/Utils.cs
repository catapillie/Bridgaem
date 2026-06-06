using Box2D.NET;
using System.Numerics;

namespace Bridgaem.Utility
{
    public static class Utils
    {
        public static B2Vec2 ToB2V2(this Vector2 v)
            => new B2Vec2(v.X, v.Y);

        public static Vector2 ToVector2(this B2Vec2 v)
            => new Vector2(v.X, v.Y);
    }
}
