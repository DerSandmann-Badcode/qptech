﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace qptech.src
{
    class ElectricalBlock : BlockMPBase
    {
        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {

        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            return false;
        }

        //Toggle power if player is holding a screwdriver or club
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            var bee = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEElectric;
            if (bee==null) return base.OnBlockInteractStart(world, byPlayer, blockSel); 
            if (byPlayer.Entity.RightHandItemSlot.Itemstack==null) return base.OnBlockInteractStart(world, byPlayer, blockSel);
            if (byPlayer.Entity.RightHandItemSlot.Itemstack.Item == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);
            string fcp = byPlayer.Entity.RightHandItemSlot.Itemstack.Item.CodeWithoutParts(1);
            if ((fcp.Contains("screwdriver")&&!fcp.Contains("head"))||fcp.Contains("woodenclub"))
            {
                bee.TogglePower();
                
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            BEElectric bee = world.BlockAccessor.GetBlockEntity(pos) as BEElectric;
            if (bee != null)
            {
                bee.FindConnections();
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }
    }
}
