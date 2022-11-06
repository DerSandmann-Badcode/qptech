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
    class BEFunctionChiseled : BlockEntityMicroBlock
    {
        public static string openname = "OPEN";
        public static string closename = "CLOSE";
        string currentstate = closename;
        Dictionary<string, List<uint>> statevoxels;
        Dictionary<string, List<int>> statematerials;
        
        //Passable needs a way to set things passable (ie no collision)
        Dictionary<string, bool> statepassable;
        List<BlockPos> controlledblocks;
        
        
        public virtual bool Passable {
            get
            {
                if (statepassable == null || !statepassable.ContainsKey(currentstate)) { return false; }
                return statepassable[currentstate];
            }
        }
        BlockPos controlblockpos;
        public BlockPos ControlBlockPos=>controlblockpos;
        

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            SetState(currentstate);
        }
                
        //Add set of voxel data to the library
        public virtual void AddState(string key, List<uint> voxels, List<int> materials, bool passable=false,bool changenow = true)
        {
            SetupDictionaries();
            statevoxels[key] = voxels;
            statematerials[key] = materials;
            
            statepassable[key] = passable;
            MarkDirty(true);
            if (changenow) { SetState(key); }
        }
        //helper function to easily add a door open state
        public virtual void AddOpen(List<uint>voxels, List<int> materials)
        {
            AddState(openname, voxels, materials, true, true);
        }
        //helper function to easily add a door closed state
        public virtual void AddClosed(List<uint> voxels, List<int> materials)
        {
            AddState(closename, voxels, materials, false, true);
        }

        //will cycle between open and closed, or set to closed if in a different state
        public virtual void ToggleOpenClosed()
        {
            //first if we are controlled by a block we will just let the block know and wait for it to tell us to open/close
            if (controlblockpos != Pos)
            {
                BEFunctionChiseled bfc = Api.World.BlockAccessor.GetBlockEntity(controlblockpos) as BEFunctionChiseled;
                if (bfc != null)
                {
                    bfc.ToggleOpenClosed();
                    return;
                }
                else
                {
                    controlblockpos = Pos;
                }
            }
            if (currentstate == closename) { SetState(openname); }
            else { SetState(closename); }
            if (controlledblocks != null && controlledblocks.Count > 0)
            {
                foreach (BlockPos p in controlledblocks)
                {
                    BEFunctionChiseled bfc = Api.World.BlockAccessor.GetBlockEntity(controlblockpos) as BEFunctionChiseled;
                    if (bfc == null) { continue; }
                    bfc.ControlBlockSignal(Pos, currentstate);
                }
            }
        }

        //change state to match door control signal (if possible)
        //also will record a given door as controlling it so it can send future door toggle requests
        //to the controller
        //also if a door control signal is recieved it will cease controlling other doors
        public virtual bool ControlBlockSignal(BlockPos fromblock, string tostate)
        {
            
            //if a signal is received from a door that is controlled by us, dump all controlled doors and obey the signal
            if (controlledblocks != null)
            {
                controlledblocks = new List<BlockPos>();
                controlblockpos = fromblock;
            }
            SetState(tostate);
            return true;
        }

        void SetupDictionaries() {
            if (statevoxels == null) { statevoxels = new Dictionary<string, List<uint>>(); }
            if (statematerials == null) { statematerials = new Dictionary<string, List<int>>(); }
            
            if (statepassable == null) { statepassable = new Dictionary<string, bool>(); }
            if (controlledblocks == null) { controlledblocks = new List<BlockPos>(); }
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {

            //Mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, rotationspeed * GameMath.DEG2RAD, 0);
            //rotationtracker += rotationspeed;
            return base.OnTesselation(mesher, tesselator);
        }

        //Set the object to a particular state
        public virtual void SetState(string newstate)
        {
            if (statepassable == null || !statepassable.ContainsKey(newstate)) { return; }
            
            
                this.MaterialIds = statematerials[newstate].ToArray();
                this.VoxelCuboids = statevoxels[newstate];
                
            if (Api is ICoreClientAPI)
            {
                RegenMesh(Api as ICoreClientAPI);
            }
            currentstate = newstate;
            MarkDirty(true);
        }

        public virtual void Sync()
        {
           //here we should verify which set of chisel data to show
            if (Api is ICoreClientAPI)
            {
                RegenMesh(Api as ICoreClientAPI);
            }
        }
        
        public virtual void SetControlledBlocks(List<BlockPos> newlist)
        {
            controlblockpos = Pos;
            controlledblocks= new List<BlockPos>(newlist);
            SetState(closename);
            foreach (BlockPos p in newlist)
            {
                BEFunctionChiseled bfc = Api.World.BlockAccessor.GetBlockEntity(p) as BEFunctionChiseled;
                if (p == null) { continue; }
                bfc.ControlBlockSignal(Pos, closename);
            }
            MarkDirty(true);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("currentstate", currentstate);
            
            //Serialize &  Save all the dictionaries
            SetupDictionaries();
            byte[] bvox = SerializerUtil.Serialize<Dictionary<string, List<uint>>>(statevoxels);
            byte[] bmat = SerializerUtil.Serialize<Dictionary<string, List<int>>>(statematerials);
            byte[] bpass = SerializerUtil.Serialize<Dictionary<string, bool>>(statepassable);
            byte[] bcontrol = SerializerUtil.Serialize<List<BlockPos>>(controlledblocks);
            tree.SetBytes("statevoxels", bvox);
            tree.SetBytes("statematerials", bmat);
            tree.SetBytes("statepassable", bpass);
            tree.SetBytes("controlledblocks", bcontrol);
            if (controlblockpos == null) { controlblockpos = Pos; }
            tree.SetBlockPos("controlblockpos", controlblockpos);
            
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            currentstate = tree.GetString("currentstate", currentstate);
            controlblockpos = tree.GetBlockPos("controlblockpos", controlblockpos);
            SetupDictionaries();
            try
            {
                byte[] voxdat = tree.GetBytes("statevoxels", null);
                if (voxdat == null) { return; }
                byte[] matdat = tree.GetBytes("statematerials", null);
                if (matdat == null) { return; }
                byte[] passdat = tree.GetBytes("statepassable", null);
                if (passdat == null) { return; }
                
                statevoxels = SerializerUtil.Deserialize<Dictionary<string, List<uint>>>(voxdat);
                statematerials = SerializerUtil.Deserialize<Dictionary<string, List<int>>>(matdat);
                statepassable = SerializerUtil.Deserialize<Dictionary<string, bool>>(passdat);
                byte[] controldat = tree.GetBytes("controlledblocks", null);
                if (controldat != null)
                {
                    controlledblocks = SerializerUtil.Deserialize<List<BlockPos>>(controldat);
                }
            }
            catch
            {
                return;
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine("STATE:" + currentstate);
            base.GetBlockInfo(forPlayer, dsc);
        }
    }
}
