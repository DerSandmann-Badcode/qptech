﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using ProtoBuf;

namespace chisel.src
{
    class ChiselToolLoader : ModSystem
    {
        public static ChiselToolLoader loader;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        string serverconfigfile = "qpchiseltoolserversettings.json";
        public static ChiselToolServerData serverconfig;
        public INetworkChannel chiselnet;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            if (api is ICoreClientAPI)
            {
                capi = api as ICoreClientAPI;
                chiselnet= capi.Network.RegisterChannel("pantograph").RegisterMessageType(typeof(DoorData));
            }
            else if (api is ICoreServerAPI)
            {
                sapi = api as ICoreServerAPI;
                chiselnet= sapi.Network.RegisterChannel("pantograph")
                    .RegisterMessageType(typeof(DoorData)).
                    SetMessageHandler<DoorData>(DoorDataHandler); 
                ServerPreStart();
            }
            loader = this;
        }

        void ServerPreStart()
        {
            sapi.RegisterCommand("qpchisel-handplaner-toolusage", "Set how fast hand planer gets damaged when used.", "", CmdSetHandPlanerMultiplier,Privilege.controlserver);
            sapi.RegisterCommand("qpchisel-pantograph-toolusage", "Set how fast hand pantograph gets damaged when used.", "", CmdSetPantographMultiplier, Privilege.controlserver);
            sapi.RegisterCommand("qpchisel-resetdefaults", "Reset chisel tools settings to defaults", "", CmdSetReset, Privilege.controlserver);
            try
            {
                serverconfig = sapi.LoadModConfig<ChiselToolServerData>(serverconfigfile);
            }
            catch
            {


            }
            if (serverconfig == null)
            {
                serverconfig = new ChiselToolServerData();
                sapi.StoreModConfig<ChiselToolServerData>(serverconfig, serverconfigfile);
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("ItemHandPlaner", typeof(ItemHandPlaner));
            api.RegisterItemClass("ItemWedge", typeof(ItemWedge));
            api.RegisterItemClass("ItemPantograph", typeof(ItemPantograph));
            api.RegisterItemClass("ItemPaintBrush", typeof(ItemPaintBrush));
            api.RegisterItemClass("ItemBlockSwapper", typeof(ItemBlockSwapper));
            api.RegisterBlockEntityClass("BEFunctionChiseled", typeof(BEFunctionChiseled));
            api.RegisterBlockClass("BlockFunctionalChiseled", typeof(BlockFunctionalChiseled));
            api.RegisterItemClass("ItemChiselBlockController", typeof(ItemChiselBlockController));
            api.RegisterItemClass("ItemLadderMaker", typeof(ItemLadderMaker));
            api.RegisterItemClass("ItemDoorPart", typeof(ItemDoorPart));
        }

        private void CmdSetHandPlanerMultiplier(IPlayer player, int groupId, CmdArgs args)
        {
            if (sapi == null) { return; }
            if (serverconfig == null) { sapi.BroadcastMessageToAllGroups("QP Chisel Server Config doesn't exist.", EnumChatType.Notification); return; }
            if (args == null || args.Length == 0)
            {
                sapi.SendMessage(player, groupId, "Hand Planer tool usage rate is " + serverconfig.handPlanerBaseDurabilityMultiplier + "(Default is " + ChiselToolServerData.handplanerDefault + ")", EnumChatType.CommandSuccess);
                return;
            }
            float newvalue = args[0].ToFloat(0.125f);
            serverconfig.handPlanerBaseDurabilityMultiplier = newvalue;
            sapi.StoreModConfig<ChiselToolServerData>(serverconfig, serverconfigfile);
            sapi.SendMessage(player, groupId, "Hand Planer tool usage rate SET to " + serverconfig.handPlanerBaseDurabilityMultiplier + "(Default is " + ChiselToolServerData.handplanerDefault + ")", EnumChatType.CommandSuccess);
        }
        private void CmdSetPantographMultiplier(IPlayer player, int groupId, CmdArgs args)
        {
            if (sapi == null) { return; }
            if (serverconfig == null) { sapi.BroadcastMessageToAllGroups("QP Chisel Server Config doesn't exist.", EnumChatType.Notification); return; }
            if (args == null || args.Length == 0)
            {
                sapi.SendMessage(player, groupId, "Pantograph tool usage rate is " + serverconfig.pantographBaseDurabilityMultiplier + "(Default is "+ChiselToolServerData.pantographDefault+")", EnumChatType.CommandSuccess);
                return;
            }
            float newvalue = args[0].ToFloat(0.125f);
            serverconfig.pantographBaseDurabilityMultiplier = newvalue;
            sapi.StoreModConfig<ChiselToolServerData>(serverconfig, serverconfigfile);
            sapi.SendMessage(player, groupId, "Pantograph tool usage rate SET to " + serverconfig.pantographBaseDurabilityMultiplier + "(Default is " + ChiselToolServerData.pantographDefault + ")", EnumChatType.CommandSuccess);
        }
        private void CmdSetReset(IPlayer player, int groupid, CmdArgs args)
        {
            if (sapi == null) { return; }
            if (serverconfig == null) { sapi.BroadcastMessageToAllGroups("QP Chisel Server Config doesn't exist.", EnumChatType.Notification); return; }
            serverconfig.Reset();
            sapi.StoreModConfig<ChiselToolServerData>(serverconfig, serverconfigfile);
            sapi.SendMessage(player, groupid, "QP's Chisel Tools server config has been reset to defaults!", EnumChatType.CommandSuccess);
        }

        //Used by pantograph to transfrom a chiseled block in to a function chiseled block
        private void DoorDataHandler(IPlayer fromplayer, DoorData networkMessage)
        {
            if (sapi == null) { return; }

            sapi.World.BlockAccessor.SetBlock(sapi.World.GetBlock(new AssetLocation("chiseltools:moveablechiseledblock")).BlockId, networkMessage.pos);
            BEFunctionChiseled bfc = sapi.World.BlockAccessor.GetBlockEntity(networkMessage.pos) as BEFunctionChiseled;
            if (bfc == null) { return; }
            bfc.AddState(networkMessage.state, networkMessage.voxeldata, networkMessage.matdata, networkMessage.passable, networkMessage.transparent);
            networkMessage.state = "ORIGINAL";
            bfc.AddState(BEFunctionChiseled.originalblockname, networkMessage.voxeldata, networkMessage.matdata, networkMessage.passable, networkMessage.transparent);
        }
        

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class ChiselToolServerData
        {
            public int handPlanerBaseDurabilityUse = 1;
            public float handPlanerBaseDurabilityMultiplier = handplanerDefault;
            public float pantographBaseDurabilityMultiplier = pantographDefault;
            public int pantographMinimumDamagePerOp = pantographMinimumDamagePerOpDefault;
            public bool enablePantographLeftClickCopy = true;
            public bool pantographInstantMode = true;
            public bool fixedToolWear = false;
            public int paintBrushUseRate = paintBrushUseRateDefault;
            public int paintBrushLiquidMultiplier = paintBrushLiquidMultiplierDefault;
            public static float handplanerDefault = 0.00625f;
            public static float pantographDefault = 0.001f;
            public static bool pantographInstantModeDefault = true;
            public static bool fixedToolWearDefault = false;
            public static int paintBrushUseRateDefault = 10;
            public static int pantographMinimumDamagePerOpDefault = 1;
            public static int paintBrushLiquidMultiplierDefault = 10;
            public static int minimumVoxelsForLadderDefault = 8;
            public int minimumVoxelsForLadder = minimumVoxelsForLadderDefault;
            public void Reset()
            {
                handPlanerBaseDurabilityMultiplier = handplanerDefault;

                pantographBaseDurabilityMultiplier = pantographDefault;
                pantographInstantMode = pantographInstantModeDefault;
                pantographMinimumDamagePerOp = pantographMinimumDamagePerOpDefault;
                paintBrushUseRate = paintBrushUseRateDefault;
                fixedToolWear = fixedToolWearDefault;
                minimumVoxelsForLadder = minimumVoxelsForLadderDefault;
            }
        }
    }
}
