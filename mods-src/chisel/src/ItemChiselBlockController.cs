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
    
    class ItemChiselBlockController:Item
    {
        //TODO - add tool wear in survival
        //  - add paintbrush graphic and sound effect
        //  - add check to not use paintbrush if materials match
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.PreventDefaultAction;
            if (blockSel == null) { return; }
            BEFunctionChiseled controllable = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFunctionChiseled;
            if (controllable == null) { return; }
            //Do nothing if we don't have access to selected block
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            if (byPlayer.Entity.Controls.Sneak)
            {
                controllable.SetState("OPEN") ;
                
            }
            else
            {
                controllable.SetState("CLOSED");
                
            }
            
            
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
            if (blockSel == null) { return; }
            BEFunctionChiseled mover = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFunctionChiseled;
            if (mover == null) { return; }
            
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
           
        }

        protected virtual int CalcDamage()
        {

            return 0;
        }

        
    }
}
