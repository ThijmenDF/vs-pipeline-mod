using System;
using System.Collections.Generic;
using System.Text;
using PipelineMod.Common.Mechanics.Interfaces;
using PipelineMod.Common.PLBlockEntity;
using PipelineMod.Common.PLBlocks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipePumpTank(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity), 
    IPipelineDestination, IPipelineSource, IPipelineTicks
{
    public bool HasSources => numSourcesLocal > 0 || NumSources > 0;
    
    private int numSourcesLocal;

    public int NumSources => Sources.Count;
    
    public bool HasDestinations => numDestinationsLocal > 0 || NumDestinations > 0;
    
    private int numDestinationsLocal;

    public int NumDestinations => Destinations.Count;

    public List<IPipelineSource> Sources { get; } = [];

    public int ActiveInputDistance { get; private set; }

    public int ActiveOutputDistance => ActiveInputDistance;

    // How much volume is currently in the destination buffer.
    private float currentVolume;
    // The max volume of the destination buffer.
    private const float maxVolume = 2f;

    public void ProvideInput(float volume)
    {
        currentVolume = GameMath.Min(volume + currentVolume, maxVolume);
    }

    // Retrieves the destination buffer's content.
    public float GetOutput()
    {
        var volume = currentVolume;
        currentVolume = 0f;
        return volume;
    }

    public BlockFacing GetInputSide()
    {
        return (Block as BlockPipePumpTank)!.inputSide;
    }
    
    public BlockFacing GetOutputSide()
    {
        return (Block as BlockPipePumpTank)!.outputSide;
    }

    public void Tick(float delta)
    {
        // Some magic numbers. a speed of 1 means a distance of 10.
        // Speed .5 => 5, .52 => 6 etc.
        var lastTravelDistance = ActiveInputDistance;
        var speed = (Blockentity as BlockEntityPipePumpTank)
            ?.Engine
            ?.GetBehavior<BEBehaviorPipePumpEngine>()
            ?.CurrentSpeed() ?? 0f;
        
        // If it's really, really slow, might as well be stopped.
        if (speed < 0.0001f) ActiveInputDistance = 0;
        else ActiveInputDistance = (int)Math.Ceiling(speed * 10f);

        if (lastTravelDistance == ActiveInputDistance) return;
        
        // Let the client know the new values.
        Blockentity.MarkDirty();
    }

    public void MarkTickComplete() {}

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        dsc.AppendLine($"Number of sources: {numSourcesLocal}");
        dsc.AppendLine($"Number of destinations: {numDestinationsLocal}");
        dsc.AppendLine($"Active Input Distance: {ActiveInputDistance}");
        dsc.AppendLine($"Active Output Distance: {ActiveOutputDistance}");
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        
        ActiveInputDistance = tree.GetInt("ActiveInputDistance");
        numSourcesLocal = tree.GetInt("NumSources");
        numDestinationsLocal = tree.GetInt("NumDestinations");
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        
        tree.SetInt("ActiveInputDistance", ActiveInputDistance);
        tree.SetInt("NumSources", NumSources);
        tree.SetInt("NumDestinations", NumDestinations );
    }
}
