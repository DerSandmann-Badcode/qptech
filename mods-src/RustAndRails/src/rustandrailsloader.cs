using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace RustAndRails.src
{
    class rustandrailsloader : ModSystem
    {
        public static Configuration Config = new Configuration();

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Log.Logger = api.Logger;
            api.RegisterBlockClass("BlockSignalSwitch", typeof(BlockSignalSwitch));
            api.RegisterBlockClass("BlockDetectorRail", typeof(BlockDetectorRail));
            api.RegisterBlockClass("BlockRail", typeof(BlockRail));
            api.RegisterBlockClass("BlockActivatorRail", typeof(BlockActivatorRail));
            api.RegisterEntity("MinecartEntity", typeof(MinecartEntity));

            api.RegisterMountable("MinecartEntity", MountableEntity.GetMountable);

        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            ModNetwork.Client = new ClientChannel(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            ModNetwork.Server = new ServerChannel(api);
            api.Event.Timer(OnInterval, 1);
        }

        private void OnInterval()
        {
            EntityCooldownStore.TickEntityCooldown();
        }
    }

    public class Configuration
    {
        public double MaximumInteractionBlockDistance = 3;
        public string[] CarryableEntities = { "game:sheep-bighorn-male", "game:sheep-bighorn-lamb", "game:sheep-bighorn-female", "game:hare-baby", "game:hare-female", "game:hare-male" };
    }
}
