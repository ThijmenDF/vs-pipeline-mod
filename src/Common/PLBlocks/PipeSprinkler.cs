using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlocks;

public class PipeSprinkler : BlockPipelineBase
{
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        
        connectableFaces =
        [
            BlockFacing.DOWN
        ];
    }
}