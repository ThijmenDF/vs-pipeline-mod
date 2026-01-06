#nullable disable
using System.Linq;
using PipelineMod.Common.PLBlockEntity;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace PipelineMod.Common.PLBlocks;

internal class BlockPipePumpEngine : BlockMPBase
{
    private BlockFacing orientation;

    private BlockFacing connectableAt = BlockFacing.WEST;
    
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        orientation = BlockFacing.FromFirstLetter(Variant["side"][0]);
        // Connector is on the 'left' side when looking north.
        connectableAt = orientation.GetCCW();
        
        api.Logger.Notification("Pump orientation: " + orientation);
    }

    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {}

    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
        api.Logger.Notification("Check connection on " + face.Code + ", connectable: " + connectableAt);
        
        return face == connectableAt;
    }
    

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
            return false;

        WasPlaced(world, blockSel.Position, blockSel.Face);
        return true;
    }

    public override void WasPlaced(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
        // Try connecting it to the connectableAt side.
        GetBEBehavior<BEBehaviorMPBase>(pos)?.tryConnect(connectableAt);
        
        PlaceSecondBlock(world, pos);
    }

    private void PlaceSecondBlock(IWorldAccessor world, BlockPos pos)
    {
        var block = world.GetBlock(new AssetLocation("pipelinemod:pipepumptank-" + orientation.Code));
        var position = pos.AddCopy(orientation);
        world.BlockAccessor.SetBlock(block.BlockId, position);
        if (world.BlockAccessor.GetBlockEntity(position) is not BlockEntityPipePumpTank tank)
            return;

        tank.Principal = pos;
    }

    private void RemoveSecondBlock(IWorldAccessor world, BlockPos pos)
    {
        var block = world.BlockAccessor.GetBlock(pos.AddCopy(orientation));
        if (block.Code.Path == "pipepumptank-" + orientation.Code)
            world.BlockAccessor.SetBlock(0, pos.AddCopy(orientation));
        else
            api.Logger.Notification("Cannot remove second block. block code path = " + block.Code.Path + " - looked at " + orientation);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        RemoveSecondBlock(world, pos);
        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
    {
        if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            return false;

        var selection = blockSel.Clone();
        selection.Position.Add(orientation);
        // Only need to check the orientation side, if that's good, we're good.
        return base.CanPlaceBlock(world, byPlayer, selection, ref failureCode);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
    {
        var entryPos = pos.AddCopy(orientation);
        if (world.BlockAccessor.GetBlock(entryPos) is BlockAngledGears gear)
        {
            if (gear.Facings.Length == 1 && gear.Facings.Contains(orientation.Opposite))
            {
                world.BlockAccessor.BreakBlock(entryPos, null);
            }
        }
        
        base.OnNeighbourBlockChange(world, pos, neibpos);
    }
}