﻿using System;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace RustAndRails.src
{
    public static class ModNetwork
    {
        public static ServerChannel Server;
        public static ClientChannel Client;
        public static string NetworkName = "RustAndRails";
    }

    public class ClientChannel
    {
        readonly ICoreClientAPI capi;
        readonly IClientNetworkChannel clientChannel;

        public ClientChannel(ICoreClientAPI api)
        {
            capi = api;

            clientChannel =
                api.Network.RegisterChannel(ModNetwork.NetworkName)
                .RegisterMessageType(typeof(PacketClientToServerMountVehicle))
                .RegisterMessageType(typeof(PacketServerToClientMountVehicleSuccess))
                .RegisterMessageType(typeof(PacketClientToServerUnmountVehicle))
                .SetMessageHandler<PacketServerToClientMountVehicleSuccess>(ClientOnMountSuccessPacket);
            ;
        }

        public void ClientSendMountPacket(long entityId)
        {
            clientChannel.SendPacket(new PacketClientToServerMountVehicle()
            {
                EntityId = entityId
            });
        }

        public void ClientSendUnmountPacket(long entityId)
        {
            clientChannel.SendPacket(new PacketClientToServerUnmountVehicle()
            {
                EntityId = entityId
            });
        }

        private void ClientOnMountSuccessPacket(PacketServerToClientMountVehicleSuccess packet)
        {
            Entity entity = capi.World.GetEntityById(packet.EntityId);

            if (entity != null && entity is MountableEntityBase)
            {
                MountableEntityBase mountableEntity = (MountableEntityBase)entity;
                IPlayer player = capi.World.Player;
                player.Entity.TryMount(mountableEntity.Seat);
            }
        }
    }

    public class ServerChannel
    {
        readonly ICoreServerAPI sapi;
        readonly IServerNetworkChannel serverChannel;

        public ServerChannel(ICoreServerAPI api)
        {
            sapi = api;

            serverChannel = api.Network.RegisterChannel(ModNetwork.NetworkName)
                .RegisterMessageType(typeof(PacketClientToServerMountVehicle))
                .RegisterMessageType(typeof(PacketServerToClientMountVehicleSuccess))
                .RegisterMessageType(typeof(PacketClientToServerUnmountVehicle))
                .SetMessageHandler<PacketClientToServerMountVehicle>(ServerOnMountPacket)
                .SetMessageHandler<PacketClientToServerUnmountVehicle>(ServerOnUnmountPacket);
        }

        private void ServerOnUnmountPacket(IPlayer fromPlayer, PacketClientToServerUnmountVehicle packet)
        {
            Entity entity = sapi.World.GetEntityById(packet.EntityId);
            if (entity != null && entity is MountableEntityBase)
            {
                MountableEntityBase mountableEntity = (MountableEntityBase)entity;
                if (mountableEntity.Seat.MountingEntity != null || mountableEntity.Seat.MountingEntity?.EntityId == fromPlayer.Entity.EntityId)
                {
                    fromPlayer.Entity.TryUnmount();
                }
            }
        }

        private void ServerOnMountPacket(IPlayer fromPlayer, PacketClientToServerMountVehicle packet)
        {
            Entity entity = sapi.World.GetEntityById(packet.EntityId);

            if (entity != null && entity is MountableEntityBase)
            {
                MountableEntityBase mountableEntity = (MountableEntityBase)entity;
                bool successfulMount = false;

                successfulMount = fromPlayer.Entity.TryMount(mountableEntity.Seat);

                if (successfulMount)
                {
                    serverChannel.SendPacket(new PacketServerToClientMountVehicleSuccess()
                    {
                        EntityId = packet.EntityId
                    }, fromPlayer as IServerPlayer);
                }
            }
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PacketClientToServerMountVehicle
    {
        public long EntityId;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PacketClientToServerUnmountVehicle
    {
        public long EntityId;
        public byte Seat;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PacketServerToClientMountVehicleSuccess
    {
        public long EntityId;
    }
}
