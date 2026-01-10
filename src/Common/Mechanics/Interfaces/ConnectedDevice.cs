using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public class ConnectedDevice(IPipelineDestination destination)
{
    public IPipelineDestination Destination = destination;
    public BlockPos Pos = destination.GetPosition();
}