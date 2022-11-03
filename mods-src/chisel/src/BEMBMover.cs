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
    class BEMBMover : BlockEntityMicroBlock
    {
        float rotationtracker;
        float rotationspeed = 0f;
        public virtual float RotationSpeed => rotationspeed;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is ICoreClientAPI)
            {
                RegisterGameTickListener(ClientTick, 50);
            }
        }

        public virtual void ClientTick(float dt)
        {

            MarkDirty(true);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {

            Mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, rotationspeed * GameMath.DEG2RAD, 0);
            rotationtracker += rotationspeed;
            return base.OnTesselation(mesher, tesselator);
        }
        public virtual void Sync(BEMBMover toblock)
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
        public virtual void AdjustRotationSpeed(float byamt)
        {
            rotationspeed += byamt;
            
        }
        public virtual void SetRotationSpeed(float amt)
        {
            rotationspeed = amt;
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("rotationspeed", RotationSpeed);
            tree.SetFloat("rotationtracker", rotationtracker);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            rotationspeed = tree.GetFloat("rotationspeed", RotationSpeed);
            rotationtracker = tree.GetFloat("rotationtracker", 0);
        }
    }
}
