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
        
        Dictionary<string, bool> statepassable;
        
        public virtual bool Passable {
            get
            {
                if (statepassable == null || !statepassable.ContainsKey(currentstate)) { return false; }
                return statepassable[currentstate];
            }
        }
        BlockPos controlblockpos;
        public BlockPos ControlBlockPos;
        public BEFunctionChiseled ControlBlock;

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
            if (currentstate == closename) { SetState(openname); }
            else { SetState(closename); }
        }

        void SetupDictionaries() {
            if (statevoxels == null) { statevoxels = new Dictionary<string, List<uint>>(); }
            if (statematerials == null) { statematerials = new Dictionary<string, List<int>>(); }
            
            if (statepassable == null) { statepassable = new Dictionary<string, bool>(); }
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
            
            if (Api is ICoreClientAPI)
            {
                this.MaterialIds = statematerials[newstate].ToArray();
                this.VoxelCuboids = statevoxels[newstate];
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
        
        
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("currentstate", currentstate);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            currentstate = tree.GetString("currentstate", currentstate);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine("STATE:" + currentstate);
            base.GetBlockInfo(forPlayer, dsc);
        }
    }
}
