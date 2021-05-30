﻿using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using System.Collections.Generic;

namespace qptech.src { 
   
    class BlockWire: ElectricalBlock
    {

        ICoreClientAPI capi;
        Block snowLayerBlock;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            capi = api as ICoreClientAPI;

            snowLayerBlock = api.World.GetBlock(new AssetLocation("snowlayer-1"));
        }

        public void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            // Todo: make this work
            /*            int nBlockId = chunkExtIds[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Up]];
                        Block upblock = api.World.Blocks[nBlockId];
                        if (upblock.snowLevel >= 1 && snowLayerBlock != null)
                        {
                            sourceMesh = sourceMesh.Clone();
                            sourceMesh.AddMeshData(capi.TesselatorManager.GetDefaultBlockMesh(snowLayerBlock));
                            return;
                        }*/

            return;  // no windwave for solid fences!

            //base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtIds, chunkLightExt, extIndex3d);
        }

        public string GetOrientations(IWorldAccessor world, BlockPos pos)
        {
            string orientations =
                GetWireCode(world, pos, BlockFacing.NORTH) +
                GetWireCode(world, pos, BlockFacing.EAST) +
                GetWireCode(world, pos, BlockFacing.SOUTH) +
                GetWireCode(world, pos, BlockFacing.WEST)
            ;

            if (orientations.Length == 0) orientations = "empty";
            return orientations;
        }

        private string GetWireCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
        {
            if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];
            return "";
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            string orientations = GetOrientations(world, blockSel.Position);
            Block block = world.BlockAccessor.GetBlock(CodeWithVariant("type", orientations));

            if (block == null) block = this;

            if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                return true;
            }

            return false;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            string orientations = GetOrientations(world, pos);

            AssetLocation newBlockCode = CodeWithVariant("type", orientations);
            if (!Code.Equals(newBlockCode))
            {
                Block block = world.BlockAccessor.GetBlock(newBlockCode);
                if (block == null) return;

                world.BlockAccessor.SetBlock(block.BlockId, pos);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                world.BlockAccessor.MarkBlockDirty(pos);
            }
            else
            {
                base.OnNeighbourBlockChange(world, pos, neibpos);
            }
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type", "wirecover" }, new string[] { "ew", "uncovered" }));
            return new ItemStack[] { new ItemStack(block) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type", "wirecover" }, new string[] { "ew", "uncovered" }));
            return new ItemStack(block);
        }
        public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
        {
            Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

            bool attrexists = block.Attributes?["wireConnect"][side.Code].Exists == true;
            if (attrexists)
            {
                return block.Attributes["wireConnect"][side.Code].AsBool(true);
            }

            return
               block is ElectricalBlock;
        }


        static string[] OneDir = new string[] { "n", "e", "s", "w" };
        static string[] TwoDir = new string[] { "ns", "ew" };
        static string[] AngledDir = new string[] { "ne", "es", "sw", "nw" };
        static string[] ThreeDir = new string[] { "nes", "new", "nsw", "esw" };

        static Dictionary<string, KeyValuePair<string[], int>> AngleGroups = new Dictionary<string, KeyValuePair<string[], int>>();

        static BlockWire()
        {
            AngleGroups["n"] = new KeyValuePair<string[], int>(OneDir, 0);
            AngleGroups["e"] = new KeyValuePair<string[], int>(OneDir, 1);
            AngleGroups["s"] = new KeyValuePair<string[], int>(OneDir, 2);
            AngleGroups["w"] = new KeyValuePair<string[], int>(OneDir, 3);

            AngleGroups["ns"] = new KeyValuePair<string[], int>(TwoDir, 0);
            AngleGroups["ew"] = new KeyValuePair<string[], int>(TwoDir, 1);

            AngleGroups["ne"] = new KeyValuePair<string[], int>(AngledDir, 0);
            AngleGroups["es"] = new KeyValuePair<string[], int>(AngledDir, 1);
            AngleGroups["sw"] = new KeyValuePair<string[], int>(AngledDir, 2);
            AngleGroups["nw"] = new KeyValuePair<string[], int>(AngledDir, 3);

            AngleGroups["nes"] = new KeyValuePair<string[], int>(ThreeDir, 0);
            AngleGroups["new"] = new KeyValuePair<string[], int>(ThreeDir, 1);
            AngleGroups["nsw"] = new KeyValuePair<string[], int>(ThreeDir, 2);
            AngleGroups["esw"] = new KeyValuePair<string[], int>(ThreeDir, 3);
        }

        public override AssetLocation GetRotatedBlockCode(int angle)
        {
            string type = Variant["type"];

            if (type == "empty" || type == "nesw") return Code;


            int angleIndex = angle / 90;

            var val = AngleGroups[type];

            string newFacing = val.Key[GameMath.Mod(val.Value + angleIndex, val.Key.Length)];

            return CodeWithVariant("type", newFacing);
            
        }

        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            var mywire= world.BlockAccessor.GetBlockEntity(pos) as BEEWire;
            if (mywire != null) { mywire.EntityCollide(entity); }
        }
    }
}
