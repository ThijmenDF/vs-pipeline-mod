using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineDestination : IPipelineNode
{
    bool HasSource { get; }
    
    int NumSources { get; set; }

    /**
     * How far away the fluid can be pulled from, if any.
     */
    int ActiveInputDistance { get; }

    /**
     * Which side the fluid can flow into.
     */
    BlockFacing GetInputSide();
    
}