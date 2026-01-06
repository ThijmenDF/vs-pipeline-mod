using PipelineMod.Common.PLBlockBehavior;
using PipelineMod.Common.PLBlockEntity;
using PipelineMod.Common.PLBlockEntityBehavior;
using PipelineMod.Common.PLBlockEntityRenderer;
using PipelineMod.Common.PLBlocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent.Mechanics;

namespace PipelineMod;

public class PipelineModModSystem : ModSystem
{
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {
        var id = Mod.Info.ModID ?? "pipelinemod";

        api.RegisterBlockClass(id + ".Blocks.PipeSegment", typeof(BlockPipeSegment));
        api.RegisterBlockClass(id + ".Blocks.PipePumpEngine", typeof(BlockPipePumpEngine));
        api.RegisterBlockClass(id + ".Blocks.PipePumpTank", typeof(BlockPipePumpTank));
        api.RegisterBlockClass(id + ".Blocks.PipeInlet", typeof(BlockPipeInlet));
        api.RegisterBlockClass(id + ".Blocks.PipeSprinkler", typeof(BlockPipeSprinkler));
        
        api.RegisterBlockEntityClass(id + ".BE.PipePumpEngine", typeof (BlockEntityPipePumpEngine));
        api.RegisterBlockEntityClass(id + ".BE.PipePumpTank", typeof (BlockEntityPipePumpTank));
        api.RegisterBlockEntityClass(id + ".BE.PipeSegment", typeof (BlockEntityPipeSegment));
        
        api.RegisterBlockEntityBehaviorClass(id + ".BEB.PipeSegment", typeof (BEBehaviorPipeSegment));
        api.RegisterBlockEntityBehaviorClass(id + ".BEB.PipePumpEngine", typeof (BEBehaviorPipePumpEngine));
        api.RegisterBlockEntityBehaviorClass(id + ".BEB.PipePumpTank", typeof (BEBehaviorPipePumpTank));
        api.RegisterBlockEntityBehaviorClass(id + ".BEB.PipeInlet", typeof (BEBehaviorPipeInlet));
        api.RegisterBlockEntityBehaviorClass(id + ".BEB.PipeSprinkler", typeof (BEBehaviorPipeSprinkler));
        
        // No longer used :(
        api.RegisterBlockBehaviorClass("MultiblockOrientable", typeof(BlockBehaviorMultiblockOrientable));
        api.RegisterBlockBehaviorClass(id + ".BB.PipeInlet", typeof(BlockBehaviorPipeInlet));
        api.RegisterBlockBehaviorClass(id + ".BB.PipeSprinkler", typeof(BlockBehaviorPipeSprinkler));

        MechNetworkRenderer.RendererByCode["pipelineengine"] = typeof(PipelineMPRenderer);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        // Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("pipelinemod:hello"));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        // Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("pipelinemod:hello"));
    }
}