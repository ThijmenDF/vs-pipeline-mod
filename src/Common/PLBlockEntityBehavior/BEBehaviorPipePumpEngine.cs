using System.Text;
using PipelineMod.Common.PLBlockEntityRenderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipePumpEngine(BlockEntity blockentity) : BEBehaviorMPBase(blockentity), IPipelineMPPumpEngineRenderable
{
    public required int[] MainGearAxis { get; set; }
    public required int[] SecondaryGearAxis { get; set; }

    public float AngleSecondary { get; set; }
    private float lastNetAngle;
    private bool hasLastNetAngle;

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        MainGearAxis = SecondaryGearAxis = [0, 0, 0];
        

        AxisSign = Block.LastCodePart() switch
        {
            "north" or "south" => [1, 0, 0],
            "east" or "west" => [0, 0, -1],
            _ => AxisSign
        };
        
        
    }

    public void ClientTick(float deltaTime)
    {
        var currentNetAngle = AngleRad;

        if (!hasLastNetAngle)
        {
            lastNetAngle = currentNetAngle;
            hasLastNetAngle = true;
            return;
        }

        var delta = UnwrappedDelta(currentNetAngle, lastNetAngle);
        lastNetAngle = currentNetAngle;
        
        AngleSecondary += delta / 3f;
        
        if (AngleSecondary is > GameMath.TWOPI or < -GameMath.TWOPI)
            AngleSecondary = GameMath.Mod(AngleSecondary, GameMath.TWOPI);
        
    }

    private float UnwrappedDelta(float current, float last)
    {
        var delta = current - last;
        
        if (delta > GameMath.PI) delta -= GameMath.TWOPI;
        else if (delta < -GameMath.PI) delta += GameMath.TWOPI;

        return delta;
    }

    public override float GetResistance()
    {
        return 0.025f;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
        base.GetBlockInfo(forPlayer, sb);
        
        sb.AppendLine($"Resistance: {GetResistance()}");
        sb.AppendLine("MPNetworkID: " + NetworkId);
        var speed = network?.Speed ?? 0f;
        speed *= GearedRatio;
        sb.AppendLine("Speed: " + speed);
    }

    /**
     * Returns the speed at the current time.
     */
    public float CurrentSpeed()
    {
        return (network?.Speed ?? 0f) * GearedRatio;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        base.OnTesselation(mesher, tesselator);
        
        var api = (Api as ICoreClientAPI)!;

        var shape = Vintagestory.API.Common.Shape.TryGet(api, "pipelinemod:shapes/pump/pump2-engine-frame.json");
        var y = 0.0f;
        switch (BlockFacing.FromCode(Block.Variant["side"]).Code)
        {
            case "north":
                MainGearAxis = SecondaryGearAxis = [1, 0, 0];
                break;
            case "south":
                MainGearAxis = SecondaryGearAxis = [-1, 0, 0];
                y = 180f;
            break;
            case "east":
                MainGearAxis = SecondaryGearAxis = [0, 0, -1];
                y = 270f;
            break;
            case "west":
                MainGearAxis = SecondaryGearAxis = [0, 0, 1];
                y = 90f;
            break;
        }

        api.Tesselator.TesselateShape(Block, shape, out var modeldata, new Vec3f(0, y, 0));
        mesher.AddMeshData(modeldata);
        return true;
    }

}