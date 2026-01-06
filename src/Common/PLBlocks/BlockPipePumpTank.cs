#nullable disable
using PipelineMod.Common.PLBlockEntity;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlocks;

internal class BlockPipePumpTank : BlockPipelineBase
{
    private BlockFacing orientation;
    public BlockFacing outputSide { get; private set; }
    public BlockFacing inputSide { get; private set; }

    public BlockFacing Orientation => orientation;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        orientation = BlockFacing.FromFirstLetter(Variant["side"][0]);

        switch (orientation.Index)
        {
            case BlockFacing.indexNORTH:
                outputSide = BlockFacing.EAST;
                inputSide = BlockFacing.WEST;
            break;
            case BlockFacing.indexSOUTH:
                outputSide = BlockFacing.WEST;
                inputSide = BlockFacing.EAST;
            break;
            case BlockFacing.indexEAST:
                outputSide = BlockFacing.SOUTH;
                inputSide = BlockFacing.NORTH;
            break;case BlockFacing.indexWEST:
                outputSide = BlockFacing.NORTH;
                inputSide = BlockFacing.SOUTH;
            break;
        }

        if (orientation.IsAxisNS)
            connectableFaces = [BlockFacing.EAST, BlockFacing.WEST];
        else 
            connectableFaces = [BlockFacing.NORTH, BlockFacing.SOUTH];
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is not BlockEntityPipePumpTank blockEntity || blockEntity.Principal == null)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
        else
        {
            var principal = blockEntity.Principal;
            world.BlockAccessor.GetBlock(principal)?.OnBlockBroken(world, principal, byPlayer, dropQuantityMultiplier);
            
            if (api.Side == EnumAppSide.Client) return;

            // There's a very useful method in ServerMain, but idk how to get to it :(
            foreach (var facing in BlockFacing.HORIZONTALS)
            {
                var loc = principal.AddCopy(facing);
                world.BlockAccessor.GetBlock(loc).OnNeighbourBlockChange(world, loc, principal);
            }
        }
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        var accessor = world.BlockAccessor;

        return accessor.GetBlockEntity(pos) is not BlockEntityPipePumpTank tank || tank.Principal == null
            ? new ItemStack(world.GetBlock(new AssetLocation("pipelinemod:pipepump-north")))
            : accessor.GetBlock(tank.Principal).OnPickBlock(world, tank.Principal);
    }
}