﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace QptechFurniture.src
{
    // Concept
    // Flower-In-Pot configuration gets moved from the flower pot json into the flower jsons
    // attributes: {
    //   plantContainable: {
    //     smallContainer: {
    //        shape: [custom shape, otherwise vanilla block shape],
    //        textures: { },
    //        transform: { },
    //     }
    //     largeContainer: {
    //        shape: [custom shape, otherwise vanilla block shape],
    //        textures: { },
    //        transform: { },
    //     }
    // }
    //

    public class BlockPlantLongContainer : Block
    {
        WorldInteraction[] interactions = new WorldInteraction[0];

        public string ContainerSize => Attributes["plantContainerSize"].AsString();

        public override void OnLoaded(ICoreAPI api)
        {
            LoadColorMapAnyway = true;

            List<ItemStack> stacks = new List<ItemStack>();

            if (Variant["contents"] != "empty")
            {
                return;
            }


            foreach (var block in api.World.Blocks)
            {
                if (block.Code == null || block.IsMissing) continue;

                if (block.Attributes?["plantContainable"].Exists == true)
                {
                    stacks.Add(new ItemStack(block));
                }
            }

            foreach (var item in api.World.Items)
            {
                if (item.Code == null || item.IsMissing) continue;

                if (item.Attributes?["plantContainable"].Exists == true)
                {
                    stacks.Add(new ItemStack(item));
                }
            }

            interactions = new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-flowerpot-plant",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = stacks.ToArray()
                }
            };
        }

        public ItemStack GetContents(IWorldAccessor world, BlockPos pos)
        {
            BlockEntityLongPlantContainer be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityLongPlantContainer;
            return be?.GetContents();
        }


        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
        {
            base.OnDecalTesselation(world, decalMesh, pos);
            BlockEntityLongPlantContainer bept = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityLongPlantContainer;
            if (bept != null)
            {
                decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, bept.MeshAngle, 0);
            }
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            if (val)
            {
                BlockEntityLongPlantContainer bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityLongPlantContainer;
                if (bect != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(dx, dz);

                    float deg45rad = GameMath.PI / 4;
                    float roundRad = ((int)Math.Round(angleHor / deg45rad)) * deg45rad;
                    bect.MeshAngle = roundRad;
                }
            }

            return val;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            base.OnBlockBroken(world, pos, byPlayer);

            ItemStack contents = GetContents(world, pos);
            if (contents != null)
            {
                world.SpawnItemEntity(contents, pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return base.OnPickBlock(world, pos);
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityLongPlantContainer be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityLongPlantContainer;

            if (byPlayer.InventoryManager?.ActiveHotbarSlot?.Empty == false && be != null)
            {
                return be.TryPutContents(byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer);
            }

            return false;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}