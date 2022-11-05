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
        float rotationtracker;
        float rotationspeed = 0f;
        bool passable = false;
        public virtual bool Passable => passable;
        public virtual float RotationSpeed => rotationspeed;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
        }

        public virtual void ClientTick(float dt)
        {

        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {

            //Mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, rotationspeed * GameMath.DEG2RAD, 0);
            //rotationtracker += rotationspeed;
            return base.OnTesselation(mesher, tesselator);
        }

        public virtual void SetPassable(bool ispass)
        {
            passable = ispass;
            if (Api is ICoreClientAPI)
            {
                RegenMesh(Api as ICoreClientAPI);
            }
            MarkDirty(true);
        }

        public virtual void Sync(BEFunctionChiseled toblock)
        {
            if (toblock != null)
            {
                rotationspeed = toblock.RotationSpeed;
                rotationtracker = 0;
            }
            if (Api is ICoreClientAPI)
            {
                RegenMesh(Api as ICoreClientAPI);
            }
        }
        
        
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("passable", Passable);
            
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            passable = tree.GetBool("passable", false);
        }
    }
}
