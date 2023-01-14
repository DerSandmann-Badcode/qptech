using System.Collections.Generic;
using Vintagestory.API.Common;

namespace RustAndRails.src
{
    public static class EntityCooldownStore
    {
        public static Dictionary<long, int> EntityMountingCooldown = new Dictionary<long, int>();

        public static void AddRidingCooldown(this EntityAgent entityAgent, int seconds)
        {
            if (!EntityMountingCooldown.ContainsKey(entityAgent.EntityId))
            {
                EntityMountingCooldown.Add(entityAgent.EntityId, seconds);
            }
        }

        public static bool HasRidingCooldown(this EntityAgent entityAgent)
        {
            if (!EntityMountingCooldown.ContainsKey(entityAgent.EntityId))
            {
                return false;
            }
            return EntityMountingCooldown[entityAgent.EntityId] > 0;
        }

        public static void TickEntityCooldown()
        {
            List<KeyValuePair<long, int>> tempList = new List<KeyValuePair<long, int>>(EntityMountingCooldown);
            foreach (KeyValuePair<long, int> kvp in tempList)
            {
                if (kvp.Value <= 0)
                {
                    EntityMountingCooldown.Remove(kvp.Key);
                }
                if (kvp.Value > 0)
                {
                    EntityMountingCooldown[kvp.Key] = kvp.Value - 1;
                }
            }
        }
    }
}
