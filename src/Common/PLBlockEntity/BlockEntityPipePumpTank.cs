using PipelineMod.Common.PLBlocks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntity;

public class BlockEntityPipePumpTank : BlockEntity
{
    public BlockPos? Principal { get; set; }

    private BlockEntityPipePumpEngine? engine;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        
        // The principal is opposite of our orientation.
        if (Block is BlockPipePumpTank tank)
            Principal = Pos.AddCopy(tank.Orientation.Opposite);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        engine = null;
    }

    public BlockEntityPipePumpEngine? Engine
    {
        get
        {
            if (engine != null)
                return engine;

            return engine = Api.World.BlockAccessor.GetBlockEntity<BlockEntityPipePumpEngine>(Principal);
        }
    }
    
}