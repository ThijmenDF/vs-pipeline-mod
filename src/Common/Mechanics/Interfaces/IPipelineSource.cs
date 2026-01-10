using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineSource : IPipelineNode
{
    /**
     * How far the fluid can be pushed to, if any.
     */
    int ActiveOutputDistance { get; }

    /**
     * Takes the current output from the source, draining any potential buffer.
     */
    float GetOutput();
    
    /**
     * The side the fluid exits the source.
     */
    BlockFacing GetOutputSide();
}