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
    
    class ItemBlockSwapper:Item
    {
        //TODO - add tool wear in survival
        //  - add paintbrush graphic and sound effect
        //  - add check to not use paintbrush if materials match
        const int inkslotnumber = 0;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (blockSel == null) { return; }
            
            //Do nothing if we don't have access to selected block
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }
            /*
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            if (bmb == null)
            {
                TryChangeBlockToChisel(blockSel, byEntity, byPlayer);
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            */
            //do nothing if we don't have a valid dye/ink block in slot 0
            if (byPlayer.InventoryManager.ActiveHotbarSlotNumber == inkslotnumber) { return; }

            
            ItemSlot inkslot = byPlayer.InventoryManager.GetHotbarInventory()[inkslotnumber];
            
            if (inkslot==null|| inkslot.Empty||inkslot.Itemstack.Block==null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            BlockEntity tbe = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            Block tb = api.World.BlockAccessor.GetBlock(blockSel.Position);
            //HERE I COULD PUT A CHECK AGAINST BLOCK ENTITES TO PREVENT MAJOR DAMAGE BY ACCIDENT?
            //TO DO SET A MODE WHETHER THE OBJECT SHOULD DROP OR NOT?
            api.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer); 
            api.World.BlockAccessor.SetBlock(inkslot.Itemstack.Block.BlockId, blockSel.Position,inkslot.Itemstack);

            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
            
        }

        protected virtual int CalcDamage()
        {

            return 0;
        }

        
    }
}
