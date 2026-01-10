using System.Text;
using PipelineMod.Common.Mechanics.Interfaces;
using PipelineMod.Common.PLBlocks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipeSprinkler(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity), 
    IPipelineDestination, IPipelineTicks
{
    private int lastColumnIndex;
    private int lastRowIndex;

    public bool HasSource => NumSources > 0;
    
    public int NumSources { get; set; }
    
    // Sprinklers cannot pull
    public int ActiveInputDistance => 0;
    
    // Sprinklers can be either direction based on the actual block.
    public BlockFacing GetInputSide() => (Block as BlockPipeSprinkler)!.GetConnectableFacings()[0];

    /**
     * Returns whether this inlet is currently used by a pump, and that pump is currently operating.
     */
    public bool ShouldBeActive() => HasSource;


    public void Tick(float delta)
    {
        // If this is active, water the nearby farm fields underneath it.
        if (!ShouldBeActive()) return;
        
        // Let's just start with a simple 3x3x3 block, scanning each column top to bottom, starting at posY - 1
        if (lastColumnIndex >= 3)
        {
            lastColumnIndex = 0;
            lastRowIndex++;
        }
        if (lastRowIndex >= 3)
        {
            lastRowIndex = 0;
        }

        var startPos = Pos.AddCopy(-1 + lastColumnIndex, -1, -1 + lastRowIndex);
        BlockEntity? block = null;
        
        for (var i = 0; i < 3; i++)
        {
            // Check if there's farmland here
            block = Api.World.BlockAccessor.GetBlockEntity(startPos.AddCopy(0, -i, 0));
            if (block != null) break;
        }
        
        // Now we upp this otherwise it never reaches 0 xD
        ++lastColumnIndex;

        // Better luck next time.
        if (block is not BlockEntityFarmland farmland) return;

        farmland.WaterFarmland(0.1f, false);
        block.MarkDirty();
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        dsc.AppendLine($"Has source: {HasSource}");
        dsc.AppendLine($"Active: {ShouldBeActive()}");
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        NumSources = tree.GetInt("NumSources");
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetInt("NumSources", NumSources);
    }
}