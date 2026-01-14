using System.Collections.Generic;
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

    private int numSourcesLocal;

    public bool HasSources => numSourcesLocal > 0 || NumSources > 0;
    
    public int NumSources => Sources.Count;

    public List<IPipelineSource> Sources { get; } = [];
    
    // Sprinklers cannot pull
    public int ActiveInputDistance => 0;

    // Local buffer to track total supply.
    private float currentBuffer;
    
    public void ProvideInput(float volume) => currentBuffer += volume;

    public void MarkTickComplete()
    {
        var wasProvided = isProvided;
        isProvided = currentBuffer >= minFluidPerSecond;
        if (wasProvided != isProvided)
            MarkDirty();

        currentBuffer = 0f;
    }

    // Sprinklers can be either direction based on the actual block.
    public BlockFacing GetInputSide() => inputFace;

    private bool isProvided;
    
    /**
     * Returns if there are sources and those sources are providing fluids.
     */
    public bool ShouldBeActive() => isProvided;

    private float minFluidPerSecond = 1f;
    private BlockFacing inputFace = BlockFacing.UP;

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        minFluidPerSecond = (Block as BlockPipeSprinkler)!.minFluidPerSecond;
        inputFace = (Block as BlockPipeSprinkler)!.GetConnectableFacings()[0];
    }

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

        farmland.WaterFarmland(0.1f);
        block.MarkDirty();
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        dsc.AppendLine($"Has source: {HasSources}");
        dsc.AppendLine($"Active: {ShouldBeActive()}");
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        numSourcesLocal = tree.GetInt("NumSources");
        isProvided = tree.GetBool("IsProvided");
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetInt("NumSources", NumSources);
        tree.SetBool("IsProvided", isProvided);
    }
}