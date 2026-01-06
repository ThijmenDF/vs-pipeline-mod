#nullable disable
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineBlock
{
    PipeNetwork GetNetwork(IWorldAccessor world, BlockPos pos);

    bool IsConnectable(BlockFacing side);
    
    BlockFacing[] GetConnectableFacings();
}