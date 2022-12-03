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
        BlockPos drillpos; //records the position being drilled
        public virtual int drillstartyoffset=>-3; //how low to start drilling
        BlockPos StartDrillPos => new BlockPos(Pos.X - range, Pos.Y + drillstartyoffset, Pos.Z - range);
        int skipcounter;
        bool surveyed = false;
        Vec3i outputcontaineroffset => new Vec3i(0, 1, 0); //where to create items
        public virtual int skip => 20; //how many onticks to skip between block breaking
        public virtual int range => 2;//how far out to mine (2=5x5 shaft)
        public virtual int maxore => 10;
        ProPickWorkSpace ppws; //used to fine ore densities
        protected const int clientplaysound = 999900001; //packet id for sound playing
        Dictionary<string, double> survey; //list of available minerals and their percentages
        Random roll;
        MultiblockStructure ms;
        public BlockPos mboffset;
        bool structurecomplete = false;
        bool nostorage = false;
        bool showingcontents = false;
        public bool StructureComplete => structurecomplete;
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
            ms = Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
            int[] offsarray = { 0, 0, 0 };
            offsarray = Block.Attributes["mboffset"].AsArray<int>(offsarray);
            mboffset = new BlockPos(offsarray[0] + Pos.X, offsarray[1] + Pos.Y, offsarray[2] + Pos.Z);

            ms.InitForUse(0);
        }

        public override void OnTick(float par)
        {
            base.OnTick(par);
            structurecomplete = CheckCompleteStructure();
            nostorage = false;
            if (Api is ICoreServerAPI)
            {
                
                if (!structurecomplete) { return; }
                if (!IsPowered) { return; }
                if (skipcounter < skip) { skipcounter++; return; }
                if (!surveyed) { DoSurvey(Pos); }
                else if (survey.Count == 0) { return; }
                DoRunningParticles();
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
                    //stop operating if nowhere to put generated material
                    bool trydrop=DoDrops(b,drillpos);
                    if (!trydrop)
                    {
                        nostorage = true;
                        TogglePower();
                        MarkDirty(true);
                        return;
                    }
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

        
        bool CheckCompleteStructure()
        {
            if (ms == null) { return false; }
            if (ms.InCompleteBlockCount(Api.World, mboffset) > 0)
            {

                return false;
            }

            return true;
        }
        protected virtual bool DoDrops(Block b, BlockPos drillpos)
        {
            BlockEntity beoutput = Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(outputcontaineroffset));
            if (beoutput == null) { return false; }
            IBlockEntityContainer outcontainer = beoutput as IBlockEntityContainer;
            if (outcontainer == null) { return false; }
            
            bool dooredrop = true; //should we calculate an ore drop?
            //handle dirt
            if (b.Code.ToString().Contains("soil")|| b.Code.ToString().Contains("mud")|| b.Code.ToString().Contains("dirt"))
            {
                dooredrop = false;
                
            }
            
            List<ItemStack>drops= new List<ItemStack>();
            
            if (b.Drops != null)
            {
                foreach (BlockDropItemStack bd in b.Drops)
                {
                    ItemStack newstack= bd.ResolvedItemstack;
                    if (newstack != null && newstack.StackSize > 0)
                    {
                        drops.Add(newstack.Clone());
                        if (bd.LastDrop) { break; }
                    }
                }
            }
            
            
            
            //handle tailings?
            //handle ore
            if (survey == null || survey.Count == 0) { return true; }
            double diceroll = roll.NextDouble();
            //first make a list of anything that qualifies this dice roll
            List<string> potentialores = new List<string>();
            foreach (string key in survey.Keys)
            {
                if (diceroll < survey[key]) { potentialores.Add(key); }
            }
            //nothing was found
            if (potentialores.Count == 0) { return true; }
            //pick something:
            int selectroll = roll.Next(0, potentialores.Count);
            string chosen = potentialores[selectroll];
            bool oredropok = true;
            AssetLocation al = new AssetLocation("game:" + "nugget-" + chosen);
            Item drop = Api.World.GetItem(al);
            if (drop == null)
            {
                al = new AssetLocation("game:" + "ore-" + chosen);
                drop = Api.World.GetItem(al);
                if (drop == null)
                {
                    oredropok = false;
                }
            }
            //Add ore to our dummy inventory if relevant
            if (oredropok)
            {
                drops.Add( new ItemStack(drop, 1 + roll.Next(0, maxore)));
            }
            //Add other block drops to our dummy inventory if relevant
            DummyInventory di = new DummyInventory(Api, drops.Count);
            for (int c = 0; c < drops.Count(); c++)
            {
                di[c].Itemstack = drops[c].Clone();
            }
            
            //di.DropAll(Pos.ToVec3d()+outputcontaineroffset);
            //if the container doesn't have enough slots than clearly it can't hold the output
            if (outcontainer.Inventory.Count() < di.Count()) { return false; }

            //pre check to see if we can store our pending inventory

            Dictionary<int,int> slotreservation = new Dictionary<int, int>(); //slot reservations (output,dummy inventory)
            bool foundroom = false;
            for (int dc = 0; dc < di.Count(); dc++)
            {
                foundroom = false;
                for (int outc = 0; outc < outcontainer.Inventory.Count(); outc++)
                {
                    if (slotreservation.ContainsKey(outc)) { continue; } //we already would use this slot
                    //if we this output slot can hold our item, then reserve it and break
                    if (outcontainer.Inventory[outc].Empty)
                    {
                        foundroom = true;
                        slotreservation[outc]=dc;
                        break;
                    }
                    else if (outcontainer.Inventory[outc].Itemstack.Collectible!=di[dc].Itemstack.Collectible)
                    {
                        continue;
                    }
                    //we have match see if there's room
                    int spaceremaining = outcontainer.Inventory[outc].Itemstack.Collectible.MaxStackSize - outcontainer.Inventory[outc].StackSize;
                    //not enough room
                    if (spaceremaining < di[dc].StackSize) { continue; }
                    //enough room, reserve
                    slotreservation[outc]=dc;
                    foundroom = true;
                    break;
                }
                if (!foundroom) //if there's no room for this item the operation has failed
                {
                    break;
                }
            }
            //the output container isn't ready for our processing, cancel processing
            if (!foundroom) { return false; }
            //the output container is ready, load it up
            for (int dc = 0; dc < di.Count(); dc++)
            {
                foreach (int outc in slotreservation.Keys)
                {
                    if (slotreservation[outc] == dc)
                    {
                        int moved = di[dc].TryPutInto(Api.World, outcontainer.Inventory[outc], di[dc].StackSize);
                        outcontainer.Inventory[outc].MarkDirty();
                    }
                }
            }
            beoutput.MarkDirty(true);
            MarkDirty(true);
            //string sound="sounds/block/rock-break-pickaxe";
            
            
            return true;
        }
        private SimpleParticleProperties smokeParticles;
        protected virtual void DoRunningParticles()
        {

            smokeParticles = new SimpleParticleProperties(
                  1, 2,
                  ColorUtil.ToRgba(192, 32, 24, 10),
                  new Vec3d(),
                  new Vec3d(0.75, 0, 0.75),
                  new Vec3f(-1 / 32f, 0.1f, -1 / 32f),
                  new Vec3f(1 / 32f, 0.1f, 1 / 32f),
                  10f,
                  -0.025f / 4,
                  0.6f,
                  2f,
                  EnumParticleModel.Quad
              );

            smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.25f);
            smokeParticles.SelfPropelled = true;
            smokeParticles.AddPos.Set(8 / 16.0, 0, 8 / 16.0);
            smokeParticles.MinPos.Set(Pos.X + 4 / 16f, Pos.Y + 2, Pos.Z + 4 / 16f);
            Api.World.SpawnParticles(smokeParticles);
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

        public bool Interact(IPlayer byPlayer)
        {
            showingcontents = !showingcontents;
            
            structurecomplete = CheckCompleteStructure();
            if (Api.Side == EnumAppSide.Client)
            {
                if (showingcontents)
                {
                    ms.HighlightIncompleteParts(Api.World, byPlayer, mboffset);
                }
                else
                {
                    ms.ClearHighlights(Api.World, byPlayer);
                }
            }
            return true;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (drillpos == null) { drillpos=StartDrillPos; }
            tree.SetBlockPos("drillpos",drillpos);
            tree.SetInt("skipcounter", skipcounter);
            tree.SetBool("nostorage", nostorage);
            base.ToTreeAttributes(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            drillpos = tree.GetBlockPos("drillpos",StartDrillPos);
            skipcounter = tree.GetInt("skipcounter");
            nostorage = tree.GetBool("nostorage");
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (!structurecomplete)
            {
                dsc.AppendLine("Structure incomplete!");
            }
            if (nostorage)
            {
                dsc.AppendLine("No available storage!");
            }
        }
    }
}
