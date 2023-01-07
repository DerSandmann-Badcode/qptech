using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RustAndRails.src
{
	public class EntitySeat : IMountable
	{
		public EntityControls controls = new EntityControls();
		public EntityAgent MountingEntity = null;
		public IMountableSupplier MountSupplier => MountedEntity;
		public readonly MountableEntityBase MountedEntity;
		public readonly float MountOffsetY;
		public readonly float MountOffsetDist;
		public static string MountedEntityIdAttribute = "mountedEntityId";
		internal long MountingEntityForLoad = 0;

		public EntitySeat(MountableEntityBase mountedEntity, float mountOffsetDist, float mountOffsetY)
		{
			controls.OnAction = this.onControls;
			this.MountedEntity = mountedEntity;
			this.MountOffsetY = mountOffsetY;
			this.MountOffsetDist = mountOffsetDist;
		}

		public static IMountable GetMountable(IWorldAccessor world, TreeAttribute tree)
		{
			Entity mountedEntity = world.GetEntityById(tree.GetLong(MountedEntityIdAttribute));
			if (mountedEntity != null && mountedEntity is MountableEntityBase)
			{
				return (mountedEntity as MountableEntityBase).Seat;
			}
			return null;
		}

		public Vec3d MountPosition
		{
			get
			{
				return this.MountedEntity.SidedPos.XYZ.AddCopy(
					MountOffsetDist * -Math.Cos(this.MountedEntity.SidedPos.Yaw),
					MountOffsetY,
					MountOffsetDist * Math.Sin(this.MountedEntity.SidedPos.Yaw)
				);
			}
		}

		public float? MountYaw
		{
			get { return this.MountingEntity.SidedPos.Yaw; }
		}

		public EntityControls Controls
		{
			get { return this.controls; }
		}

		public string SuggestedAnimation
		{
			get { return "sitflooridle"; }
		}

		internal void onControls(EnumEntityAction action, bool on, ref EnumHandling handled)
		{
			if (this.MountedEntity.World.Side == EnumAppSide.Client)
			{
				if (action == EnumEntityAction.Sneak && on)
				{
					this.MountingEntity?.TryUnmount();
					controls.StopAllMovement();
					ModNetwork.Client.ClientSendUnmountPacket(this.MountedEntity.EntityId);
				}
			}
		}

		public void DidUnmount(EntityAgent entityAgent)
		{
			this.MountingEntity = null;
		}

		public void MountableToTreeAttributes(TreeAttribute tree)
		{
			tree.SetString("className", GetType().Name);
			tree.SetLong(MountedEntityIdAttribute, MountedEntity.EntityId);
		}

		public void DidMount(EntityAgent entityAgent)
		{
			if (this.MountingEntity != null && this.MountingEntity != entityAgent)
			{
				this.MountingEntity.TryUnmount();
				return;
			}
			this.MountingEntity = entityAgent;
		}
	}
}
