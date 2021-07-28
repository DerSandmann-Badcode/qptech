﻿using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace qptech.src
{
    class BEBMPMotor : BEBehaviorMPRotor
    {
        BEMPMotor motor {get => Blockentity as BEMPMotor; }

        protected override float Resistance => ResistanceLink;
        protected override double AccelerationFactor => AccelerationFactorLink;
        protected override float TargetSpeed => TargetSpeedLink;
        protected override float TorqueFactor => TorqueFactorLink;

        public float ResistanceLink { get; set; }
        public double AccelerationFactorLink { get; set; }
        public float TargetSpeedLink { get; set; }
        public float TorqueFactorLink { get; set; }

        public float OnSpeed { get; set; } = 1.0f;
        public float OnTorque { get; set; } = 0.5f;

        public BEBMPMotor(BlockEntity blockentity) : base(blockentity)
        {
            Blockentity = blockentity;
            ownFacing = BlockFacingExt.FromIndex(ownFacing.Index >= 3 ? 0 : ownFacing.Index + 1);
            OutFacingForNetworkDiscovery = ownFacing;
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            ResistanceLink = 0.5f;
            AccelerationFactorLink = 0.9;
            TargetSpeedLink = 0.0f;
            TorqueFactorLink = 0.0f;

            if (Block?.Attributes != null)
            {
                OnSpeed = Block.Attributes["onSpeed"].AsFloat(OnSpeed);
                OnTorque = Block.Attributes["onTorque"].AsFloat(OnTorque);
            }

            switch (BlockFacing.FromCode(Block.Variant["side"]).Index)
            {
                case 0:
                    AxisSign = new int[] { 1, 0, 0 };
                    break;
                case 1:
                    AxisSign = new int[] { 0, 0, -1 };
                    break;
                case 2:
                    AxisSign = new int[] { 1, 0, 0 };
                    break;
                case 3:
                    AxisSign = new int[] { 0, 0, -1 };
                    break;
                default:
                    break;
            }

            Blockentity.RegisterGameTickListener(UpdateTF, 75);
            Blockentity.RegisterGameTickListener(UpdateMP, 1000);
        }

        public void UpdateTF(float dt)
        {
            switch (motor.DeviceState)
            {
                case BEEBaseDevice.enDeviceState.WARMUP:
                case BEEBaseDevice.enDeviceState.IDLE:
                case BEEBaseDevice.enDeviceState.MATERIALHOLD:
                case BEEBaseDevice.enDeviceState.ERROR:
                    TargetSpeedLink = TargetSpeedLink > 0 ? TargetSpeedLink - 0.01f : 0;
                    TorqueFactorLink = TorqueFactorLink > 0 ? TorqueFactorLink - 0.01f : 0;
                    break;
                case BEEBaseDevice.enDeviceState.RUNNING:
                    TargetSpeedLink = OnSpeed;
                    TorqueFactorLink = OnTorque;
                    motor.UsePowerP();
                    break;
                default:
                    break;
            }
        }

        public void UpdateMP(float dt)
        {
            network?.updateNetwork(manager.getTickNumber());
        }
    }
}
