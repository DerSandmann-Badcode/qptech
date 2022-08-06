using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Vintagestory.API.MathTools;

namespace qptech.src
{
    /// <summary>
    /// Set an Autosmith recipe by clicking with a relevant item
    /// </summary>
    class BlockAutosmith:ElectricalBlock
    {
        static Dictionary<string, string> variantlist;
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            //must sneak click
            if (!byPlayer.Entity.Controls.Sneak) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
            //must have a relevant item
            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

            BEEAutosmith machine = (BEEAutosmith)api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (machine == null) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
            if (stack==null) {
                machine.HaltProduction();                
            }
            else
            {
                
                machine.SetCurrentItem(stack.Collectible.Code.ToString());
                
            }
            return true;
        }
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            BEEAutosmith cf = world.BlockAccessor.GetBlockEntity(pos) as BEEAutosmith;
            if (cf == null)
            {
                return base.GetPlacedBlockName(world, pos);
            }
            if (cf.CurrentRecipe != null)
            {
                return "Autosmith (" + cf.CurrentRecipe.Output.ResolvedItemstack.GetName() + ")";
            }
            else
            {
                return "Autosmith (No Recipe)";
            }
            
        }
        
    }
}
