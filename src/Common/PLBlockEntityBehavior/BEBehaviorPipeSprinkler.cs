using System.Text;
using Vintagestory.API.Common;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipeSprinkler(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity)
{
    /**
     * Returns whether this inlet is currently used by a pump, and that pump is currently operating.
     */
    public bool ShouldBeActive()
    {
        return DistToNearestSource > 0 && Source?.ActiveTravelDistance >= DistToNearestSource;
    }
    
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        dsc.AppendLine($"Active: {ShouldBeActive()}");
    }
}