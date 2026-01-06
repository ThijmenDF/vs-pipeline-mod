#nullable disable
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlocks;

internal class BlockPipeInlet : BlockPipelineBase
{
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        
        connectableFaces = [
            BlockFacing.UP,
        ];
    }
}