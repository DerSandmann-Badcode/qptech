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
    
    class ItemDoorPart:Item
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
            Block placeblock = api.World.GetBlock(new AssetLocation("chiseltools:moveablechiseledblock"));
            BlockPos usepos = blockSel.Position.Copy().Offset(blockSel.Face);
            api.World.BlockAccessor.SetBlock(placeblock.BlockId, usepos);
            //consume a door part
            if (byPlayer?.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                ItemSlot s = byPlayer.InventoryManager.ActiveHotbarSlot;
                s.TakeOut(1);
                s.MarkDirty();
            }
            handling = EnumHandHandling.PreventDefaultAction;
        }
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
        }

    }
}
