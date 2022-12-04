﻿using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace qptech.src.itemtransport
{
    class GUIConveyor:GuiDialogBlockEntity
    {
        ICoreClientAPI api;
        ItemPipe conveyor;
        ItemFilter itemfilter;
        DummyInventory di;
        public GUIConveyor(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle,blockEntityPos,capi)
        {
            api = capi;
        }
        public void SetupDialog(ItemPipe conveyor)
        {
            this.conveyor = conveyor;
            di = new DummyInventory(api,1);
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds allowTextBounds = ElementBounds.Fixed(21, 133, 325, 30);
            ElementBounds allowTextInputBounds = ElementBounds.Fixed(21, 171, 551, 37);            
            ElementBounds mustMatchAllBoxBounds = ElementBounds.Fixed(408,133,164,25);
            ElementBounds blockTextBounds = ElementBounds.Fixed(21, 223, 254, 30);
            ElementBounds blockTextInputBounds = ElementBounds.Fixed(21, 262, 551, 37);
            ElementBounds applyButtonBounds = ElementBounds.Fixed(21, 531, 152, 39);
            ElementBounds cancelButtonBounds = ElementBounds.Fixed(224, 531, 152, 39);
            ElementBounds clearButtonBounds = ElementBounds.Fixed(424, 68, 152, 39);
            ElementBounds toggleOnOffButtonBounds = ElementBounds.Fixed(21,68,152,39);
            ElementBounds transferSliderTextBounds = ElementBounds.Fixed(19, 314, 388, 30);
            ElementBounds transferSliderBounds = ElementBounds.Fixed(21, 353, 552, 34);
            ElementBounds drandanddropBounds = ElementBounds.Fixed(21, 353 + 70);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(
                allowTextBounds,allowTextInputBounds,mustMatchAllBoxBounds,
                applyButtonBounds,cancelButtonBounds,clearButtonBounds,toggleOnOffButtonBounds,
                blockTextBounds,blockTextInputBounds,transferSliderTextBounds,transferSliderBounds);
            string guicomponame = conveyor.Pos.ToString()+" Conveyor";
            if (conveyor.itemfilter == null)
            {
                itemfilter = new ItemFilter();
            }
            else
            {
                itemfilter = conveyor.itemfilter;
                
            }

            string onofftext = "DISABLE";

            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Item Filter Setup", OnTitleBarCloseClicked)
                .AddRichtext("Only allow objects with", CairoFont.WhiteDetailText(), allowTextBounds)
                .AddTextInput(allowTextInputBounds, OnChangeItemFilterInput, CairoFont.WhiteDetailText(), "allow")
                .AddToggleButton("Match All", CairoFont.WhiteDetailText(), OnMatchAllToggle, mustMatchAllBoxBounds, "mustmatchall")
                .AddRichtext("Block objects with", CairoFont.WhiteDetailText(), blockTextBounds)
                .AddTextInput(blockTextInputBounds, OnChangeBlockFilterInput, CairoFont.WhiteDetailText(), "block")
                .AddButton("Apply", OnApplyButton, applyButtonBounds)
                .AddButton("Cancel", TryClose, cancelButtonBounds)
                .AddButton("Clear", OnClearButton, clearButtonBounds)
                .AddToggleButton(onofftext, CairoFont.WhiteDetailText(), OnToggleOnOffButton, toggleOnOffButtonBounds, "onoff")
                .AddItemSlotGrid(di,DoSendPacket,1,drandanddropBounds)
                
            ;
            SingleComposer.GetToggleButton("onoff").SetValue(itemfilter.isoff);
            SingleComposer.GetToggleButton("mustmatchall").SetValue(itemfilter.mustmatchall);
            SingleComposer.GetTextInput("allow").SetValue(itemfilter.allowonlymatch);
            SingleComposer.GetTextInput("block").SetValue(itemfilter.blockonlymatch);
            if (conveyor.StackSize != 1)
            {
                SingleComposer.AddRichtext("Minimum Transfer Quantity", CairoFont.WhiteDetailText(), transferSliderTextBounds);
                SingleComposer.AddSlider(OnChangeTransferSlider, transferSliderBounds, "transferslider");
                SingleComposer.GetSlider("transferslider").SetValues(itemfilter.minsize, 0, conveyor.StackSize, 1);
            }
            SingleComposer.Compose();

        }

        public void DoSendPacket(object p)
        {
            return;
        }
        
        public bool OnChangeTransferSlider(int slider)
        {
            itemfilter.minsize = slider;
            itemfilter.maxsize = slider;
            if (slider == 0) { itemfilter.maxsize = conveyor.StackSize; itemfilter.minsize = 1; }
            return true;
        }
        public void OnChangeItemFilterInput(string newinput)
        {
            itemfilter.allowonlymatch = newinput;
        }

        public void OnChangeBlockFilterInput(string newinput)
        {
            itemfilter.blockonlymatch = newinput;
        }

        public void OnMatchAllToggle(bool newvalue)
        {
            itemfilter.mustmatchall = newvalue;
        }
        public const string killtext = "noitems";
        public void OnToggleOnOffButton(bool onoff)
        {
            itemfilter.isoff = onoff;
            
            
        }

        public bool OnGoButton()
        {
            itemfilter.allowonlymatch=itemfilter.allowonlymatch.Replace(killtext, "");
            
            OnApplyButton();
            return true;
        }

        public override bool TryOpen()
        {
            
            return base.TryOpen();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        bool OnApplyButton()
        {
            conveyor.OnNewFilter(itemfilter);
            TryClose();
            return true;
        }
        bool OnClearButton()
        {
            itemfilter = new ItemFilter();
            OnApplyButton();
            return true;
        }
        
    }
}
