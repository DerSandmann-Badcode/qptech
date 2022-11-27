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
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using HarmonyLib;


namespace qptech.src.Electricity
{
    class BEMiningRig:BEElectric
    {
        BlockPos drillpos;
        public virtual int drillstartyoffset=>-1;
        int skipcounter;
        public virtual int skip => 5;
        public virtual int range => 1;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
        }

        public override void OnTick(float par)
        {
            base.OnTick(par);
            if (Api is ICoreServerAPI)
            {
                if (skipcounter < skip) { skipcounter++;return; }
                skipcounter = 0;
                if (drillpos == null) { drillpos=StartDrillPos;MarkDirty(true); }

                if (drillpos.Y <= 10) { return; }
                Block b = Api.World.BlockAccessor.GetBlock(drillpos);
                if (b.BlockId == 0)
                {
                    skipcounter = skip;

                }
                else
                {
                    Api.World.BlockAccessor.SetBlock(0, drillpos);
                }
                drillpos.X++;
                if ((drillpos.X - Pos.X) > range)
                {
                    drillpos.X = Pos.X - range;
                    drillpos.Z++;
                    if (drillpos.Z - Pos.Z > range)
                    {
                        drillpos.Y--;
                        drillpos.Z = Pos.Z - range;
                    }
                }
                
                MarkDirty(true);
            }
        }

        BlockPos StartDrillPos=>new BlockPos(Pos.X-range, Pos.Y + drillstartyoffset, Pos.Z-range);
            

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (drillpos == null) { drillpos=StartDrillPos; }
            tree.SetBlockPos("drillpos",drillpos);
            tree.SetInt("skipcounter", skipcounter);
            base.ToTreeAttributes(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            drillpos = tree.GetBlockPos("drillpos",StartDrillPos);
            skipcounter = tree.GetInt("skipcounter");
        }
    }
}
