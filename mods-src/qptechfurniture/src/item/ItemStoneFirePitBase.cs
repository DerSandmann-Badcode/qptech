﻿using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;


namespace QptechFurniture.src
{
    public class ItemStoneFirePitBase: Item
    {

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity?.World == null || !byEntity.Controls.Sneak) return;

            IWorldAccessor world = byEntity.World;
            Block stonefirepitBlock = world.GetBlock(new AssetLocation("stonefirepit-construct1"));
            if (stonefirepitBlock == null) return;


            BlockPos onPos = blockSel.DidOffset ? blockSel.Position : blockSel.Position.AddCopy(blockSel.Face);

            IPlayer byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
            if (!byEntity.World.Claims.TryAccess(byPlayer, onPos, EnumBlockAccessFlags.BuildOrBreak))
            {
                return;
            }



            Block block = world.BlockAccessor.GetBlock(onPos.DownCopy());
            Block atBlock = world.BlockAccessor.GetBlock(onPos);

            string useless = "";

            if (!block.CanAttachBlockAt(byEntity.World.BlockAccessor, stonefirepitBlock, onPos.DownCopy(), BlockFacing.UP)) return;
            if (!stonefirepitBlock.CanPlaceBlock(world, byPlayer, new BlockSelection() { Position = onPos, Face = BlockFacing.UP }, ref useless)) return;

            world.BlockAccessor.SetBlock(stonefirepitBlock.BlockId, onPos);

            if (stonefirepitBlock.Sounds != null) world.PlaySoundAt(stonefirepitBlock.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);

            itemslot.Itemstack.StackSize--;
            handHandling = EnumHandHandling.PreventDefaultAction;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[] {
                new WorldInteraction()
                {
                    HotKeyCode = "sneak",
                    ActionLangCode = "heldhelp-createfirepit",
                    MouseButton = EnumMouseButton.Right,
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }

    }
}
