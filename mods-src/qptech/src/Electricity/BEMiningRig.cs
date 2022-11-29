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
        bool surveyed = false;
        Vec3d dropoffset => new Vec3d(0, 6, 0);
        public virtual int skip => 5;
        public virtual int range => 1;
        ProPickWorkSpace ppws;
        
        Dictionary<string, double> survey;
        Random roll;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            survey = new Dictionary<string, double>();
            roll = new Random();
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


                if (skipcounter < skip) { skipcounter++; return; }
                if (!surveyed) { DoSurvey(Pos); }
                else if (survey.Count == 0) { return; }
                
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
                    DoDrops(b);
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

        protected virtual void DoDrops(Block b)
        {
            //handle dirt
            if (b.Code.ToString().Contains("soil")|| b.Code.ToString().Contains("mud")|| b.Code.ToString().Contains("dirt"))
            {
                return; //for now we just skip dirt
            }
            //handle stone
            //handle tailings?
            //handle ore
            if (survey == null || survey.Count == 0) { return; }
            double diceroll = roll.NextDouble();
            //first make a list of anything that qualifies this dice roll
            List<string> potentialores = new List<string>();
            foreach (string key in survey.Keys)
            {
                if (diceroll < survey[key]) { potentialores.Add(key); }
            }
            //nothing was found
            if (potentialores.Count == 0) { return; }
            //pick something:
            int selectroll = roll.Next(0, potentialores.Count);
            string chosen = potentialores[selectroll];
            chosen = "nugget-" + chosen;
            AssetLocation al = new AssetLocation("game:" + chosen);
            Item drop = Api.World.GetItem(al);
            if (drop == null)
            {
                return;
            }
            DummyInventory di = new DummyInventory(Api, 1);
            di[0].Itemstack = new ItemStack(drop, 1);
            di.DropAll(Pos.ToVec3d()+dropoffset);
        }

        protected virtual void DoSurvey( BlockPos pos)
        {
            survey = new Dictionary<string, double>();
            surveyed = true;
            DepositVariant[] deposits = Api.ModLoader.GetModSystem<GenDeposits>()?.Deposits;
            if (deposits == null) return;

            IBlockAccessor blockAccess = Api.World.BlockAccessor;
            int chunksize = blockAccess.ChunkSize;
            int regsize = blockAccess.RegionSize;

            IMapRegion reg = Api.World.BlockAccessor.GetMapRegion(pos.X / regsize, pos.Z / regsize);
            int lx = pos.X % regsize;
            int lz = pos.Z % regsize;

            pos = pos.Copy();
            pos.Y = Api.World.BlockAccessor.GetTerrainMapheightAt(pos);

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
                    if (survey.ContainsKey(val.Key)) { survey[val.Key] += totalFactor; }
                    else { survey.Add(val.Key, totalFactor); }
                    
                }

            }
            
            MarkDirty(true);
            
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            
        }
    }
}
