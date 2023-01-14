using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RustAndRails.src
{
    class BlockDetectorRail:BlockRail,IRailwaySignalReceiver
    {
        protected void CartDetected(IWorldAccessor world, BlockPos blockpos,int strength,string signal)
        {
            
            if (world==null||Attributes == null) { return; }
            if (!signal.Contains("cart")) { return; }
            string replace = Attributes["replace"].AsString(null);
            if (replace == null) { return; }
            Block replaceblock = world.GetBlock(new AssetLocation(replace));
            if (replaceblock == null) { return; }
            
            //search for and activate switches - this will eventually be a generic switching behaviour
            foreach(BlockFacing bf in BlockFacing.ALLFACES)
            {
                BlockPos checkpos = blockpos.Copy().Offset(bf);
                IRailwaySignalReceiver signalswitch = world.BlockAccessor.GetBlock(checkpos) as IRailwaySignalReceiver;
                if (signalswitch != null)
                {
                    signalswitch.ReceiveRailwaySignal(world, checkpos,strength,signal);
                }
                else
                {
                    Block checkblock = world.BlockAccessor.GetBlock(checkpos);
                    if (checkblock.Attributes != null)
                    {
                        string switchblock = checkblock.Attributes["railswitch"].AsString("");
                        if (switchblock != "")
                        {
                            Block newrail = world.GetBlock(new AssetLocation(switchblock));
                            if (newrail != null)
                            {
                                world.BlockAccessor.SetBlock(newrail.BlockId, checkpos);

                            }
                        }
                    }
                }
            }
            world.BlockAccessor.SetBlock(replaceblock.BlockId, blockpos);
        }

        public virtual void ReceiveRailwaySignal(IWorldAccessor world, BlockPos pos,int strength, string signal)
        {
            strength--;
            if (strength <= 0) { return; }
            CartDetected(world,pos,strength,signal);
        }
    }
}
