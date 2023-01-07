using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace RustAndRails.src
{
	public class MountableEntityBase: Entity, IRenderer, IMountableSupplier
	{
		public EntitySeat Seat;
		public float MountOffsetY = 0.10f;
		public float MountOffsetDistFront = 0f;

		public MountableEntityBase()
		{
			this.Seat = new EntitySeat(this, this.MountOffsetDistFront, this.MountOffsetY);
		}

		public Vec3f GetMountOffset(Entity entity)
		{
			if (Seat.MountingEntity == entity)
			{
				return new Vec3f(
					Seat.MountOffsetDist * (float)-Math.Cos(Pos.Yaw),
					Seat.MountOffsetY,
					Seat.MountOffsetDist * (float)Math.Sin(Pos.Yaw)
				);
			}
			return null;
		}

		public bool IsMountedBy(Entity entity)
		{
			return Seat.MountingEntity == entity;
		}

		public double RenderOrder => 0;
		public int RenderRange => 999;

		public void OnRenderFrame(float dt, EnumRenderStage stage)
		{

		}

		public void Dispose()
		{

		}

		public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
		{
			base.Initialize(properties, api, InChunkIndex3d);

			// This will cause the client to remount when loading back in; But should it?
			// What if someone else mounts the vehicle after you've logged out
			//if (Seat.MountingEntityForLoad != 0 && Seat.MountingEntity == null)
			//{
			//	var entity = api.World.GetEntityById(Seat.MountingEntityForLoad) as EntityAgent;
			//	if (entity != null)
			//	{
			//		if (api is ICoreClientAPI)
			//		{
			//			ModNetwork.Client.ClientSendMountPacket(this.EntityId);
			//		}
			//	}
			//}
		}

		public override void FromBytes(BinaryReader reader, bool isSync)
		{
			base.FromBytes(reader, isSync);
			try
			{
				Seat.MountingEntityForLoad = reader.ReadInt64();
			}
			catch
			{
				// This should only happen on pre-existing worlds
				// prior to cart riding
			}
		}

		public override void ToBytes(BinaryWriter writer, bool forClient)
		{
			base.ToBytes(writer, forClient);
			writer.Write(Seat.MountingEntity?.EntityId ?? 0);
		}
	}
}
