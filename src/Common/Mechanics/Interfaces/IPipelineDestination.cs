using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineDestination : IPipelineNode
{
    /**
     * Which sources are able to provide to this destination.
     */
    List<IPipelineSource> Sources { get; }

    /**
     * How far away the fluid can be pulled from, if any.
     */
    int ActiveInputDistance { get; }

    /**
     * Provide a certain amount of volume to this destination.
     */
    void ProvideInput(float volume);

    /**
     * Called after the serverTick was completed.
     */
    void MarkTickComplete();

    /**
     * Which side the fluid can flow into.
     */
    BlockFacing GetInputSide();
    
}