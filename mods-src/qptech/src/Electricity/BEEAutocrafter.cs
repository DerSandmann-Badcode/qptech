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
using Vintagestory.API.Client;
namespace qptech.src
{
    class BEEAutocrafter : BEEBaseDevice
    {
        BlockFacing rmInputFace;
        BlockFacing fgOutputFace;
        string currentrecipecode;
        List<GridRecipe> currentrecipes;
        public List<GridRecipe> CurrentRecipes
        {
            get
            {

                if (Api != null)
                {
                    if (currentrecipecode == "") { return null; }
                    if (currentrecipes!=null&&currentrecipes[0].Output.ResolvedItemstack.Collectible.Code.ToString()==currentrecipecode)
                    { return currentrecipes; }
                    currentrecipes= GetRecipeForItem(Api, currentrecipecode);
                    return currentrecipes;
                }
                return null;
            }

        }

        public static List<GridRecipe> GetRecipeForItem(ICoreAPI api, string recipecode)
        {
            if (api == null||api.World==null||api.World.GridRecipes==null||recipecode=="") { return null; }
            var temp = api.World.GridRecipes.Where(x => x.Output.Code.ToString() == recipecode);
            if (temp == null || temp.Count() == 0) { return null; }
            List<GridRecipe> recipes = temp.ToList<GridRecipe>();

            return recipes;
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block.Attributes == null) { return; }
            rmInputFace = BlockFacing.FromCode(Block.Attributes["inputFace"].AsString("up"));
            fgOutputFace = BlockFacing.FromCode(Block.Attributes["outputFace"].AsString("down"));
            rmInputFace = OrientFace(Block.Code.ToString(), rmInputFace);
            fgOutputFace = OrientFace(Block.Code.ToString(), fgOutputFace);
        }
        protected override void DoDeviceStart()
        {
            if (!IsPowered) { deviceState = enDeviceState.POWERHOLD; return; }
            if (!IsOn) { currentrecipecode = ""; return; }
            deviceState = enDeviceState.IDLE;
            CheckForRecipe();
            
            if (currentrecipecode!=""&&CurrentRecipes!=null&&CurrentRecipes.Count<0)
            {
                bool canstart = TryTakeMaterials();
                if (canstart)
                {
                    deviceState = enDeviceState.RUNNING;
                    starttime = Api.World.Calendar.TotalHours;

                    return;
                }
                else
                {
                    deviceState = enDeviceState.MATERIALHOLD;
                    return;
                }
            }


        }
        protected override double completetime => starttime + processingTime;
        protected override void UsePower()
        {

            if (!IsOn) { return; }
            if (deviceState == enDeviceState.POWERHOLD && IsPowered) { deviceState = enDeviceState.IDLE; MarkDirty(); return; }
            if (deviceState == enDeviceState.WARMUP) { deviceState = enDeviceState.IDLE; MarkDirty(); return; }
            if (currentrecipecode == "") { deviceState = enDeviceState.IDLE; }
            if (deviceState == enDeviceState.IDLE) { DoDeviceStart(); return; }
            if (deviceState == enDeviceState.MATERIALHOLD && CurrentRecipes != null && TryTakeMaterials())
            {
                deviceState = enDeviceState.RUNNING;
                
                starttime = Api.World.Calendar.TotalHours;
                MarkDirty();
                return;
            }
            if ((deviceState == enDeviceState.RUNNING && Api.World.Calendar.TotalHours > completetime) || deviceState == enDeviceState.WAITOUTPUT)
            {
                DoDeviceComplete();
            }
        }
        public void SetCurrentItem(string toitem)
        {
            //if (Api is ICoreClientAPI) {  return; }
            if (deviceState == enDeviceState.RUNNING || deviceState == enDeviceState.WAITOUTPUT)
            {
                PlaySound(Api, "sounds/error", Pos);
                return;
            }
            currentrecipecode = toitem;
            PlaySound(Api, "sounds/filterset", Pos);
            return;
        }
        public void HaltProduction()
        {
            currentrecipecode = "";
            
            deviceState = enDeviceState.WARMUP;
            PlaySound(Api, "sounds/clearfilter", Pos);
        }

        protected override void DoDeviceComplete()
        {
            if (TryOutputProduct()) { deviceState = enDeviceState.IDLE; }
            else { deviceState = enDeviceState.WAITOUTPUT; }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (CurrentRecipes != null) { tree.SetString("currentRecipe", currentrecipecode); }

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            currentrecipecode = tree.GetString("currentRecipe", "");

        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (deviceState == enDeviceState.RUNNING)
            {
                // started at 5 complete at 10 takes 
                double totaltime = completetime - starttime;
                double elapsedtime = Api.World.Calendar.TotalHours - starttime;
                if (totaltime > 0.1)
                {
                    double pct = (elapsedtime) / totaltime;
                    int percent = (int)(pct * 100);
                    if (percent > 0)
                    {
                        dsc.AppendLine(percent + "% complete");
                    }
                }
            }
        }

        protected virtual bool CheckForRecipe()
        {
            BlockPos checkpos = Pos.Copy().Offset(BlockFacing.UP);
            BlockEntityGenericTypedContainer gtc = Api.World.BlockAccessor.GetBlockEntity(checkpos) as BlockEntityGenericTypedContainer;
            if (gtc == null||gtc.Inventory==null|| gtc.Inventory.Empty) { return false; }
            if (gtc.Inventory[0] == null || gtc.Inventory[0].Empty || gtc.Inventory[0].Itemstack == null || gtc.Inventory[0].Itemstack.StackSize == 0) { return false; }
            currentrecipes= GetRecipeForItem(Api,gtc.Inventory[0].Itemstack.Collectible.Code.ToString());
            if (currentrecipes == null || currentrecipes.Count() == 0) { return false; }
            currentrecipecode = gtc.Inventory[0].Itemstack.Collectible.Code.ToString();
            return true;
        }
        protected virtual bool TryTakeMaterials()
        {
            if (currentrecipecode == "") { return false; }
            if (CurrentRecipes == null || CurrentRecipes.Count == 0) { return false; }
            BlockPos checkpos = Pos.Copy().Offset(rmInputFace);
            //Check for a container at the rminputpos
            var checkblock = Api.World.BlockAccessor.GetBlockEntity(checkpos) as IBlockEntityContainer;
            if (checkblock == null) { return false; }
            if (checkblock.Inventory.Empty) { return false; }

            //TODO
            //have to iterate every recipe, and ingredient through every available itemslot
            //will also need to track the source itemslots to see how many where used
            //then relieve the inventory and return true
            bool completed = false;
            
            foreach (GridRecipe recipe in CurrentRecipes)
            {
                bool thiscompleted=true;
                //need to make a copy of the original inventory in case the recipe fails
                DummyInventory di = new DummyInventory(Api, checkblock.Inventory.Count());
                for (int c= 0; c < di.Count(); c++){
                    if (checkblock.Inventory[c] != null && !checkblock.Inventory[c].Empty && checkblock.Inventory[c].Itemstack != null)
                    {
                        di[c].Itemstack = new ItemStack(checkblock.Inventory[c].Itemstack.Collectible, checkblock.Inventory[c].Itemstack.StackSize);
                    }
                }
                //go thru each ingredient of this recipe and check against each slot in our copy, record how much we've used, and stop if we've used enough
                foreach (GridRecipeIngredient ingredient in recipe.Ingredients.Values)
                {
                    int ingredientused = ingredient.Quantity;
                    foreach (ItemSlot slot in checkblock.Inventory)
                    {
                        if (ingredient.SatisfiesAsIngredient(slot.Itemstack))
                        {
                            //tools shouldn't be used up
                            if (ingredient.IsTool)
                            {
                                ingredientused = 0;
                                break;
                            }
                            //only use as much as is needed and as much as is available
                            int amounttouse = Math.Min(ingredientused, slot.Itemstack.StackSize);
                            slot.Itemstack.StackSize -= amounttouse;
                            ingredientused -= amounttouse;
                            if (ingredientused == 0) { break; }
                        }
                    }
                    if (ingredientused>0) { thiscompleted = false;break; }
                }
                if (thiscompleted)
                {
                    //TODO Mark as ok, relieve actual inventory etc
                    for (int c = 0; c < di.Count(); c++)
                    {
                        if (di[c] == null || di[c].Empty || di[c].Itemstack == null || di[c].Itemstack.StackSize == 0)
                        {
                            checkblock.Inventory[c].Itemstack = null;
                            
                        }
                        else
                        {
                            checkblock.Inventory[c].Itemstack.StackSize = di[c].Itemstack.StackSize;
                        }
                        checkblock.Inventory[c].MarkDirty();
                    }
                    
                    completed = true;
                    break;
                }
                //at this point this recipe has failed and the next recipe will try and run
            }
            

            return completed;
        }

        protected virtual bool TryOutputProduct()
        {
            return false;
        }
    }
}