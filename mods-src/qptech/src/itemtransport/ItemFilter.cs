﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace qptech.src.itemtransport
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ItemFilter
    {
        public int minsize = 1;
        public int maxsize = 10000; //1000 should cover all the max stacks
        static int defaultminsize => 1;
        static int defaultmaxsize => 10000;
        static char[] Splitcode => ",".ToCharArray();
        public string filtercode="";
        public string allowonlymatch="";
        public bool mustmatchall = false;
        public string blockonlymatch="";
        public Dictionary<string, string> rules;
        public bool onlysmeltable = false;
        public bool isoff ;
        public ItemFilter()
        {
            
        }
        public int TestStack(ItemStack itemstack)
        {
            if (isoff) { return 0; }
            int acceptcount = Math.Min(itemstack.StackSize, maxsize);
            
            if (itemstack.StackSize < minsize) { return 0; }
            if (minsize > 1) { acceptcount = minsize; }
            string code = itemstack.Collectible.Code.ToString().ToLower().Trim();
            if (filtercode != "")
            {
                if (filtercode != code) { return 0; }
            }
            if (allowonlymatch != "")
            {
                string[] matchentries = allowonlymatch.Split(Splitcode);
                bool foundmatch = false;
                foreach (string match in matchentries)
                {
                    string cleanmatch=match.ToLower().Trim();
                    if (mustmatchall && !code.Contains(cleanmatch)) { return 0; }
                    else if ((code.Contains(cleanmatch))){ foundmatch=true; }

                }
                if (!foundmatch) { return 0; }
            }
            if (blockonlymatch != "")
            {
                string[] matchentries = blockonlymatch.Split(Splitcode);
                foreach (string match in matchentries)
                {
                    string cleanmatch = match.ToLower().Trim();
                    if ((code.Contains(cleanmatch))) { return 0; }
                }
            }
            return acceptcount;
        }
        public void ClearFilter()
        {
            minsize = defaultminsize;
            maxsize = defaultmaxsize;
            allowonlymatch = "";
            blockonlymatch = "";
            filtercode = "";
            isoff = true;
            onlysmeltable = false;
        }
        public void SetFilterToStack(ItemStack itemstack)
        {
            if (itemstack == null)
            {
                return;
            }
            if (itemstack.Item != null) { filtercode = itemstack.Item.Code.ToString(); }
            else if (itemstack.Block != null) { filtercode = itemstack.Block.Code.ToString(); }
        }
        public string GetFilterDescription()
        {
            string d = "Filter Info: ";
            if (isoff) { d += "OFF! "; }
            if (minsize != defaultminsize || maxsize != defaultmaxsize)
            {
                d += " Qty Limits:";
                if (minsize != defaultminsize) { d += " at least " + minsize + " "; }
                if (maxsize != defaultmaxsize) { d += " maximum " + maxsize + " "; }
                d += " items. ";
            }
            if (filtercode != "") { d += " Only accepts " + filtercode; }
            if (allowonlymatch != "") { d += "Allow items with [" + allowonlymatch + "]"; }
            if (blockonlymatch!="") { d += "Block items with [" + blockonlymatch + "]"; }
            return d;
        }
        public ItemFilter Copy()
        {
            ItemFilter copy = new ItemFilter();
            copy.allowonlymatch = allowonlymatch;
            copy.blockonlymatch = blockonlymatch;
            copy.filtercode = filtercode;
            copy.maxsize = maxsize;
            copy.minsize = minsize;
            copy.onlysmeltable = onlysmeltable;
            copy.mustmatchall = mustmatchall;
            copy.isoff = isoff;
            if (rules != null) { copy.rules = new Dictionary<string, string>(rules); }
            return copy;
        }

    }
}
