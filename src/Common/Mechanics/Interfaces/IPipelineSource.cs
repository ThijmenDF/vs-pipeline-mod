using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineSource : IPipelineNode
{
    /**
     * If this source if slowing anywhere at all.
     */
    bool HasDestinations { get; }
    
    /**
     * The amount of destinations. See IPipelineNode for the dictionary.
     */
    int NumDestinations { get; }
    
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