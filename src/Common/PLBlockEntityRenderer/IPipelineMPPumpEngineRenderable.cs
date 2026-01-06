using Vintagestory.GameContent.Mechanics;

namespace PipelineMod.Common.PLBlockEntityRenderer;

public interface IPipelineMPPumpEngineRenderable : IMechanicalPowerRenderable
{
    int[] MainGearAxis { get; set; }
    int[] SecondaryGearAxis { get; set; }

    float AngleSecondary { get; protected set; }
    
    void ClientTick(float deltaTime);
}