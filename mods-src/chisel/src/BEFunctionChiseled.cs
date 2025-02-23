﻿using System;
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
using ProtoBuf;
namespace chisel.src
{
    class BEFunctionChiseled : BlockEntityMicroBlock
    {
        public enum enPacketCode { SETSTATE=999001, ADDSTATE=999002, SETCONTROLLED=999003 };
        public static string openname = "OPEN";
        public static string closename = "CLOSE";
        public static string originalblockname = "ORIGINAL";
        string currentstate = closename;
        Dictionary<string, List<uint>> statevoxels;
        Dictionary<string, List<int>> statematerials;
        ICoreServerAPI sapi;
        //Passable needs a way to set things passable (ie no collision)
        Dictionary<string, bool> statepassable;
        List<BlockPos> controlledblocks;
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
           
            //ItemStack dropstack = new ItemStack(sapi.World.GetItem(new AssetLocation("chiseltools:doorpart")), 1);
            //sapi.World.SpawnItemEntity(dropstack, Pos.ToVec3d());
            controlblockpos = null;
            if (controlledblocks != null && controlledblocks.Count > 0)
            {
                foreach (BlockPos pos in controlledblocks)
                {
                    BEFunctionChiseled bfc = Api.World.BlockAccessor.GetBlockEntity(pos) as BEFunctionChiseled;
                    if (bfc == null) { continue; }
                    bfc.SetControlledBlocks(new List<BlockPos>());
                }
                controlledblocks = new List<BlockPos>();
            }
            //now we need to make a new chiseled block where the old door block was and copy back original state
            if (!statevoxels.ContainsKey(originalblockname)) { return; }
            //package up a special door data flagged to create original and let server make the block next tick
            
            if (Api is ICoreClientAPI)
            {
                DoorData reborn = new DoorData();
                reborn.voxeldata = new List<uint>(statevoxels[originalblockname]);
                reborn.matdata = new List<int>(statematerials[originalblockname]);
                reborn.pos = Pos;
                reborn.makenewblock = true;
                (ChiselToolLoader.loader.chiselnet as IClientNetworkChannel).SendPacket<DoorData>(reborn);
            }
        }

        public virtual bool Passable {
            get
            {
                if (statepassable == null || !statepassable.ContainsKey(currentstate)) { return false; }
                return statepassable[currentstate];
            }
        }

       

        BlockPos controlblockpos;
        public BlockPos ControlBlockPos => controlblockpos;

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            controlblockpos = Pos;
            SetupDictionaries();
            List<int> newmat = new List<int>();
            Block blankblock = Api.World.GetBlock(new AssetLocation("chiseltools:techblank"));
            newmat.Add(blankblock.BlockId);

            List<uint> newvox = new List<uint>();
            CuboidWithMaterial cwms = new CuboidWithMaterial();
            cwms.Set(0, 0, 0, 16, 16, 16);
            cwms.Material = 0;

            newvox.Add(ToUint(cwms));
            //add a (ugly) block as closed state
            AddState(closename, newvox, newmat, false, false);
            //add a default open/empty state
            AddState(openname, newvox, newmat, true, true);
            MarkDirty();
            
            base.OnBlockPlaced(byItemStack);
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is ICoreServerAPI) { sapi = api as ICoreServerAPI; }
            SetState(currentstate);
        }

        //Add set of voxel data to the library
        public virtual void AddState(string key, List<uint> voxels, List<int> materials, bool passable = false, bool blank = false)
        {
            if (sapi == null) { return; }
            SetupDictionaries();
            if (blank)
            {
                CuboidWithMaterial cwms = new CuboidWithMaterial();
                cwms.Set(0, 0, 0, 16, 16, 16);
                cwms.Material = 0;
                materials = new List<int>();
                Block blankblock = Api.World.GetBlock(new AssetLocation("chiseltools:blankblock"));
                materials.Add(blankblock.BlockId);

                voxels = new List<uint>();
                voxels.Add(ToUint(cwms));
            }
            statevoxels[key] = voxels;
            statematerials[key] = materials;
            statepassable[key] = passable;
            MarkDirty(true);
            
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
            string targetstate = currentstate;
            if (currentstate == closename) { SetState(openname); targetstate = openname; }
            else { SetState(closename); targetstate = closename; }
            if (controlledblocks != null && controlledblocks.Count > 0)
            {
                foreach (BlockPos p in controlledblocks)
                {
                    BEFunctionChiseled bfc = Api.World.BlockAccessor.GetBlockEntity(p) as BEFunctionChiseled;
                    if (bfc == null) { continue; }
                    bfc.ControlBlockSignal(Pos, targetstate);

                }
            }
        }

        //change state to match door control signal (if possible)
        //also will record a given door as controlling it so it can send future door toggle requests
        //to the controller
        //also if a door control signal is recieved it will cease controlling other doors
        public virtual bool ControlBlockSignal(BlockPos fromblock, string tostate)
        {
            controlblockpos = fromblock;
            controlledblocks = new List<BlockPos>();
            SetState(tostate);
            return true;
        }

        public virtual void InitControlBlockSignal(BlockPos fromblock)
        {
            controlblockpos = fromblock;
            controlledblocks = new List<BlockPos>();
            MarkDirty(true);
            SetState(closename);
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

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid == (int)enPacketCode.ADDSTATE && data!=null)
            {
                DoorData dat = SerializerUtil.Deserialize<DoorData>(data);
                if (dat == null) { return; }
                AddState(dat.state, dat.voxeldata, dat.matdata, dat.passable, dat.transparent);
            }
            else if (packetid == (int)enPacketCode.SETCONTROLLED)
            {
                if (data == null) { controlblockpos = Pos;controlledblocks = new List<BlockPos>();MarkDirty(true); }
                else
                {
                    controlledblocks = SerializerUtil.Deserialize<List<BlockPos>>(data);
                    controlblockpos = Pos;
                    MarkDirty(true);
                }
            }
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)enPacketCode.ADDSTATE && data != null)
            {
                DoorData dat = SerializerUtil.Deserialize<DoorData>(data);
                if (dat == null) { return; }
                AddState(dat.state, dat.voxeldata, dat.matdata, dat.passable, dat.transparent);
            }
            base.OnReceivedServerPacket(packetid, data);
        }
        //Set the object to a particular state
        public virtual void SetState(string newstate)
        {
            if (statepassable == null || !statepassable.ContainsKey(newstate)) { return; }


            this.MaterialIds = statematerials[newstate].ToArray();
            this.VoxelCuboids = statevoxels[newstate];

            currentstate = newstate;
            if (Api is ICoreClientAPI)
            {
                RegenMesh();
            }
            MarkDirty(true);

        }

        public virtual void SetControlledBlocks(List<BlockPos> newlist)
        {
            controlblockpos = Pos;
            controlledblocks = new List<BlockPos>(newlist);

            foreach (BlockPos p in newlist)
            {
                if (p == Pos)
                {
                    break;
                }
                BEFunctionChiseled bfc = Api.World.BlockAccessor.GetBlockEntity(p) as BEFunctionChiseled;
                if (p == null) { continue; }
                bfc.InitControlBlockSignal(Pos);
            }

            SetState(closename);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            try
            {
                base.ToTreeAttributes(tree);
            }
            catch
            {
                return;
            }
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
            if (controlblockpos == null) { controlblockpos = Pos; }
            if (controlblockpos != Pos) { dsc.AppendLine("Controlled by " + controlblockpos.ToString()); }
            if (controlledblocks != null && controlledblocks.Count > 0)
            {
                dsc.AppendLine("Controlling:");
                foreach (BlockPos p in controlledblocks)
                {
                    dsc.Append(p.ToString() + ",");
                }
            }
            if (VoxelCuboids == null || VoxelCuboids.Count == 0) { return; }
            base.GetBlockInfo(forPlayer, dsc);
        }
    }
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class DoorData
    {
        public BlockPos pos;
        public DoorData() { }
        public string state;
        public List<uint> voxeldata;
        public List<int> matdata;
        public bool passable=false;
        public bool transparent = false;
        public bool makenewblock = false;
    }

}
