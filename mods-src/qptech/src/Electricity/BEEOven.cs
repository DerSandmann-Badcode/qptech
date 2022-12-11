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
using Vintagestory.API.Util;
using Vintagestory.API.Client;

namespace qptech.src
{
    class BEEOven:BEEBaseDevice
    {
        protected float animationSpeed = 0.5f;
        
        protected double internalheat;
        protected double stackheat;
        protected double restingheat = 30;
        protected double heatPerTick = 5;
        protected double insulationFactor = 0.5;
        protected double maxHeat = 1000;
        protected double stackHeatFactor = 50; //averages up the heat over this factor
        public double StackHeatChange => (internalheat + stackheat * (stackHeatFactor - 1)) / stackHeatFactor;
        /// <summary>
        /// Internal heat starts at resting heat
        /// While processing will increse internal heat,
        /// somehow we will average stack heat (which starts at resting - or possibly
        /// pull from object?) up towards internal heat somehow
        /// Once heat is reached item is done - if not processing then heat will
        /// slowly fall to resting heat
        /// Insulation factor determines how quickly internal heat falls
        /// </summary>
        protected int processQty = 1; //how many items to process at once
        protected BlockFacing rmInputFace; //what faces will be checked for input containers
        protected BlockFacing outputFace ;
        ItemStack bakingitemstack;
        string bakingcode = "";
        float? bakingtemp = 0;
        DummyInventory dummy;
        private SimpleParticleProperties smokeParticles;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (Block.Attributes != null)
            {
                //requiredFlux = Block.Attributes["requiredFlux"].AsInt(requiredFlux);
                rmInputFace = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("west"));

                outputFace = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("east"));
                animationSpeed = Block.Attributes["animationSpeed"].AsFloat(animationSpeed);
                rmInputFace = OrientFace(Block.Code.ToString(), rmInputFace);
                outputFace = OrientFace(Block.Code.ToString(), outputFace);
                heatPerTick = Block.Attributes["heatPerTick"].AsDouble(heatPerTick);
                insulationFactor = Block.Attributes["insulationFactor"].AsDouble(insulationFactor);
                maxHeat = Block.Attributes["maxHeat"].AsDouble(maxHeat);
                stackHeatFactor = Block.Attributes["stackHeatFactor"].AsDouble(stackHeatFactor);
            }
            dummy = new DummyInventory(api);
        }
        protected override void DoDeviceStart()
        {
            if (Api.World.Side is EnumAppSide.Client) { return; }
            if (!IsPowered) { DoFailedStart(); }
            
            if (deviceState == enDeviceState.IDLE)
            {
                
                TryStart();
                if (deviceState == enDeviceState.IDLE) { DoCooling(); }
            }
            this.MarkDirty(true);
        }
        void DoCooling()
        {
            if (Api.World.Side is EnumAppSide.Client) { return; }
            if (internalheat <= restingheat) { internalheat = restingheat;return; }
            internalheat *= insulationFactor;
            
            stackheat = StackHeatChange; //BS average code lol
            this.MarkDirty(true);
        }


        protected override void DoDeviceProcessing()
        {

            
            if (Api.World.Side is EnumAppSide.Client)
            {
                return;
            }
            if (!IsPowered) { DoCooling(); return; }
            
            if (internalheat < maxHeat)
            {
                internalheat += heatPerTick;
                if (internalheat > maxHeat) { internalheat = maxHeat; }
            }
            stackheat = StackHeatChange; //BS average code lol
            if (stackheat >= bakingtemp )
            {
                DoDeviceComplete();
            }
            this.MarkDirty(true);
        }
        protected override void DoRunningParticles()
        {

            smokeParticles = new SimpleParticleProperties(
                  1, 2,
                  ColorUtil.ToRgba(70, 22, 22, 22),
                  new Vec3d(),
                  new Vec3d(0.75, 0, 0.75),
                  new Vec3f(-1 / 32f, 0.1f, -1 / 32f),
                  new Vec3f(1 / 32f, 0.1f, 1 / 32f),
                  1.5f,
                  -0.025f / 4,
                  0.2f,
                  0.6f,
                  EnumParticleModel.Quad
              );

            smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
            smokeParticles.SelfPropelled = true;
            smokeParticles.AddPos.Set(8 / 16.0, 0, 8 / 16.0);
            smokeParticles.MinPos.Set(Pos.X + 4 / 16f, Pos.Y + 3 / 16f, Pos.Z + 4 / 16f);
            Api.World.SpawnParticles(smokeParticles);
        }
        protected override void DoDeviceComplete()
        {
            if (deviceState != enDeviceState.RUNNING) { 
                return;
            }
            
            ItemStack resultStack = null;
            if (bakingitemstack.Class == EnumItemClass.Block)
            {
                Block block = Api.World.GetBlock(new AssetLocation(bakingcode));
                if (block != null)
                {
                    resultStack = new ItemStack(block);
                }
            }
            else
            {
                Item item = Api.World.GetItem(new AssetLocation(bakingcode));
                if (item != null) resultStack = new ItemStack(item);
            }

            if (resultStack == null)
            {
                deviceState = enDeviceState.ERROR;
                MarkDirty(true);
                return;
            }

            var collObjCb = bakingitemstack.Collectible as IBakeableCallback;

            if (collObjCb != null)
            {
                collObjCb.OnBaked(bakingitemstack, resultStack);
            }

            
            dummy[0].Itemstack = resultStack;
            BlockPos bp = Pos.Copy().Offset(outputFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            var outputContainer = checkblock as BlockEntityContainer;
            int outputQuantity = 1;
            if (outputContainer != null)
            {
                WeightedSlot tryoutput = outputContainer.Inventory.GetBestSuitedSlot(dummy[0]);

                if (tryoutput.slot != null)
                {
                    ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, outputQuantity);

                    dummy[0].TryPutInto(tryoutput.slot, ref op);

                }
            }

            if (!dummy.Empty)
            {
                //If no storage then spill on the ground
                return;
            }
            deviceState = enDeviceState.IDLE;
            Api.World.PlaySoundAt(new AssetLocation("sounds/doorslide"), Pos.X, Pos.Y, Pos.Z, null, false, 8, 1);
            MarkDirty(true);
        }

        void TryStart()
        {
 
            if (Api.World.Side is EnumAppSide.Client) { return; }
            bakingcode = "";
            bakingtemp = 0;
            bakingitemstack = null;
            MarkDirty(true);
            BlockPos bp = Pos.Copy().Offset(rmInputFace);
            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            var inputContainer = checkblock as BlockEntityContainer;
            if (inputContainer == null) { return; }
            if (inputContainer.Inventory.Empty) { return; }
            for (int c = 0; c < inputContainer.Inventory.Count; c++)
            {
                ItemSlot checkslot = inputContainer.Inventory[c];
                if (checkslot == null) { continue; }
                if (checkslot.StackSize == 0) { continue; }
                if (checkslot.Itemstack.Attributes.GetBool("bakeable", true) == false) continue;
                BakingProperties bakingprops = BakingProperties.ReadFrom(checkslot.Itemstack);
                bakingitemstack = checkslot.Itemstack.Clone();
                if (bakingprops == null) continue;
                bakingcode = bakingprops.ResultCode;
                bakingtemp = bakingprops.Temp;
                inputContainer.Inventory[c].Itemstack.StackSize--;
                if (inputContainer.Inventory[c].Itemstack.StackSize == 0)
                {
                    inputContainer.Inventory[c].Itemstack = null;
                }
                inputContainer.MarkDirty(true);
                deviceState = enDeviceState.RUNNING;
                MarkDirty(true);

            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine("INTERNAL TEMP : " + Math.Floor(internalheat).ToString());
            if (bakingcode != null)
            {
                dsc.AppendLine("Baking: " + bakingcode);
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            
            internalheat = tree.GetDouble("internalheat");

            stackheat = tree.GetDouble("stackheat");
            bakingcode = tree.GetString("bakingcode", "");
            bakingtemp = tree.GetFloat("bakingtemp", 0);
            bakingitemstack = tree.GetItemstack("bakingitemstack",null);
            if (bakingitemstack != null)
            {
                bakingitemstack.ResolveBlockOrItem(worldAccessForResolve);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            
            tree.SetDouble("internalheat", internalheat);

            tree.SetDouble("stackheat", stackheat);
            tree.SetString("bakingcode", bakingcode);
            tree.SetFloat("bakingtemp", (float)bakingtemp);
            tree.SetItemstack("bakingitemstack", bakingitemstack);
        }
    }
}
