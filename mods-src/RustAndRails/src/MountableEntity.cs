using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RustAndRails.src
{
    public abstract class MountableEntity : Entity, IMountable, IMountableSupplier
    {
        private List<EntityAgent> Passengers = new List<EntityAgent>();
        public IMountableSupplier MountSupplier => this;
        public virtual int MaximumPassengers => 1;
        public virtual string SuggestedAnimation => "sitflooridle";
        public virtual float? MountYaw => this.SidedPos.Yaw + 1.5f;
        public virtual float MountOffsetY => 0.10f;
        public virtual float MountOffsetDistFront => 0f;
        public virtual Vec3d MountPosition => this.SidedPos.XYZ.AddCopy(
                MountOffsetDistFront * -Math.Cos(this.SidedPos.Yaw),
                MountOffsetY,
                MountOffsetDistFront * Math.Sin(this.SidedPos.Yaw)
            );
        public abstract Vec3f GetMountOffset(Entity entity);

        public EntityControls controls = new EntityControls();

        public MountableEntity()
        {
            controls = new EntityControls
            {
                OnAction = onControls
            };
        }

        public EntityControls Controls
        {
            get { return this.controls; }
        }

        public static string MountedEntityIdAttribute = "mountedEntityId";

        private void AddPassenger(EntityAgent passenger)
        {
            // Sometimes this gets fired multiple times
            if (!Passengers.Contains(passenger))
            {
                Passengers.Add(passenger);
            }
        }

        public bool CanAddPassenger(EntityAgent passenger)
        {
            return !this.Passengers.Any();
        }

        public int PassengerCount()
        {
            return this.Passengers.Count;
        }

        public List<EntityAgent> GetPassengers()
        {
            return this.Passengers;
        }

        private void RemovePassenger(EntityAgent passenger)
        {
            Passengers.Remove(passenger);
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
        }

        internal void onControls(EnumEntityAction action, bool on, ref EnumHandling handled)
        {
            if (this.World.Side == EnumAppSide.Client)
            {
                if (action == EnumEntityAction.Sneak && on)
                {
                    ModNetwork.Client.ClientSendUnmountPacket(this.EntityId);
                }
            }
        }

        public static IMountable GetMountable(IWorldAccessor world, TreeAttribute tree)
        {
            Entity mountedEntity = world.GetEntityById(tree.GetLong(MountedEntityIdAttribute));
            if (mountedEntity is MountableEntity)
            {
                var ee = (MountableEntity)mountedEntity;
                if (ee != null)
                {
                    return ee;
                }
            }
            return null;
        }

        public void MountableToTreeAttributes(TreeAttribute tree)
        {
            tree.SetString("className", GetType().Name);
            tree.SetLong(MountedEntityIdAttribute, this.EntityId);
        }

        public void DidUnmount(EntityAgent entityAgent)
        {
            entityAgent.AddRidingCooldown(3);
            if (this.World.Side.IsServer())
            {
                this.RemovePassenger(entityAgent);
            }
            else
            {
                this.RemovePassenger(entityAgent);
            }
            TryToUnmountToSafeLocation(entityAgent);
        }

        public void TryToUnmountToSafeLocation(EntityAgent entityAgent)
        {
            var test = this.SidedPos.HorizontalAheadCopy(2);
            entityAgent.SidedPos.SetPos(test);
        }

        public void DidMount(EntityAgent entityAgent)
        {
            if (this.World.Side.IsServer())
            {
                this.AddPassenger(entityAgent);
            }
            else
            {
                this.AddPassenger(entityAgent);
            }
        }

        public bool IsMountedBy(Entity entity)
        {
            return this.Passengers.Any(e => e.EntityId == entity.EntityId);
        }

        public void RemoveAllPassengers()
        {
            foreach (var passenger in Passengers.ToList())
            {
                passenger.TryUnmount();
            }
            Passengers.Clear();
        }
    }
}
