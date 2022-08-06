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
using qptech.src.networks;


namespace qptech.src.multiblock
{
    /// <summary>
    /// Will create and link dummy objects to account for multi blocks
    /// When setting up the locations of your blocks, assume a north facing
    /// eg: [0,0,-1] would put a dummy block north of the placed block when facing north
    /// </summary>
    class BEBMultiDummy : BlockEntityBehavior, IDummyParent
    {
        public string dummyblockname => "machines:dummy";
        List<BEDummyBlock> dummies;
        int[] dummylocations;
        BlockEntity be;
        long l1;
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (Api is ICoreServerAPI)
            {
                InstantiateDummies();
                
            }
            l1 = be.RegisterDelayedCallback(SetupDummies, 1);
        }
        int[] GetDummyLocations()
        {
            int[] dummylocations = be.Block.Attributes["dummylocations"].AsArray<int>();
            string pointing = be.Block.LastCodePart();
            //settings are assuming north to start
            if (pointing == "east")
            {    
                for (int c = 0; c < dummylocations.Length; c += 3)
                {
                    if (dummylocations[c] != 0 || dummylocations[c + 2] != 0)
                    {
                        int x = dummylocations[c];
                        int z = dummylocations[c + 2];
                        dummylocations[c] = z;
                        dummylocations[c + 2] = x;
                            
                    }    
                }
            }
            else if (pointing == "south")
            {
                for (int c = 0; c < dummylocations.Length; c += 3)
                {
                    if (dummylocations[c] != 0 || dummylocations[c + 2] != 0)
                    {
                        int x = dummylocations[c];
                        int z = dummylocations[c + 2];
                        dummylocations[c] = -x;
                        dummylocations[c + 2] = -z;

                    }
                }
            }
            else if (pointing == "west")
            {
                for (int c = 0; c < dummylocations.Length; c += 3)
                {
                    if (dummylocations[c] != 0 || dummylocations[c + 2] != 0)
                    {
                        int x = dummylocations[c];
                        int z = dummylocations[c + 2];
                        dummylocations[c] = -z;
                        dummylocations[c + 2] = -x;

                    }
                }
            }
            return dummylocations;
        }
        protected virtual void InstantiateDummies()
        {
            if (be.Block.Attributes != null)
            {
                int[] dummylocations = GetDummyLocations();

                if (dummylocations != null && dummylocations.Length > 0 && Api is ICoreServerAPI)
                {
                    dummies = new List<BEDummyBlock>();
                    Block dummyblock = Api.World.BlockAccessor.GetBlock(new AssetLocation(dummyblockname));

                    for (int c = 0; c < dummylocations.Length; c += 3)
                    {
                        BlockPos dpos = new BlockPos(be.Pos.X + dummylocations[c], be.Pos.Y + dummylocations[c + 1], be.Pos.Z + dummylocations[c + 2]);
                        Block existing = Api.World.BlockAccessor.GetBlock(dpos.X,dpos.Y,dpos.Z,BlockLayersAccess.Default);
                        if (existing.Id!=0){
                            Api.World.BlockAccessor.BreakBlock(be.Pos, null);
                            return;
                        }
                        Api.World.BlockAccessor.SetBlock(dummyblock.BlockId, dpos);
                        //north/south Z east/west X
                    }
                }
            }
        }

        public virtual void SetupDummies(float dt)
        {
            if (be.Block.Attributes != null)
            {
                int[] dummylocations = GetDummyLocations();
                if (dummylocations != null && dummylocations.Length > 0 && Api is ICoreServerAPI)
                {
                    dummies = new List<BEDummyBlock>();
                    

                    for (int c = 0; c < dummylocations.Length; c += 3)
                    {
                        BlockPos dpos = new BlockPos(be.Pos.X+dummylocations[c], be.Pos.Y + dummylocations[c + 1], be.Pos.Z + dummylocations[c + 2]);
                        //adjust for facing
                        BEDummyBlock bed = Api.World.BlockAccessor.GetBlockEntity(dpos) as BEDummyBlock;
                        
                        if (bed != null)
                        {
                            bed.SetParent(this);
                            dummies.Add(bed);
                        }
                    }
                }
            }
        }
        public BEBMultiDummy(BlockEntity be) : base(be)
        {
            this.be = be;

        }
        public void OnDummyBroken()
        {
            
            Api.World.BlockAccessor.BreakBlock(be.Pos, null);
        }

        public string GetDisplayName()
        {
            return be.Block.GetPlacedBlockName(Api.World,be.Pos);
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            if (dummies != null)
            {
                foreach (BEDummyBlock dummy in dummies)
                {
                    if (dummy != null) { dummy.ParentBroken(); }
                }
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            
        }
    }
    
}
