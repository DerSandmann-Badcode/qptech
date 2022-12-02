using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.API.Client;

namespace qptech.src.Electricity
{
    class BlockMiningRig:ElectricalBlock
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel.Position;


            BEMiningRig besc = world.BlockAccessor.GetBlockEntity(pos) as BEMiningRig;

            if (besc != null)
            {
                if (besc.StructureComplete) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
                besc.Interact(byPlayer);
                (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return true;
            }
           

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
