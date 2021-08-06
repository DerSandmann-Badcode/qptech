﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace qptech.src
{
    class EForgeContentsRenderer : IRenderer, ITexPositionSource
    {
        private ICoreClientAPI capi;
        private BlockPos pos;
        
        MeshRef workItemMeshRef;
        MeshRef elementMeshRef;
        string elementShapeName = "";
        
        //MeshRef coalQuadRef;


        ItemStack stack;
        float fuelLevel;
        bool burning;

        //TextureAtlasPosition coaltexpos;
        //TextureAtlasPosition elementtexpos;

        int textureId;


        string tmpMetal;
        ITexPositionSource tmpTextureSource;

        Matrixf ModelMat = new Matrixf();



        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange
        {
            get { return 24; }
        }

        public Size2i AtlasSize
        {
            get { return capi.BlockTextureAtlas.Size; }
        }

        public TextureAtlasPosition this[string textureCode]
        {
            get { return tmpTextureSource[tmpMetal]; }
        }




        public EForgeContentsRenderer(BlockPos pos, ICoreClientAPI capi,string elementName)
        {
            this.pos = pos;
            this.capi = capi;
            elementShapeName = elementName;
            Block elementblock = capi.World.GetBlock(new AssetLocation(elementName));
            elementMeshRef = capi.Render.UploadMesh(capi.TesselatorManager.GetDefaultBlockMesh(elementblock)); 
            
        }

        public void SetContents(ItemStack stack, float fuelLevel, bool burning, bool regen)
        {
            this.stack = stack;
            this.fuelLevel = fuelLevel;
            this.burning = burning;

            if (regen) RegenMesh();
        }


        void RegenMesh()
        {
            workItemMeshRef?.Dispose();
            workItemMeshRef = null;

            //RegenElementMesh();

            if (stack == null) return;

            Shape shape;

            tmpMetal = stack.Collectible.LastCodePart();
            MeshData mesh = null;

            string firstCodePart = stack.Collectible.FirstCodePart();
            if (firstCodePart == "metalplate")
            {
                tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("platepile")));
                shape = capi.Assets.TryGet("shapes/block/stone/forge/platepile.json").ToObject<Shape>();
                textureId = tmpTextureSource[tmpMetal].atlasTextureId;
                capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);

            }
            else if (firstCodePart == "workitem")
            {
                MeshData workItemMesh = ItemWorkItem.GenMesh(capi, stack, ItemWorkItem.GetVoxels(stack), out textureId);
                workItemMesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
                workItemMesh.Translate(0, -9f / 16f, 0);
                workItemMeshRef = capi.Render.UploadMesh(workItemMesh);
            }
            else if (firstCodePart == "ingot")
            {
                tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("ingotpile")));
                shape = capi.Assets.TryGet("shapes/block/stone/forge/ingotpile.json").ToObject<Shape>();
                textureId = tmpTextureSource[tmpMetal].atlasTextureId;
                capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);
            }
            else if (stack.Collectible.Attributes?.IsTrue("forgable") == true)
            {
                if (stack.Class == EnumItemClass.Block)
                {
                    mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                    textureId = capi.BlockTextureAtlas.AtlasTextureIds[0];
                }
                else
                {
                    capi.Tesselator.TesselateItem(stack.Item, out mesh);
                    textureId = capi.ItemTextureAtlas.AtlasTextureIds[0];
                }

                ModelTransform tf = stack.Collectible.Attributes["inForgeTransform"].AsObject<ModelTransform>();
                if (tf != null)
                {
                    tf.EnsureDefaultValues();
                    mesh.ModelTransform(tf);
                }
            }

            if (mesh != null)
            {
                //mesh.Rgba2 = null;
                workItemMeshRef = capi.Render.UploadMesh(mesh);
            }
        }

       

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            

            IRenderAPI rpi = capi.Render;
            IClientWorldAccessor worldAccess = capi.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            IStandardShaderProgram prog = rpi.StandardShader;
            prog.Use();
            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaTint = ColorUtil.WhiteArgbVec;
            prog.DontWarpVertices = 0;
            prog.AddRenderFlags = 0;
            prog.ExtraGodray = 0;
            prog.OverlayOpacity = 0;


            if (stack != null && workItemMeshRef != null)
            {
                int temp = (int)stack.Collectible.GetTemperature(capi.World, stack);

                Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
                float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(temp);
                int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);

                prog.NormalShaded = 1;
                prog.RgbaLightIn = lightrgbs;
                prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], extraGlow / 255f);

                prog.ExtraGlow = extraGlow;
                prog.Tex2D = textureId;
                prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y + 10 / 16f + fuelLevel * 0.65f, pos.Z - camPos.Z).Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

                rpi.RenderMesh(workItemMeshRef);
                
            }

            if (fuelLevel > 0)
            {
                Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);

                if (burning)
                {
                    float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(1200);
                    prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], 1);
                }
                else
                {
                    prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
                }

                prog.NormalShaded = 0;
                prog.RgbaLightIn = lightrgbs;

                prog.ExtraGlow = burning ? 255 : 0;

                // The coal or embers
               // rpi.BindTexture2d(burning ? elementtexpos.atlasTextureId : coaltexpos.atlasTextureId);

                prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y , pos.Z - camPos.Z).Values;
                //prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y + 10 / 16f + fuelLevel * 0.65f, pos.Z - camPos.Z).Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                rpi.RenderMesh(elementMeshRef);
                //rpi.RenderMesh(ispower ? elementMeshRef : metalbarQuadRef);

            }


            prog.Stop();
        }



        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            //elementMeshRef?.Dispose();
           //coalQuadRef?.Dispose();
            workItemMeshRef?.Dispose();
        }
    }
}
