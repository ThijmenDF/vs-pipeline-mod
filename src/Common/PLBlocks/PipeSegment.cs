#nullable disable
using PipelineMod.Common.PLBlockEntity;
using PipelineMod.Common.PLBlockEntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PipelineMod.Common.PLBlocks;

internal class PipeSegment : BlockPipelineBase
{
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        // Pipe can connect on all sides.
        connectableFaces = [
            BlockFacing.DOWN,
            BlockFacing.UP,
            BlockFacing.EAST,
            BlockFacing.WEST,
            BlockFacing.NORTH,
            BlockFacing.SOUTH,
        ];
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        var behavior = blockAccessor.GetBlockEntity<BlockEntityPipeSegment>(pos)?.GetBehavior<BEBehaviorPipeSegment>();
        if (behavior == null)
            return base.GetSelectionBoxes(blockAccessor, pos);

        return behavior.GetSelectionBoxes();
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        var behavior = blockAccessor.GetBlockEntity<BlockEntityPipeSegment>(pos)?.GetBehavior<BEBehaviorPipeSegment>();
        if (behavior == null)
            return base.GetCollisionBoxes(blockAccessor, pos);

        return behavior.GetSelectionBoxes();
    }
}