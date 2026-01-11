using System.Text;
using PipelineMod.Common.Mechanics.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipeInlet(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity), IPipelineSource
{
    // todo: see if this can be cached and only updated if the block it resides in changes.
    public bool IsSubmerged => waterLayer()?.IsLiquid() ?? false;
    
    public bool HasDestinations => numDestinationsLocal > 0 || NumDestinations > 0;

    private int numDestinationsLocal;

    public int NumDestinations => Destinations.Count;

    // Inlet cannot push
    public int ActiveOutputDistance => 0;

    // Simple; the inlet provides 1 per tick if submerged.
    public float GetOutput() => IsSubmerged ? 1f : 0f;

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
        return HasDestinations && IsSubmerged;
    }
    
    /**
     * Returns the water layer of this inlet, if any.
     */
    private Block? waterLayer()
    {
        return Api.World.BlockAccessor.GetBlock(Pos, BlockLayersAccess.Fluid);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        dsc.AppendLine($"Number of destinations: {numDestinationsLocal}");
        dsc.AppendLine($"Active: {ShouldInletBeActive()}");
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        numDestinationsLocal = tree.GetInt("NumDestinations");
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetInt("NumDestinations", NumDestinations );
    }
}