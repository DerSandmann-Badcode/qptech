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
using qptech.src.itemtransport;

namespace qptech.src
{
    class BEItemScanner: BlockEntityGenericTypedContainer
    {
        MeshData meshdata;
        ICoreClientAPI capi;
        float roty = 0;
        float rotspeed = 0.05f;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Api is ICoreServerAPI) {  }
            else {
                capi = api as ICoreClientAPI;
                RegisterGameTickListener(OnClientTick, 100);
            }
        }
        public void OnClientTick(float dt)
        {
            GenMesh();
            MarkDirty(true);
        }
        public virtual void GenMesh()
        {
            meshdata = null;
            if (Inventory==null||Inventory.Empty) { roty = 0; return; }
            if (capi == null) { roty = 0; return; }
            ItemStack itemstack = Inventory[0].Itemstack;
            if (itemstack == null || (itemstack.Item == null && itemstack.Block == null)) { return; }


            if (itemstack.Class == EnumItemClass.Item)
            {
               capi.Tesselator.TesselateItem(itemstack.Item, out meshdata);

            }
            else
            {
                capi.Tesselator.TesselateBlock(itemstack.Block, out meshdata);
            }

            float[] meshsize = ItemPipe.GetMeshSize(meshdata);

            float scalefactor = Math.Max(Math.Max(meshsize[0], meshsize[1]), meshsize[2]);
            float targetscale = 0.25f;
            if (scalefactor <= 0) { scalefactor = targetscale; }
            else
            {
                scalefactor = targetscale / scalefactor; /// 0.5/1
            }
            Vec3f mid = new Vec3f(0.5f, 0.5f, 0.5f);
            scalefactor = Math.Min(scalefactor, 0.5f);
            meshdata.Scale(mid, scalefactor, scalefactor, scalefactor);
            meshdata.Rotate(mid, 0, roty * GameMath.RAD2DEG, 0);
            meshdata.Translate(new Vec3f(0, 0.1f, 0));
            roty += rotspeed;
            if (roty >= 360) { roty = 360-roty; }
            //Vec3f startv = o.Copy().Offset(outputface.Opposite).ToVec3f();
            
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            
            if (meshdata == null) { return base.OnTesselation(mesher, tessThreadTesselator); }

            try { mesher.AddMeshData(meshdata); }
            catch { return base.OnTesselation(mesher, tessThreadTesselator); }

            return base.OnTesselation(mesher, tessThreadTesselator);
        }
    }

    
}
