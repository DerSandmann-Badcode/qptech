﻿using System;
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
using Vintagestory.API.Util;

namespace qptech.src
{
    //Device to use up electricity
    //intermediate class, shouldn't generally be used
    public class BEEBaseDevice:BEElectric,IConduit
    {
        
        public enum enDeviceState { IDLE, RUNNING, WARMUP, MATERIALHOLD, ERROR, POWERHOLD,PROCESSHOLD,WAITOUTPUT }
        ILoadedSound ambientSound;
        string runsound = "";
        protected int requiredFlux = 1;     //how much TF to run
        protected double processingTime = 1000; //how many ticks for process to run
        protected const int clientplaysound = 999900001;
        protected string animationCode = "";
        protected string animation = "";
        protected float runAnimationSpeed = 1;
        protected virtual double completetime=>processingTime+starttime;
        protected double starttime;
        public int RequiredFlux { get { return requiredFlux; } }
        //public bool IsPowered { get { return capacitor >= requiredFlux; } }
        float soundlevel = 0f;
        bool alreadyPlayedSound = false;
        bool loopsound = false;
        int soundoffdelaycounter = 0;
        
        public virtual float SoundLevel
        {
            get { return soundlevel; }
        }
        protected enDeviceState deviceState = enDeviceState.WARMUP;
        public enDeviceState DeviceState { get { return deviceState; } }
        public override int RequestPower()
        {
            if (!isOn) { return 0; }
            return usePower;
        }
        public override int ReceivePowerOffer(int amt)
        {
            lastPower = Math.Min(amt, usePower); MarkDirty(true);
            if (!IsOn||DeviceState != enDeviceState.RUNNING) { return 0; }
            
            return lastPower;
        }
        public override void OnTick(float par)
        {
            base.OnTick(par);
            if (deviceState == enDeviceState.RUNNING) {
                DoRunningParticles();
                
            }
            ToggleAmbientSounds(shouldAnimate);
            if (Api is ICoreClientAPI) {
                DoAnimations();
                return;
            }
            UsePower();
            
        }
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            starttime = 0;
            
            if (Block.Attributes != null) {
                requiredFlux = Block.Attributes["requiredFlux"].AsInt(requiredFlux);
                processingTime = Block.Attributes["processingTime"].AsDouble(processingTime);
                animationCode = Block.Attributes["animationCode"].AsString(animationCode);
                animation = Block.Attributes["animation"].AsString(animation);
                runAnimationSpeed = Block.Attributes["runAnimationSpeed"].AsFloat(runAnimationSpeed);
                runsound = Block.Attributes["runsound"].AsString(runsound);
                soundlevel = Block.Attributes["soundlevel"].AsFloat(soundlevel);
                loopsound = Block.Attributes["loopsound"].AsBool(loopsound);
            }
            if (api.World.Side == EnumAppSide.Client && animationCode != "")
            {
                float rotY = Block.Shape.rotateY;
                animUtil.InitializeAnimator(this.ToString(), new Vec3f(0, rotY, 0));
            }
            
        }

        protected virtual void DoRunningParticles()
        {

        }
        protected virtual void UsePower()
        {
            if (!IsOn) { return; }
            else if (lastPower<usePower && DeviceState == enDeviceState.RUNNING)
            {
                deviceState = enDeviceState.POWERHOLD;
                MarkDirty();
            }
            else if (lastPower>=usePower && DeviceState == enDeviceState.POWERHOLD)
            {
                deviceState = enDeviceState.RUNNING;
                MarkDirty();
            }
            else if (DeviceState == enDeviceState.IDLE||DeviceState==enDeviceState.MATERIALHOLD)
            {
                DoDeviceStart();
                
            }
            
            else if (DeviceState == enDeviceState.RUNNING) { DoDeviceProcessing(); } 
            if (DeviceState == enDeviceState.WARMUP) { deviceState = enDeviceState.IDLE; }
        }
        protected virtual void ResetTimers()
        {
            
            starttime = Api.World.ElapsedMilliseconds;
        }
        protected virtual void DoDeviceStart()
        {

            if (Api.World.Side is EnumAppSide.Client) { return; }
            if (!IsPowered) { DoFailedStart(); return; }
            ResetTimers();
            
            if (deviceState == enDeviceState.IDLE)
            {
                
               if (IsPowered)
                {
               
                    deviceState = enDeviceState.RUNNING;
                }
                
            }
            this.MarkDirty(true);

        }

        protected virtual void DoDeviceProcessing()
        {
            if (Api.World.ElapsedMilliseconds>=completetime)
            {
                DoDeviceComplete();
                return;
            }
            if (!IsPowered)
            {
                DoFailedProcessing();
                return;
            }
            
            
            
        }
        //can do some feedback if device can't run
        protected virtual void DoFailedStart()
        {
            deviceState = enDeviceState.IDLE;
            ResetTimers();
        }
        //feedback if device cannot process
        protected virtual void DoFailedProcessing()
        {
            ResetTimers();
        }
        //Do whatever needs doing on a successful cycle
        protected virtual void DoDeviceComplete()
        {
            deviceState = enDeviceState.IDLE;
            ResetTimers();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            
            deviceState = (enDeviceState)tree.GetInt("deviceState");
            starttime = tree.GetDouble("starttime");
            
            if (Api!=null&&  starttime > Api.World.ElapsedMilliseconds) { starttime = Api.World.ElapsedMilliseconds; }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            
            tree.SetInt("deviceState", (int)deviceState);
            tree.SetDouble("starttime", starttime);
        }
        protected BlockEntityAnimationUtil animUtil
        {
            get
            {
                if (GetBehavior<BEBehaviorAnimatable>() == null) { return null; }
                return GetBehavior<BEBehaviorAnimatable>().animUtil;
            }
        }
        protected bool animationIsRunning = false;
        protected virtual bool shouldAnimate => (IsOn&&deviceState == enDeviceState.RUNNING) && animation != "";
        int idlecount = 0;
        protected virtual void DoAnimations()
        {
            
            if (shouldAnimate && !animationIsRunning)
            {

                var meta = new AnimationMetaData() { Animation = animation, Code = animationCode, AnimationSpeed = runAnimationSpeed, EaseInSpeed = 1, EaseOutSpeed = 2, Weight = 1, BlendMode = EnumAnimationBlendMode.Add };
                animUtil.StartAnimation(meta);
                animUtil.StartAnimation(new AnimationMetaData() { Animation = animation, Code = animationCode, AnimationSpeed = runAnimationSpeed, EaseInSpeed = 1, EaseOutSpeed = 1, Weight = 1, BlendMode = EnumAnimationBlendMode.Average });
                animationIsRunning = true;
                idlecount = 0;
            }
            else if (!shouldAnimate && animationIsRunning && idlecount < 5)
            {
                idlecount++;
            }
            else if (!shouldAnimate && animationIsRunning)
            {
                animationIsRunning = false;
                idlecount = 0;
                animUtil.StopAnimation(animationCode);
            }

        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            
            dsc.Append(" "+DeviceState.ToString());
            dsc.AppendLine("");
            base.GetBlockInfo(forPlayer, dsc);
        }

        public virtual int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace)
        {
            return 0;
        }
        public override void CleanBlock()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.CleanBlock();
        }
        public override void OnBlockRemoved()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.OnBlockRemoved();
        }
        public override void OnBlockUnloaded()
        {
            ambientSound?.Stop();
            ambientSound?.Dispose();
            ambientSound = null;
            base.OnBlockUnloaded();
        }
        public void ToggleAmbientSounds(bool on)
        {
            if (Api.Side != EnumAppSide.Client) return;
            if (runsound == "" || SoundLevel == 0) { return; }
            if (on)
            {
                
                if (ambientSound == null || !ambientSound.IsPlaying && (!alreadyPlayedSound||loopsound))
                {
                    ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation(runsound),
                        ShouldLoop = loopsound,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = SoundLevel,
                        Range = 10
                    });
                    soundoffdelaycounter = 0;
                    ambientSound.Start();
                    alreadyPlayedSound = true;
                }
            }
            else if (loopsound && soundoffdelaycounter<10)
            {
                soundoffdelaycounter++;
            }
            else 
            {
                ambientSound?.Stop();
                ambientSound?.Dispose();
                ambientSound = null;
                alreadyPlayedSound = false;
            }

        }
        public override string GetStatusUI()
        {
            return base.GetStatusUI();
        }
        public static void PlaySound(ICoreAPI Api, string soundname, BlockPos pos)
        {
            if ((Api is ICoreClientAPI))
            {
                ILoadedSound pambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation(soundname),
                    ShouldLoop = false,
                    Position = pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = true,
                    Volume = 2,
                    Range = 15
                });

                pambientSound.Start();
            }
            else
            {
                byte[] data = SerializerUtil.Serialize<string>(soundname);
                (Api as ICoreServerAPI).Network.BroadcastBlockEntityPacket(pos.X, pos.Y, pos.Z, clientplaysound, data);
            }
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == clientplaysound)
            {
                string soundname = "";
                try
                {
                    soundname = SerializerUtil.Deserialize<string>(data);
                }
                catch
                {
                    return;
                }
                PlaySound(Api,soundname,Pos);
            }
        }
    }
   
    public interface IConduit
    {
        int ReceiveItemOffer(ItemSlot offerslot, BlockFacing onFace);
        
    }

    
}
