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
        public List<GridRecipe> CurrentRecipes
        {
            get
            {

                if (Api != null)
                {
                    return GetRecipeForItem(Api, currentrecipecode);
                }
                return null;
            }

        }

        public static List<GridRecipe> GetRecipeForItem(ICoreAPI api, string recipecode)
        {
            if (api == null||api.World==null||api.World.GridRecipes==null) { return null; }
            
            List<GridRecipe> recipes = api.World.GridRecipes;

            return (List<GridRecipe>)recipes.Where(x => x.Output.Code.ToString() == recipecode);
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

            if (CurrentRecipes == null)
            {
                deviceState = enDeviceState.IDLE;

            }
            else
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


        protected virtual bool TryTakeMaterials()
        {
            return false;
        }

        protected virtual bool TryOutputProduct()
        {
            return false;
        }
    }
}