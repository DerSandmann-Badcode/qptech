using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace RustAndRails.src
{
    class rustandrailsloader : ModSystem
    {
        public static rustandrailsloader loader;
		public static Configuration Config = new Configuration();

		public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("BlockSignalSwitch", typeof(BlockSignalSwitch));
            api.RegisterBlockClass("BlockDetectorRail", typeof(BlockDetectorRail));
            api.RegisterBlockClass("BlockRail", typeof(BlockRail));
            api.RegisterEntity("MinecartEntity", typeof(MinecartEntity));
			api.RegisterMountable(typeof(EntitySeat).Name, EntitySeat.GetMountable);
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
			ModNetwork.Client = new ClientChannel(api);
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
			ModNetwork.Server = new ServerChannel(api);
		}
	}

	public class Configuration
	{
		public double MaximumInteractionBlockDistance = 3;
	}
}
