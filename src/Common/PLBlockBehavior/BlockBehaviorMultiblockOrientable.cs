using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PipelineMod.Common.PLBlockBehavior;

public class BlockBehaviorMultiblockOrientable(Block block) : BlockBehavior(block)
{
    private int InitialX;
    private int InitialY;
    private int InitialZ;
    
    private int SizeX;
    private int SizeY;
    private int SizeZ;

    private Vec3i InitialControllerPositionRel = null!;
    private Vec3i ControllerPositionRel = null!;

    private string type = null!;

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        SizeX = InitialX = properties["sizex"].AsInt(3);
        SizeY = InitialY = properties["sizey"].AsInt(3);
        SizeZ = InitialZ = properties["sizez"].AsInt(3);
        type = properties["type"].AsString("monolithic");
        ControllerPositionRel = properties["cposition"].AsObject(new Vec3i(1, 0, 1));
        InitialControllerPositionRel = properties["cposition"].AsObject(new Vec3i(1, 0, 1));
    }

    /**
     * Rotates the multiblock to face a specific direction.
     */
    public void SetRotation(BlockFacing facing)
    {
        if (facing.IsAxisWE)
        {
            // Swap x and y sizes
            SizeZ = InitialX;
            SizeX = InitialZ;
        }
        else
        {
            // Set them again
            SizeX = InitialX;
            SizeZ = InitialZ;
        }
        
        // Adjust the controller position.
        switch (facing.Index)
        {
            case BlockFacing.indexNORTH: // North is the natual position
                ControllerPositionRel.X = InitialControllerPositionRel.X;
                ControllerPositionRel.Z = InitialControllerPositionRel.Z;
            break;
            case BlockFacing.indexSOUTH: // South is completely inverted (opposite corner, basically)
                ControllerPositionRel.X = InitialX - 1 - InitialControllerPositionRel.X;
                ControllerPositionRel.Z = InitialZ - 1 - InitialControllerPositionRel.Z;
            break;
            case BlockFacing.indexEAST:
                ControllerPositionRel.X = InitialZ - 1 - InitialControllerPositionRel.Z;
                ControllerPositionRel.Z = InitialControllerPositionRel.X;
            break;
            case BlockFacing.indexWEST:
                ControllerPositionRel.X = InitialControllerPositionRel.Z;
                ControllerPositionRel.Z = InitialX - 1 - InitialControllerPositionRel.X;
            break;
        }
    }

    public override bool CanPlaceBlock(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref EnumHandling handling,
        ref string failureCode)
    {
        var blocked = false;
        IterateOverEach(blockSel.Position, mpos =>
        {
            if (mpos == blockSel.Position || world.BlockAccessor.GetBlock(mpos).IsReplacableBy(block))
                return true;
            blocked = true;
            return false;
        });
        if (!blocked)
            return true;
        handling = EnumHandling.PreventDefault;
        failureCode = "notenoughspace";
        return false;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        IterateOverEach(pos, mpos =>
        {
            if (mpos == pos)
                return true;
            var num1 = mpos.X - pos.X;
            var num2 = mpos.Y - pos.Y;
            var num3 = mpos.Z - pos.Z;
            var blockCode = new AssetLocation(
                $"multiblock-{type}-{(num1 < 0 ? "n" : (num1 > 0 ? "p" : "")) + Math.Abs(num1).ToString()}-{(num2 < 0 ? "n" : (num2 > 0 ? "p" : "")) + Math.Abs(num2).ToString()}-{(num3 < 0 ? "n" : (num3 > 0 ? "p" : "")) + Math.Abs(num3).ToString()}");
            world.BlockAccessor.SetBlock(
                (world.GetBlock(blockCode) ?? throw new IndexOutOfRangeException(
                    "Multiblocks are currently limited to 5x5x5 with the controller being in the middle of it, yours likely exceeds the limit because I could not find block with code " +
                    blockCode.Path)).Id, mpos);
            return true;
        });
    }

    public void IterateOverEach(BlockPos controllerPos, ActionConsumable<BlockPos> onBlock)
    {
        var num1 = controllerPos.X - ControllerPositionRel.X;
        var num2 = controllerPos.Y - ControllerPositionRel.Y;
        var num3 = controllerPos.Z - ControllerPositionRel.Z;
        var t1 = new BlockPos(controllerPos.dimension);
        for (var index1 = 0; index1 < SizeX; ++index1)
        {
            for (var index2 = 0; index2 < SizeY; ++index2)
            {
                for (var index3 = 0; index3 < SizeZ; ++index3)
                {
                    t1.Set(num1 + index1, num2 + index2, num3 + index3);
                    if (!onBlock(t1))
                        return;
                }
            }
        }
    }

    public override void OnBlockRemoved(
        IWorldAccessor world,
        BlockPos pos,
        ref EnumHandling handling)
    {
        IterateOverEach(pos, mpos =>
        {
            if (mpos == pos || world.BlockAccessor.GetBlock(mpos) is not BlockMultiblock)
                return true;
            world.BlockAccessor.SetBlock(0, mpos);
            return true;
        });
        world.BlockAccessor.MarkBlockModified(pos);
    }
}