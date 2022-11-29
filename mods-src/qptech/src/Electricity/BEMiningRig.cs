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
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
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
        ProPickWorkSpace ppws;
        bool temp = false;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            ppws = ObjectCacheUtil.GetOrCreate<ProPickWorkSpace>(api, "propickworkspace", () =>
            {
                ProPickWorkSpace ppws = new ProPickWorkSpace();
                ppws.OnLoaded(api);
                return ppws;
            });
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
                if (!temp) { DoSurvey(Api.World, Pos); }
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

        protected virtual void DoSurvey( IWorldAccessor world,  BlockPos pos)
        {
            temp = true;
            
            
            DepositVariant[] deposits = Api.ModLoader.GetModSystem<GenDeposits>()?.Deposits;
            if (deposits == null) return;

            IBlockAccessor blockAccess = world.BlockAccessor;
            int chunksize = blockAccess.ChunkSize;
            int regsize = blockAccess.RegionSize;

            IMapRegion reg = world.BlockAccessor.GetMapRegion(pos.X / regsize, pos.Z / regsize);
            int lx = pos.X % regsize;
            int lz = pos.Z % regsize;

            pos = pos.Copy();
            pos.Y = world.BlockAccessor.GetTerrainMapheightAt(pos);

            int[] blockColumn = ppws.GetRockColumn(pos.X, pos.Z);

            List<KeyValuePair<double, string>> readouts = new List<KeyValuePair<double, string>>();

            List<string> traceamounts = new List<string>();

            foreach (var val in reg.OreMaps)
            {
                IntDataMap2D map = val.Value;
                int noiseSize = map.InnerSize;

                float posXInRegionOre = (float)lx / regsize * noiseSize;
                float posZInRegionOre = (float)lz / regsize * noiseSize;

                int oreDist = map.GetUnpaddedColorLerped(posXInRegionOre, posZInRegionOre);

                double ppt;
                double totalFactor;

                if (!ppws.depositsByCode.ContainsKey(val.Key))
                {
                    
                    continue;
                }

                ppws.depositsByCode[val.Key].GetPropickReading(pos, oreDist, blockColumn, out ppt, out totalFactor);
                
                if (totalFactor > 0.002)
                {
                    readouts.Add(new KeyValuePair<double, string>(totalFactor, val.Key));
                }
            }
            bool x = true;
            
        }


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
