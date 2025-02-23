﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;

namespace qptech.src.networks
{
    class FluidNetwork : IFlexNetwork
    {
        Guid networkID;
        public Guid NetworkID => networkID;

        public string ProductID => "FLUID";
        public string fluid = "";
        public string Fluid => fluid;
        int networkCapacity;
        int networkLevel;
        public int NetworkCapacity => networkCapacity;
        public int NetworkLevel => networkLevel;
        bool purge = false;
        List<IFlexNetworkMember> members;
        public FluidNetwork(Guid newid)
        {
            members = new List<IFlexNetworkMember>();
            networkID = newid;
        }
        public List<IFlexNetworkMember> GetMembers()
        {
            if (members == null) { members = new List<IFlexNetworkMember>(); }
            return members;
        }

        public bool JoinNetwork(IFlexNetworkMember member)
        {
            if (member == null) { return false; }
            IFluidNetworkMember fnm = member as IFluidNetworkMember;
            if (fnm == null) { return false; }
            
            if (!GetMembers().Contains(fnm))
            {
                GetMembers().Add(fnm);
            }
            return true;
        }

        public bool MergeWith(Guid newnetworkID)
        {
            return true;
        }

        public void OnTick(float dt)
        {
            //Iterate thru members and average out fluid
            //if fluid level is zero then set all fluid to ""
            networkCapacity = 0;
            networkLevel = 0;
            int maxflow = 50000;
            if (GetMembers().Count == 0) { FlexNetworkManager.DeleteNetwork(NetworkID); return; }
            List<BlockEntity> validoutputs = new List<BlockEntity>();
            List<BlockEntity> validinputs = new List<BlockEntity>();
            
            Item fluiditem = null;
            
            //Do inventory of available fluids, set fluid type
            foreach (IFlexNetworkMember nm in GetMembers().ToArray())
            {
                IFluidNetworkMember fnm = nm as IFluidNetworkMember;
                if (fnm == null) { continue; }
                //Do an inventory of available fluid
                maxflow = Math.Min(maxflow, fnm.FluidRate);
                foreach (BlockEntity inputnode in fnm.InputNodes().ToArray())
                {
                    if (inputnode == null) { continue; }
                    //handle fluidnetworkusers
                    
                    IFluidNetworkUser fnu = inputnode as IFluidNetworkUser;
                    if (fnu != null)
                    {
                        
                        if (fluiditem == null) { fluiditem = fnu.QueryFluid(); }
                        if (fluiditem == null) { continue; } //if both are null no point in continuing
                        int avail = fnu.QueryFluid(fluiditem);
                        if (avail > 0 && !validinputs.Contains(inputnode))
                        {
                            networkLevel += Math.Min(avail,maxflow);
                            validinputs.Add(inputnode);
                        }
                        continue;
                    }
                    //handle tanks
                    IFluidTank inputtank = inputnode as IFluidTank;
                    if (inputtank != null)
                    {
                        if (purge)
                        {
                            inputtank.Purge();
                            continue;
                        }
                        //assign fluid for this cycle if available
                        if (inputtank.CurrentLevel > 0 && inputtank.CurrentItem != null && fluiditem == null)
                        {
                            fluiditem = inputtank.CurrentItem;
                        }
                        if (!validinputs.Contains(inputnode) && inputtank.CurrentLevel > 0 && inputtank.CurrentItem == fluiditem)
                        {

                            validinputs.Add(inputnode);
                            networkLevel += Math.Min(inputtank.CurrentLevel,maxflow);
                        }
                        continue;
                    }
                    BlockEntityBarrel inputbarrel = inputnode as BlockEntityBarrel;
                    if (inputbarrel != null)
                    {
                        
                        if (inputbarrel.CanSeal) { continue; }
                        if (inputbarrel.Inventory[1].Empty) { continue; }
                        /*
                        Offtopic found this id thing, very interesting for future inventory projects (for doing inventory[x])
                        inventory = new InventoryGeneric(2, null, null, (id, self) =>
                        {
                            if (id == 0) return new ItemSlotBarrelInput(self);
                            else return new ItemSlotLiquidOnly(self, 50);
                        });
                        */
                        if (inputbarrel.Inventory[1].Itemstack == null) { continue; }

                        if (inputbarrel.Inventory[1].StackSize > 0 && inputbarrel.Inventory[1].Itemstack.Item != null && fluiditem == null)
                        {
                            fluiditem = inputbarrel.Inventory[1].Itemstack.Item;
                            if (1 == 1) { }
                        }
                        if (inputbarrel.Inventory[1].Itemstack.Item == fluiditem && !validinputs.Contains(inputnode))
                        {
                            validinputs.Add(inputnode);
                            networkLevel += inputbarrel.Inventory[1].StackSize;
                            continue;
                        }
                    }
                    

                }
            }
            //if no fluid is available we won't bother  checking outputs
            if (fluiditem == null || networkLevel == 0) { return; }
            int totalfluidused = 0;
            foreach (IFlexNetworkMember nm in GetMembers().ToArray())
            {
                if (networkLevel <= 0&&!purge) { break; }
                

                IFluidNetworkMember fnm = nm as IFluidNetworkMember;
                if (fnm == null) { continue; }
                
                //Do an inventory of available fluid
                foreach (BlockEntity outnode in fnm.OutputNodes().ToArray())
                {

                    
                    if (outnode == null) { continue; }
                    if (validinputs.Contains(outnode)) { continue; }
                    
                    //handle fluid network users
                    IFluidNetworkUser fnu = outnode as IFluidNetworkUser;
                    if (fnu != null&&networkLevel>0)
                    {
                        
                        if (validinputs.Contains(outnode)) { continue; }
                        int used = fnu.OfferFluid(fluiditem, Math.Min(networkLevel,maxflow));
                        networkLevel -= used;
                        totalfluidused += used;
                        validinputs.Add(outnode);
                        continue;
                    }
                    else if (networkLevel <= 0) { continue; }
                    //handle tanks
                    IFluidTank outputtank = outnode as IFluidTank;
                    if (outputtank != null)
                    {
                       if (purge)
                        {
                            outputtank.Purge();
                            continue;
                        }
                       if (networkLevel <= 0) { continue; }
                        if (outputtank.IsFull) { continue; }
                        if (outputtank.CurrentLevel > 0 && outputtank.CurrentItem != null && fluiditem == null)
                        {
                            fluiditem = outputtank.CurrentItem;
                        }
                        if (outputtank.CurrentItem != null && outputtank.CurrentItem != fluiditem) { continue; }
                       if (validoutputs.Contains(outnode)) { continue; }
                        //this all lines up so we could now do inventory transfer
                        //** Need to add a check for tankpos==itself to the fluid tank class!!**
                        int used = outputtank.ReceiveFluidOffer(fluiditem, Math.Min(maxflow,networkLevel), outputtank.TankPos);
                        networkLevel -= used;
                        totalfluidused += used;
                        validoutputs.Add(outnode);
                        continue;
                    }
                    BlockEntityBarrel outbarrel = outnode as BlockEntityBarrel;
                    if (outbarrel != null&&networkLevel>=0)
                    {
                        if (outbarrel.Sealed ) { continue; }
                        if (outbarrel.CanSeal && !outbarrel.Inventory[0].Empty) { continue; }
                        bool canreceive = false;
                        if (outbarrel.Inventory == null) { continue; }
                        if (outbarrel.Inventory.Empty || outbarrel.Inventory[1].Itemstack == null||outbarrel.Inventory[1].Itemstack.Item==null) { canreceive = true; }
                        
                        else if (outbarrel.Inventory[1].StackSize == 0) { canreceive = true; }
                        if (outbarrel.Inventory[1].StackSize > 0 && outbarrel.Inventory[1].Itemstack.Item != null && fluiditem == null)
                        {
                            fluiditem = outbarrel.Inventory[1].Itemstack.Item;
                            
                        }
                        else if (outbarrel.Inventory!=null&&outbarrel.Inventory[1]!=null&&outbarrel.Inventory[1].Itemstack!=null&& outbarrel.Inventory[1].Itemstack.Item == fluiditem) { canreceive = true; }
                        if (canreceive && outbarrel.Inventory[1].StackSize >= outbarrel.CapacityLitres*100) { canreceive = false; }
                        if (canreceive&&!validoutputs.Contains(outnode)) {
                            int used = Math.Min(maxflow, networkLevel);
                            used = Math.Min(used, outbarrel.CapacityLitres*100 - outbarrel.Inventory[1].StackSize);
                            if (used > 0)
                            {
                                networkLevel -= used;
                                totalfluidused += used;
                                ItemStack newstack = new ItemStack(fluiditem, used + outbarrel.Inventory[1].StackSize);
                                outbarrel.Inventory[1].Itemstack = newstack;
                                outbarrel.MarkDirty();
                                validoutputs.Add(outnode);
                            }
                        }
                    }
                }


            }
            //Finally we need to go through fluid that was used and take from source containers
            if (purge)
            {
                purge = false;

                return;
            }
            foreach (BlockEntity srcn in validinputs)
            {
                
                if (srcn == null) { continue; }
                
                if (totalfluidused <= 0) { break; }
                IFluidNetworkUser fnu = srcn as IFluidNetworkUser;
                if (fnu != null)
                {
                    int taken = fnu.TakeFluid(fluiditem,Math.Min(totalfluidused,maxflow));
                    totalfluidused -= taken;
                    continue;
                }
                IFluidTank ift = srcn as IFluidTank;
                if (ift != null)
                {
                    
                    if (ift.CurrentLevel <= 0) { continue; }
                    int taken = ift.TryTakeFluid(totalfluidused, ift.TankPos);
                    totalfluidused -= taken;
                    continue;
                }
                BlockEntityBarrel srcbarrel = srcn as BlockEntityBarrel;
                if (srcbarrel != null)
                {
                    int taken = Math.Min(srcbarrel.Inventory[1].StackSize, totalfluidused);
                    if (taken <= 0) { continue; }
                    totalfluidused -= taken;
                    srcbarrel.Inventory[1].Itemstack.StackSize -= taken;
                    if (srcbarrel.Inventory[1].Itemstack.StackSize <= 0)
                    {
                        srcbarrel.Inventory[1].Itemstack = null;
                    }
                    srcbarrel.MarkDirty(true);
                }
            }
            string newfluid = "";
            if (fluiditem != null) { newfluid = fluiditem.Code.ToString(); }
            if (newfluid != fluid)
            {
                fluid = newfluid;
                foreach (IFluidNetworkMember m in members)
                {
                    if (m != null)
                    {
                        if (fluiditem == null) { m.SetFluid(""); }
                        else { m.SetFluid(fluiditem.Code.ToString()); }
                    }
                }
            }
        }

        public void RemoveNetwork()
        {
            foreach (IFlexNetworkMember m in GetMembers())
            {
                if (m != null && m.NetworkID == NetworkID) { m.NetworkRemove(); }

            }
        }

        public void OnPulse(string channel)
        {
            if (channel == "PURGE")
            {
                purge = true;return;
            }
            foreach (IFlexNetworkMember m in GetMembers().ToArray())
            {
                m.OnPulse(channel);
            }
        }
    }
    
    interface IFluidNetworkMember : IFlexNetworkMember
    {
        
        List<BlockEntity> OutputNodes();
        List<BlockEntity> InputNodes();
        void SetFluid(string fluid);
        int FluidRate { get; }

    }
}
