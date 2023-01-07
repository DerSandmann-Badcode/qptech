using Vintagestory.API.Common.Entities;

namespace RustAndRails.src
{
    public static class EntityPosExtensions
    {
        public static EntityPos SetRoll(this EntityPos entityPos, float roll)
        {
            return entityPos.SetAngles(roll, entityPos.Yaw, entityPos.Pitch);
        }
    }
}
