#nullable disable
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineSource : IPipelineNode
{
    /**
     * If this source if slowing anywhere at all.
     */
    bool HasDestination { get; }
    
    int NumDestinations { get; set; }
    
    bool CanBeActive { get; }
    
    /**
     * How far the fluid can be pushed to, if any.
     */
    int ActiveOutputDistance { get; }
    
    /**
     * The side the fluid exits the source.
     */
    BlockFacing GetOutputSide();
}