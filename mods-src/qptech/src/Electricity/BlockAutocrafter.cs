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
    class BlockAutocrafter:ElectricalBlock
    {
        
        
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            BEEAutocrafter cf = world.BlockAccessor.GetBlockEntity(pos) as BEEAutocrafter;
            if (cf == null)
            {
                return base.GetPlacedBlockName(world, pos);
            }
            if (cf.CurrentRecipes != null)
            {
                return "Autocrafter (" + cf.CurrentRecipes[0].Output.ResolvedItemstack.GetName() + ")";
            }
            else
            {
                return "Autocrafter (No Recipe)";
            }
            
        }
        
    }
}
