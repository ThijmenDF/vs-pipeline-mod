#nullable disable
using Vintagestory.API.Common;

namespace PipelineMod.Common.PLBlockEntity;

public class BlockEntityPipePumpEngine : BlockEntity
{
    // private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;
    //
    // private bool isRunning = false;

    // public void OnInteract(ILogger logger)
    // {
    //     logger.Notification("Pipe Pump Interacted");
    //     if (isRunning) return;
    //
    //     if (Api.Side != EnumAppSide.Client)
    //         return; // Only client side
    //     
    //     isRunning = true;
    //     animUtil.StartAnimation(new AnimationMetaData
    //     {
    //         Animation = "Rotate",
    //         Code = "rotate",
    //         AnimationSpeed = 1f,
    //     });
    //     
    //     MarkDirty(true);
    // }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        api.Logger.Notification("Pump initialized, rotation: " + GetRotation());
    }

    public int GetRotation()
    {
        return Block.LastCodePart() switch
        {
            "east" => 270,
            "south" => 180,
            "west" => 90,
            _ => 0
        };
    }

    // public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    // {
    //     if (animUtil?.animator == null)
    //         animUtil?.InitializeAnimator("pipepump", null, null, new Vec3f(0, GetRotation(), 0));
    //     
    //     return base.OnTesselation(mesher, tessThreadTesselator);
    // }
}