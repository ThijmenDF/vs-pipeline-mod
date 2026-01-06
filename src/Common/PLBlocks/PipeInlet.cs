#nullable disable
using PipelineMod.Common.PLBlockEntity;
using PipelineMod.Common.PLBlockEntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PipelineMod.Common.PLBlocks;

internal class PipeInlet : BlockPipelineBase
{
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        
        connectableFaces = [
            BlockFacing.UP,
        ];
    }
}