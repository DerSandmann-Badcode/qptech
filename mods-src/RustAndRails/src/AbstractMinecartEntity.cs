namespace RustAndRails.src
{
    public abstract class AbstractMinecartEntity : MountableEntity
    {
        public abstract void OnActivatorRail(int x, int y, int z, bool powered);
    }
}