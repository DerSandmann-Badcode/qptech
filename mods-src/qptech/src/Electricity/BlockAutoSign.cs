using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Vintagestory.API.MathTools;

namespace qptech.src
{
    /// <summary>
    /// Set an Autosmith recipe by clicking with a relevant item
    /// </summary>
    class BlockAutoSign:BlockSign
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntity entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);

            if (entity is BlockEntitySign)
            {
                BEAutoSign besigh = (BEAutoSign)entity;
                besigh.OnRightClick(byPlayer);
                return true;
            }

            return true;
        }


    }
}
