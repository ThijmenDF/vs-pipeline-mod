using System;
using System.Text;
using PipelineMod.Common.Mechanics.Interfaces;
using PipelineMod.Common.PLBlockEntity;
using PipelineMod.Common.PLBlocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipePumpTank(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity), IPipelineDevice
{
    
    public IPipelineSource? ClosestInlet { get; set; }
    public int DistanceToClosestInlet { get; set; }

    public int MaxTravelDistance { get; set; } = 12;
    
    public int ActiveTravelDistance { get; set; }

    private BlockPos? inletPos;

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        
        if (api.Side ==  EnumAppSide.Client)
            findInlet(api as ICoreClientAPI);
    }

    public void Tick(float delta)
    {
        // Check if there's a grate within range
        //if (DistanceToClosestInlet <= 0 || ClosestInlet == null) return;

        // Check if the grate is submerged.
        //if (!ClosestInlet.IsSubmerged) return;
        
        // Actually "pump" water out of this source.
        //ClosestInlet.RemoveWaterLayer();

        // Some magic numbers. a speed of 1 means a distance of 10.
        // Speed .5 => 5, .52 => 6 etc.
        var lastTravelDistance = ActiveTravelDistance;
        var speed = (Blockentity as BlockEntityPipePumpTank)
            ?.Engine
            ?.GetBehavior<BEBehaviorPipePumpEngine>()
            ?.CurrentSpeed() ?? 0f;
        ActiveTravelDistance = (int)Math.Ceiling(speed* 10f);

        if (lastTravelDistance != ActiveTravelDistance)
        {
            Blockentity.MarkDirty(true);
        }

    }

    public BlockFacing GetInputSide()
    {
        return (Block as BlockPipePumpTank)!.inputSide;
    }
    
    public BlockFacing GetOutputSide()
    {
        return (Block as BlockPipePumpTank)!.outputSide;
    }


    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        dsc.AppendLine($"Distance to ClosestInlet: {DistanceToClosestInlet}");
        dsc.AppendLine($"Active Distance: {ActiveTravelDistance}");
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        
        DistanceToClosestInlet = tree.GetInt("distanceToClosestGrate");
        ActiveTravelDistance = tree.GetInt("activeTravelDistance");
        inletPos = tree.GetBlockPos("sourcePos");
        
        
        if (worldAccessForResolve.Side == EnumAppSide.Client) 
            findInlet(Api as ICoreClientAPI);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        
        tree.SetInt("distanceToClosestGrate", DistanceToClosestInlet);
        tree.SetInt("activeTravelDistance", ActiveTravelDistance);
        
        if (ClosestInlet != null)
            tree.SetBlockPos("sourcePos", ClosestInlet.GetPosition());
    }

    private void findInlet(ICoreClientAPI? api)
    {
        if (api == null || inletPos == null)
        {
            ClosestInlet = null;
            return;
        }
        
        ClosestInlet = api.World.BlockAccessor.GetBlockEntity(inletPos)?.GetBehavior<IPipelineSource>();
    }
}
