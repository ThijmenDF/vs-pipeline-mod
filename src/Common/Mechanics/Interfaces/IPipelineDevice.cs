using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineDevice : IPipelineNode
{
    IPipelineSource? ClosestInlet { get; set; }
    
    int DistanceToClosestInlet { get; set; }
    
    int MaxTravelDistance { get; set; }
    
    int ActiveTravelDistance { get; set; }

    BlockFacing GetInputSide();
    BlockFacing GetOutputSide();
    
}