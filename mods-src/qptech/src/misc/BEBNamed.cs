using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Client;


namespace qptech.src.misc
{
    class BEBNamed:BlockEntityBehavior
    {
        BlockEntity be;
        string rename;
        public virtual string Rename => rename;
        public BEBNamed(BlockEntity be) : base(be)
        {
            this.be = be;

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("rename", rename);
            base.ToTreeAttributes(tree);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            rename = tree.GetString("rename");
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (rename != "") {
                dsc.AppendLine(rename);
            }
            base.GetBlockInfo(forPlayer, dsc);
        }
    }
}
