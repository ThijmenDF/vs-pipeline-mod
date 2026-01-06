#nullable disable
using System.Linq;
using PipelineMod.Common.Mechanics;
using PipelineMod.Common.Mechanics.Interfaces;
using PipelineMod.Common.PLBlockEntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlocks;

public abstract class BlockPipelineBase : Block, IPipelineBlock
{
    protected BlockFacing[] connectableFaces = [];


    public virtual PipeNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
    {
        return world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorPipeBase>()?.Network;
    }

    public bool IsConnectable(BlockFacing side)
    {
        return connectableFaces.Contains(side);
    }

    public BlockFacing[] GetConnectableFacings()
    {
        return connectableFaces;
    }
}