﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace qptech.src
{

    public class EntityHopper : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("BlockEntityHiddenHopper", typeof(BlockEntityHiddenHopper));
        }


        class BlockEntityHiddenHopper : BlockEntityOpenableContainer
        {

            internal InventoryGeneric inventory;

            public BlockFacing[] PullFaces = new BlockFacing[0];
            public BlockFacing[] PushFaces = new BlockFacing[0];
            public BlockFacing[] AcceptFromFaces = new BlockFacing[0];

            public string inventoryClassName = "hopper";
            public string ItemFlowObjectLangCode = "hopper-contents";
            public int QuantitySlots = 4;
            protected float itemFlowRate = 1;

            public BlockFacing LastReceivedFromDir;

            int checkRateMs;
            float itemFlowAccum;

            private static AssetLocation hopperClose = new AssetLocation("sounds/block/cokeovendoor-close");
            private static AssetLocation hopperOpen = new AssetLocation("sounds/block/cokeovendoor-open");
            private static AssetLocation hopperTumble = new AssetLocation("sounds/block/hoppertumble");
            public override AssetLocation OpenSound => hopperOpen;
            public override AssetLocation CloseSound => hopperClose;

            public virtual float ItemFlowRate => itemFlowRate;

            private BlockEntityAnimationUtil animUtil => ((BEBehaviorAnimatable)GetBehavior<BEBehaviorAnimatable>())?.animUtil;

            public BlockEntityHiddenHopper() : base()
            {

            }

            public override InventoryBase Inventory
            {
                get { return inventory; }
            }

            private void InitInventory()
            {
                if (Block?.Attributes != null)
                {
                    if (Block.Attributes["pullFaces"].Exists)
                    {
                        string[] faces = Block.Attributes["pullFaces"].AsArray<string>(null);
                        PullFaces = new BlockFacing[faces.Length];
                        for (int i = 0; i < faces.Length; i++)
                        {
                            PullFaces[i] = BlockFacing.FromCode(faces[i]);
                        }
                    }

                    if (Block.Attributes["pushFaces"].Exists)
                    {
                        string[] faces = Block.Attributes["pushFaces"].AsArray<string>(null);
                        PushFaces = new BlockFacing[faces.Length];
                        for (int i = 0; i < faces.Length; i++)
                        {
                            PushFaces[i] = BlockFacing.FromCode(faces[i]);
                        }
                    }

                    if (Block.Attributes["acceptFromFaces"].Exists)
                    {
                        string[] faces = Block.Attributes["acceptFromFaces"].AsArray<string>(null);
                        AcceptFromFaces = new BlockFacing[faces.Length];
                        for (int i = 0; i < faces.Length; i++)
                        {
                            AcceptFromFaces[i] = BlockFacing.FromCode(faces[i]);
                        }
                    }

                    itemFlowRate = Block.Attributes["item-flowrate"].AsFloat(itemFlowRate);
                    checkRateMs = Block.Attributes["item-checkrateMs"].AsInt(200);
                    inventoryClassName = Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
                    ItemFlowObjectLangCode = Block.Attributes["itemFlowObjectLangCode"].AsString(ItemFlowObjectLangCode);
                    QuantitySlots = Block.Attributes["quantitySlots"].AsInt(QuantitySlots);
                }

                if (inventory == null)
                {
                    inventory = new InventoryGeneric(QuantitySlots, null, null, null);

                    inventory.OnInventoryClosed += OnInvClosed;
                    inventory.OnInventoryOpened += OnInvOpened;
                    inventory.SlotModified += OnSlotModifid;

                    inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
                    inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
                }
            }

            // Return the slot where a chute may pull items from. Return null if it is now allowed to pull any items from this inventory
            private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
            {
                if (PushFaces.Contains(atBlockFace))
                {
                    return inventory[0];
                }

                return null;
            }

            // Return the slot where a chute may push items into. Return null if it shouldn't move items into this inventory.
            private ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
            {
                if (PullFaces.Contains(atBlockFace) || AcceptFromFaces.Contains(atBlockFace))
                {
                    return inventory[0];
                }

                return null;
            }



            public override string InventoryClassName
            {
                get { return inventoryClassName; }
            }

            private void OnSlotModifid(int slot)
            {
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
            }

            protected virtual void OnInvOpened(IPlayer player)
            {
                inventory.PutLocked = false;

                if (Api.Side == EnumAppSide.Client)
                {
                    animUtil.StartAnimation(new AnimationMetaData()
                    {
                        Animation = "lidopen",
                        Code = "lidopen",
                        AnimationSpeed = 1.8f,
                        EaseOutSpeed = 6,
                        EaseInSpeed = 15
                    });
                }
            }

            protected virtual void OnInvClosed(IPlayer player)
            {
                animUtil.StopAnimation("lidopen");

                invDialog?.Dispose();
                invDialog = null;
            }

            public override void Initialize(ICoreAPI api)
            {
                InitInventory();

                base.Initialize(api);

                if (api is ICoreServerAPI)
                {
                    // Randomize movement a bit
                    RegisterDelayedCallback((dt) => RegisterGameTickListener(MoveItem, checkRateMs), 10 + api.World.Rand.Next(200));
                }

                if (api.World.Side == EnumAppSide.Client)
                {
                    animUtil?.InitializeAnimator("machines:hiddenhopper", new Vec3f(0, Block.Shape.rotateY, 0));
                }
            }


            /// <summary>
            /// Attempts to move the item from slot to slot.
            /// </summary>
            /// <param name="dt"></param>
            public void MoveItem(float dt)
            {
                itemFlowAccum = Math.Min(itemFlowAccum + ItemFlowRate, Math.Max(1, ItemFlowRate * 2));
                if (itemFlowAccum < 1) return;

                if (PushFaces != null && PushFaces.Length > 0 && !inventory.IsEmpty)
                {
                    ItemStack stack = inventory.First(slot => !slot.Empty).Itemstack;

                    BlockFacing outputFace = PushFaces[Api.World.Rand.Next(PushFaces.Length)];
                    int dir = stack.Attributes.GetInt("chuteDir", -1);
                    BlockFacing desiredDir = dir >= 0 && PushFaces.Contains(BlockFacing.ALLFACES[dir]) ? BlockFacing.ALLFACES[dir] : null;

                    // If we have a desired dir, try to go there
                    if (desiredDir != null)
                    {
                        // Try spit it out first
                        if (!TrySpitOut(desiredDir))
                        {
                            // Then try push it in there,
                            if (!TryPushInto(desiredDir) && outputFace != desiredDir.Opposite)
                            {
                                // Otherwise try spit it out in a random face, but only if its not back where it came frome
                                if (!TrySpitOut(outputFace))
                                {
                                    TryPushInto(outputFace);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Without a desired dir, try to spit it out anywhere first
                        if (!TrySpitOut(outputFace))
                        {
                            // Then try push it anywhere next
                            TryPushInto(outputFace);
                        }
                    }

                }

                if (PullFaces != null && PullFaces.Length > 0 && inventory.IsEmpty)
                {
                    BlockFacing inputFace = PullFaces[Api.World.Rand.Next(PullFaces.Length)];

                    TryPullFrom(inputFace);
                }
            }



            private void TryPullFrom(BlockFacing inputFace)
            {
                BlockPos InputPosition = Pos.AddCopy(inputFace);

                if (Api.World.BlockAccessor.GetBlockEntity(InputPosition) is BlockEntityContainer beContainer)
                {
                    //do not both push and pull across the same chute-chute connection
                    if (beContainer.Block is BlockChute chute)
                    {
                        string[] pushFaces = chute.Attributes["pushFaces"].AsArray<string>(null);
                        if (pushFaces?.Contains(inputFace.Opposite.Code) == true) return;
                    }

                    ItemSlot sourceSlot = beContainer.Inventory.GetAutoPullFromSlot(inputFace.Opposite);
                    ItemSlot targetSlot = sourceSlot == null ? null : inventory.GetBestSuitedSlot(sourceSlot).slot;
                    BlockEntityItemFlow beFlow = beContainer as BlockEntityItemFlow;

                    if (sourceSlot != null && targetSlot != null && (beFlow == null || targetSlot.Empty))
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, (int)itemFlowAccum);

                        int horTravelled = sourceSlot.Itemstack.Attributes.GetInt("chuteQHTravelled");
                        if (horTravelled < 2)
                        {
                            int qmoved = sourceSlot.TryPutInto(targetSlot, ref op);
                            if (qmoved > 0)
                            {
                                if (beFlow != null)
                                {
                                    targetSlot.Itemstack.Attributes.SetInt("chuteQHTravelled", inputFace.IsHorizontal ? (horTravelled + 1) : 0);
                                    targetSlot.Itemstack.Attributes.SetInt("chuteDir", inputFace.Opposite.Index);
                                }
                                else
                                {
                                    targetSlot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
                                    targetSlot.Itemstack.Attributes.RemoveAttribute("chuteDir");
                                }

                                sourceSlot.MarkDirty();
                                targetSlot.MarkDirty();
                                MarkDirty(false);
                                beFlow?.MarkDirty();
                            }

                            if (qmoved > 0 && Api.World.Rand.NextDouble() < 0.2)
                            {
                                Api.World.PlaySoundAt(hopperTumble, Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, null, true, 8, 0.5f);

                                itemFlowAccum -= qmoved;
                            }
                        }
                    }
                }
            }


            private bool TryPushInto(BlockFacing outputFace)
            {
                BlockPos OutputPosition = Pos.AddCopy(outputFace);
                if (Api.World.BlockAccessor.GetBlockEntity(OutputPosition) is BlockEntityContainer beContainer)
                {
                    ItemSlot sourceSlot = inventory.FirstOrDefault(slot => !slot.Empty);
                    if ((sourceSlot?.Itemstack?.StackSize ?? 0) == 0) return false;  //seems FirstOrDefault() method can sometimes give a slot with stacksize == 0, weird

                    int horTravelled = sourceSlot.Itemstack.Attributes.GetInt("chuteQHTravelled");
                    int chuteDir = sourceSlot.Itemstack.Attributes.GetInt("chuteDir");
                    sourceSlot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
                    sourceSlot.Itemstack.Attributes.RemoveAttribute("chuteDir");

                    if (horTravelled >= 2) return false;  //chutes can't move items more than 1 block horizontally without a drop

                    ItemSlot targetSlot = beContainer.Inventory.GetAutoPushIntoSlot(outputFace.Opposite, sourceSlot);
                    BlockEntityItemFlow beFlow = beContainer as BlockEntityItemFlow;

                    if (targetSlot != null && (beFlow == null || targetSlot.Empty))
                    {
                        int quantity = (int)itemFlowAccum;
                        ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, quantity);

                        int qmoved = sourceSlot.TryPutInto(targetSlot, ref op);

                        if (qmoved > 0)
                        {
                            if (Api.World.Rand.NextDouble() < 0.2)
                            {
                                Api.World.PlaySoundAt(hopperTumble, Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, null, true, 8, 0.5f);
                            }

                            if (beFlow != null)
                            {
                                targetSlot.Itemstack.Attributes.SetInt("chuteQHTravelled", outputFace.IsHorizontal ? (horTravelled + 1) : 0);
                                targetSlot.Itemstack.Attributes.SetInt("chuteDir", outputFace.Index);
                            }
                            else
                            {
                                targetSlot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
                                targetSlot.Itemstack.Attributes.RemoveAttribute("chuteDir");
                            }

                            sourceSlot.MarkDirty();
                            targetSlot.MarkDirty();
                            MarkDirty(false);
                            beFlow?.MarkDirty(false);

                            itemFlowAccum -= qmoved;

                            return true;
                        }
                        else
                        {
                            //If the push failed, re-apply original chuteDir so that the itemStack still has it for next push attempt
                            sourceSlot.Itemstack.Attributes.SetInt("chuteDir", chuteDir);
                        }
                    }
                }

                return false;
            }

            private bool TrySpitOut(BlockFacing outputFace)
            {
                if (!PushFaces.Contains(outputFace)) return false;

                if (Api.World.BlockAccessor.GetBlock(Pos.AddCopy(outputFace)).Replaceable >= 6000)
                {
                    ItemSlot sourceSlot = inventory.FirstOrDefault(slot => !slot.Empty);

                    ItemStack stack = sourceSlot.TakeOut((int)itemFlowAccum);
                    itemFlowAccum -= stack.StackSize;

                    stack.Attributes.RemoveAttribute("chuteQHTravelled");
                    stack.Attributes.RemoveAttribute("chuteDir");

                    float velox = outputFace.Normalf.X / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 1 / 20f) * Math.Sign(outputFace.Normalf.X);
                    float veloy = outputFace.Normalf.Y / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 1 / 20f) * Math.Sign(outputFace.Normalf.Y);
                    float veloz = outputFace.Normalf.Z / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 1 / 20f) * Math.Sign(outputFace.Normalf.Z);

                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5 + outputFace.Normalf.X / 2, 0.5 + outputFace.Normalf.Y / 2, 0.5 + outputFace.Normalf.Z / 2), new Vec3d(velox, veloy, veloz));

                    sourceSlot.MarkDirty();
                    MarkDirty(false);
                    return true;
                }

                return false;
            }

            public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
            {
                if (Api.World is IServerWorldAccessor)
                {
                    byte[] data;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryWriter writer = new BinaryWriter(ms);
                        writer.Write("BlockEntityItemFlowDialog");
                        writer.Write(Lang.Get(ItemFlowObjectLangCode));
                        writer.Write((byte)4); // Quantity columns
                        TreeAttribute tree = new TreeAttribute();
                        inventory.ToTreeAttributes(tree);
                        tree.ToBytes(writer);
                        data = ms.ToArray();
                    }

                    ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                        (IServerPlayer)byPlayer,
                        Pos.X, Pos.Y, Pos.Z,
                        (int)EnumBlockContainerPacketId.OpenInventory,
                        data
                    );

                    byPlayer.InventoryManager.OpenInventory(inventory);

                }

                return true;
            }


            public override void OnReceivedServerPacket(int packetid, byte[] data)
            {
                base.OnReceivedServerPacket(packetid, data);
            }

            public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
            {
                InitInventory();

                int index = tree.GetInt("lastReceivedFromDir");

                if (index < 0) LastReceivedFromDir = null;
                else LastReceivedFromDir = BlockFacing.ALLFACES[index];

                base.FromTreeAttributes(tree, worldForResolving);
            }

            public override void ToTreeAttributes(ITreeAttribute tree)
            {
                base.ToTreeAttributes(tree);

                tree.SetInt("lastReceivedFromDir", LastReceivedFromDir?.Index ?? -1);
            }

            public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
            {
                if (Block is BlockChute)
                {
                    //Don't call base.GetBlockInfo - we don't want to show food perish info for chutes as they are not going to be long term storage!
                    foreach (var val in Behaviors)
                    {
                        val.GetBlockInfo(forPlayer, sb);
                    }

                    sb.AppendLine(Lang.Get("Transporting: {0}", inventory[0].Empty ? Lang.Get("nothing") : inventory[0].StackSize + "x " + inventory[0].GetStackName()));

                    // Uncomment this code block for useful debug information
                    /*
                    sb.AppendLine(string.Format("Chute: {0} {1} {2}", c.Type, c.Side, c.Vertical));
                    String s1, s2;
                    s1 = (c.PullFaces.Length > 0) ? c.PullFaces[0] : "-";
                    s2 = (c.PullFaces.Length > 1) ? c.PullFaces[1] : "-";
                    sb.AppendLine(string.Format("Pull: {0} {1}", s1, s2));
                    s1 = (c.PushFaces.Length > 0) ? c.PushFaces[0] : "-";
                    s2 = (c.PushFaces.Length > 1) ? c.PushFaces[1] : "-";
                    sb.AppendLine(string.Format("Push: {0} {1}", s1, s2));
                    s1 = (c.AcceptFaces.Length > 0) ? c.AcceptFaces[0] : "-";
                    s2 = (c.AcceptFaces.Length > 1) ? c.AcceptFaces[1] : "-";
                    sb.AppendLine(string.Format("Accept: {0} {1}", s1, s2));
                    */
                    sb.AppendLine("\u00A0                                                           \u00A0");   //Try to prevent info box size changes when contents changes
                }
                else
                {  //It's a hopper
                    base.GetBlockInfo(forPlayer, sb);

                    sb.AppendLine(Lang.Get("Contents: {0}", inventory[0].Empty ? Lang.Get("Empty") : inventory[0].StackSize + "x " + inventory[0].GetStackName()));
                }
            }

            public override void OnBlockBroken()
            {
                if (Api.World is IServerWorldAccessor)
                {
                    Vec3d epos = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
                    foreach (var slot in inventory)
                    {
                        if (slot.Itemstack == null) continue;

                        slot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
                        slot.Itemstack.Attributes.RemoveAttribute("chuteDir");

                        Api.World.SpawnItemEntity(slot.Itemstack, epos);
                        slot.Itemstack = null;
                        slot.MarkDirty();
                    }
                }

                invDialog?.TryClose();
                invDialog = null;

                base.OnBlockBroken();
            }

        }
    }
}