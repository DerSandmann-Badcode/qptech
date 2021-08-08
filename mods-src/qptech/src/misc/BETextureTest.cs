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
using Vintagestory.API.Util;


namespace qptech.src
{
    public class BETextureTest : BlockEntity,ITexPositionSource
    {
        List<string> gaugetextures;
        int texno = 0;
        float pcttracker = 0.5f;
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                //capi.BlockTextureAtlas.Positions[Block.Textures[path].Baked.TextureSubId];
                return capi.BlockTextureAtlas.Positions[Block.Textures[gaugetextures[texno]].Baked.TextureSubId];
            }
        }
        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;
        ICoreClientAPI capi;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
            }
            gaugetextures = new List<string>();
            gaugetextures.Add("roundgauge-0");
            gaugetextures.Add("roundgauge-25");
            gaugetextures.Add("roundgauge-50");
            gaugetextures.Add("roundgauge-75");
            gaugetextures.Add("roundgauge-100");
            gaugetextures.Add("roundgauge-100");
            RegisterGameTickListener(OnTick, 100);

        }

        public void OnTick(float dt)
        {

            pcttracker = 0;

            BlockPos bp = Pos.Copy().Offset(BlockFacing.DOWN);

            BlockEntity checkblock = Api.World.BlockAccessor.GetBlockEntity(bp);
            var bee = checkblock as BEElectric;
            if (bee != null)
            {
                if (bee.IsOn)
                {
                    pcttracker = bee.CapacitorPercentage;
                    if (pcttracker > 1) { pcttracker = 1; }
                    if (pcttracker < 0) { pcttracker = 0; }
                }
            }

            int newtexno = (int)((float)(gaugetextures.Count-1) * pcttracker);
            
            if (newtexno != texno)
            {
                texno = newtexno;
                this.MarkDirty(true);
            }
           
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.Append(pcttracker.ToString()+" "+texno.ToString());

            //dsc.AppendLine("IN:" + inputConnections.Count.ToString() + " OUT:" + outputConnections.Count.ToString());
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            
            Shape shape = capi.TesselatorManager.GetCachedShape(new AssetLocation("machines:block/metal/electric/roundgauge0"));
            MeshData meshdata;
            capi.Tesselator.TesselateShape("roundgauge0", shape, out meshdata, this);
            mesher.AddMeshData(meshdata);
            return true;
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);


            //if (type == null) type = defaultType; // No idea why. Somewhere something has no type. Probably some worldgen ruins
            //texno = tree.GetInt("texno");
            
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            //tree.SetInt("texno", texno);
            
        }
    }

    
    public class TextureTestLoader : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("BETextureTest", typeof(BETextureTest));
        }
    }
}
