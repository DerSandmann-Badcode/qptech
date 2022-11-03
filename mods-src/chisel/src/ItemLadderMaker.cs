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
    
    class ItemLadderMaker:Item
    {
        //TODO - add tool wear in survival
        //  - add paintbrush graphic and sound effect
        //  - add check to not use paintbrush if materials match
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.PreventDefaultAction;
            if (blockSel == null) { return; }
            
            //Do nothing if we don't have access to selected block
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            //TODO Add Survival Mode damage and ladder draw
            BlockChisel bc = api.World.BlockAccessor.GetBlock(blockSel.Position) as BlockChisel;
            if (bc == null) { return; }
            AssetLocation al = new AssetLocation("chiseltools:climbablechiseledblock");
            Block nb = api.World.GetBlock(al);
            if (bc.BlockId == nb.BlockId) { return; }
            api.World.BlockAccessor.ExchangeBlock(nb.Id, blockSel.Position);
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
            return;
            
        }

        protected virtual int CalcDamage()
        {

            return 0;
        }

        
    }
}
