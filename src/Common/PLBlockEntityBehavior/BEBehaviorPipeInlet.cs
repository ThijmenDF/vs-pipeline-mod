using System.Text;
using PipelineMod.Common.Mechanics.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipeInlet(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity), IPipelineSource
{
    public bool IsSubmerged => waterLayer().IsLiquid();
    
    public bool HasDestination => NumDestinations > 0;

    public int NumDestinations { get; set; }

    // Inlets are only active if they are submerged in a liquid.
    public bool CanBeActive => IsSubmerged;

    // Inlet cannot push
    public int ActiveOutputDistance => 0;
    
    // Output side is always up.
    public BlockFacing GetOutputSide()
    {
        return BlockFacing.UP;
    }

    /**
     * Returns whether this inlet is currently used by a pump, and that pump is currently operating.
     */
    public bool ShouldInletBeActive()
    {
        return HasDestination && IsSubmerged;
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
        dsc.AppendLine($"Number of destinations: {NumDestinations}");
        dsc.AppendLine($"CanBeActive: {CanBeActive}");
        dsc.AppendLine($"Active: {ShouldInletBeActive()}");
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        NumDestinations = tree.GetInt("NumDestinations");
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetInt("NumDestinations", NumDestinations );
    }
}