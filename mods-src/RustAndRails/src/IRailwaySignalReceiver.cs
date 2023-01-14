using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RustAndRails.src
{
    interface IRailwaySignalReceiver
    {
        /// <summary>
        /// Railway signal system - blocks and send signals to each toher
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos">Postion of this block</param>
        /// <param name="strength">Strength of the signal - most blocks would reduce by 1</param>
        /// <param name="signal">Consider a channel, or can be used for whatever info you want to check</param>
        void ReceiveRailwaySignal(IWorldAccessor world, BlockPos pos,int strength,string signal);
    }
}
