using System.Text;
using PipelineMod.Common.Mechanics.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipeInlet(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity), IPipelineSource
{
    public bool IsSubmerged => waterLayer().IsLiquid();

    /**
     * Returns whether this inlet is currently used by a pump, and that pump is currently operating.
     */
    public bool ShouldInletBeActive()
    {
        // Api?.Logger.Notification($"IsSubmerged: {IsSubmerged}, DistToNearestSource: {DistToNearestSource}, Source finds this to be the closest: {Source?.ClosestInlet == this}, ActiveTravelDistance: {Source?.ActiveTravelDistance}");
        return IsSubmerged
               && DistToNearestSource > 0 
               && Source?.ClosestInlet == this 
               && Source.ActiveTravelDistance >= DistToNearestSource;
    }
    
    /**
     * Returns the water layer of this inlet, if any.
     */
    private Block waterLayer()
    {
        return Api.World.BlockAccessor.GetBlock(Pos, BlockLayersAccess.Fluid);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        dsc.AppendLine($"Active: {ShouldInletBeActive()}");
    }
}