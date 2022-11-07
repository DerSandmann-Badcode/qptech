using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;

namespace chisel.src
{
    class BlockFunctionalChiseled:BlockChisel
    {
        
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            
            BlockFunctionalChiseled bfc = blockAccessor.GetBlock(pos) as BlockFunctionalChiseled;
            BEFunctionChiseled befcc=blockAccessor.GetBlockEntity(pos) as BEFunctionChiseled;
            
            if (bfc == null || befcc == null|| !befcc.Passable) { return base.GetCollisionBoxes(blockAccessor, pos); }
            
            return null;
            //return base.GetCollisionBoxes(blockAccessor, pos);
        }
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Cuboidf[] cube = { new Cuboidf(0, 0, 0, 1, 1, 1) };

            return cube;
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }
            BEFunctionChiseled befcc = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFunctionChiseled;
            if (befcc != null)
            {
                befcc.ToggleOpenClosed();
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        public override bool AllowSnowCoverage(IWorldAccessor world, BlockPos blockPos)
        {
            return false;
        }
        public override float GetSnowLevel(BlockPos pos)
        {
            return 0;
        }
    }
}
