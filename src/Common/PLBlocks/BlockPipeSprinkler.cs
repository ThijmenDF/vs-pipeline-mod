#nullable disable
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlocks;

public class BlockPipeSprinkler : BlockPipelineBase
{
    private Cuboidf collisonBox;
    
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        
        var orientation = BlockFacing.FromFirstLetter(Variant["side"][0]);
        
        api.Logger.Notification("Orientation: " + orientation);
        
        connectableFaces = [orientation.Opposite];

        if (orientation == BlockFacing.UP)
            collisonBox = new Cuboidf(x1: 0.344, y1: 0.3125, z1: 0.344, x2: 0.655, y2: 1, z2: 0.655);
        else
            collisonBox = new Cuboidf(x1: 0.344, y1: 0, z1: 0.344, x2: 0.655, y2: 0.689, z2: 0.655);
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return [collisonBox];
    }
    
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return [collisonBox];
    }
}