﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Vintagestory.API.Client;

namespace qptech.src
{
    /// <summary>
    /// Offers electric cooling & heating boosts
    /// </summary>
    class BEEHVAC:BEEBaseDevice
    {
        public enum enHVACType { COOLINGFAN,HEATSINK,HEATER}
        
        RoomRegistry roomReg;
        
        /// <summary>
        /// HVACRegistry should keep a list of all HVAC items and what rooms they are in
        /// main issue will be what to do when rooms are destroyed?
        /// </summary>
        BlockFacing hotside=BlockFacing.FromCode("north");
        BlockFacing coldside=BlockFacing.FromCode("south");
        BlockPos hotPos=> Pos.Copy().Offset(hotside);
        BlockPos coldPos => Pos.Copy().Offset(coldside);
        float poweredcoolingbonus = 50f; //10C bonus
        public float ColdBonus => -poweredcoolingbonus;
        public float HotBonus => poweredcoolingbonus;
        public override void Initialize(ICoreAPI api)
        {
            
            base.Initialize(api);
            hotside= OrientFace(Block.Code.ToShortString(), hotside);
            coldside = OrientFace(Block.Code.ToShortString(), coldside);
            
            RegisterGameTickListener(RoomCheck, 150);
        }


        public void RoomCheck(float par)
        {
            RoomStats.ClearHVAC(this);
            roomReg= Api.ModLoader.GetModSystem<RoomRegistry>();
            Room hotroom= roomReg.GetRoomForPosition(hotPos);
            Room coldroom = roomReg.GetRoomForPosition(coldPos);
            if (hotroom.Location == coldroom.Location) { return; } //HOT & COLD ARE THE SAME SO THIS REALLY DOES NOTHING
            RoomStats hotstats = new RoomStats(this, hotroom);
            hotstats.temperatureChange = HotBonus;
            RoomStats coldstats = new RoomStats(this, coldroom);
            coldstats.temperatureChange = ColdBonus;
            RoomStats.HVACRegistry.Add(hotstats);
            RoomStats.HVACRegistry.Add(coldstats);
            //test code:
            float storageinhot = RoomStats.GetStorageBonus(Api, hotPos);
            float storageincold = RoomStats.GetStorageBonus(Api, coldPos);
            
            
        }
        
        void Cleanup()
        {
            RoomStats.ClearHVAC(this);
        }

        public override void OnBlockRemoved()
        {
            Cleanup();
            base.OnBlockRemoved();
        }
        public override void OnBlockUnloaded()
        {
            Cleanup();
            base.OnBlockUnloaded();
        }
    }
    
    class RoomStats
    {
        public Room room; //which room this applies to
        public BEEHVAC statsource; //which object supplies this bonus
        public float temperatureChange=1; //adjustment to temp in degrees C
        public RoomStats()
        {

        }
        public RoomStats(BEEHVAC beehvac,Room forroom)
        {
            room = forroom;
            statsource = beehvac;
        }
        
        public static float GetStorageBonus(ICoreAPI Api,BlockPos Pos)
        {
            
            if (Api is ICoreClientAPI) { return 0; }
            if (HVACRegistry == null) { return 0; }
            if (HVACRegistry.Count == 0) { return 0; }
            
            RoomRegistry roomReg = Api.ModLoader.GetModSystem<RoomRegistry>();
            if (roomReg == null) { return 0; }
            
            Room room = roomReg.GetRoomForPosition(Pos);
            float bonus = 0;
            var findHVACs = from hvac in HVACRegistry
                            where hvac.room.Location == room.Location
                            select hvac.temperatureChange;


            float addbonus = findHVACs.Sum();
            return bonus+addbonus;
        }
        public static List<RoomStats> HVACRegistry
        {
            get
            {
                if (hvacregistry == null) { hvacregistry = new List<RoomStats>(); }

                return hvacregistry;
            }
        }
        static List<RoomStats> hvacregistry;
        public static void ClearHVAC(BEEHVAC hvac)
        {
            HVACRegistry.RemoveAll(x => x.statsource == hvac);

        }
    }
}
