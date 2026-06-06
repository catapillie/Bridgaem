namespace Bridgaem
{
    public abstract class Entity
    {
        public Entity()
        { }

        public virtual void Update() { }

        public virtual void Render() { }

        public virtual void Destroy() { }
    }
}
