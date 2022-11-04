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
            if (!CanMakeLadder(byPlayer)) { return; }
            //TODO Add Survival Mode damage and ladder draw
            BlockChisel bc = api.World.BlockAccessor.GetBlock(blockSel.Position) as BlockChisel;
            if (bc == null) { return; }
            BlockEntityMicroBlock bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            //First Copy the original block
            string copiedname = bmb.BlockName;
            List<uint> copiedblockvoxels = new List<uint>(bmb.VoxelCuboids);
            List<int> copiedblockmaterials = new List<int>(bmb.MaterialIds);
            int copiedvolume = (int)(bmb.VolumeRel * 16f * 16f * 16f);
            //Then Create our new ladder block
            AssetLocation al = new AssetLocation("chiseltools:climbablechiseledblock");
            Block nb = api.World.GetBlock(al);
            if (bc.BlockId == nb.BlockId) { return; }
            //Voxel count check 
            bool voxelcountok = false;
            if (ChiselToolLoader.serverconfig.minimumVoxelsForLadder > 0)
            {
                int voxelcount = 0;
                //Count voxels to make sure there are enough for ladder
                foreach (uint su in copiedblockvoxels)
                {
                    CuboidWithMaterial cwm = new CuboidWithMaterial();
                    BlockEntityMicroBlock.FromUint(su, cwm);
                    
                    //cycle through each voxel of the source cuboid and see if it's safe to write to the destination block
                    voxelcount += cwm.Volume;
                    if (voxelcount>= ChiselToolLoader.serverconfig.minimumVoxelsForLadder)
                    {
                        voxelcountok = true;
                        break;
                    }

                }
                if (!voxelcountok) { return; }
            }
            api.World.BlockAccessor.SetBlock(nb.BlockId, blockSel.Position);
            bmb = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
            bmb.BlockName = copiedname + "[Ladder]";
            //Then Paste shape and material data into new block
            bmb.MaterialIds = copiedblockmaterials.ToArray();
            bmb.VoxelCuboids = new List<uint>(copiedblockvoxels);
            bmb.MarkDirty(true);
            
            if (api is ICoreServerAPI && byPlayer?.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                int dmg = 1;
                this.DamageItem(api.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, dmg);
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }
            api.World.PlaySoundAt(new AssetLocation("sounds/stone_move"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 12, 1);
        }

        public virtual bool CanMakeLadder(IPlayer player)
        {
            return true;
        }
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
            return;
            
        }

        


    }
}
