#nullable disable
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockBehavior;

public class VerticalOrientable : BlockBehavior
{
    
    string dropBlockFace = "up";
    
    string variantCode = "verticalorientation";

    JsonItemStack drop = null;

    public VerticalOrientable(Block block) : base(block)
    {
        if (!block.Variant.ContainsKey("verticalorientation"))
        {
            variantCode = "side";
        }
    }
    
    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        if (properties["dropBlockFace"].Exists)
        {
            dropBlockFace = properties["dropBlockFace"].AsString();
        }
        if (properties["drop"].Exists)
        {
            drop = properties["drop"].AsObject<JsonItemStack>(null, block.Code.Domain);
        }
    }
    
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        drop?.Resolve(api.World, "VerticalOrientable drop for " + block.Code);
    }
    
    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            handling = EnumHandling.PreventDefault;
            var blockCode = block.CodeWithVariant(variantCode, blockSel.Face.Code);
            var orientedBlock = world.BlockAccessor.GetBlock(blockCode);

            if (orientedBlock == null)
            {
                throw new NullReferenceException("Unable to to find a rotated block with code " + blockCode + ", you're maybe missing the side variant group of have a dash in your block code");
            }

            if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
            return false;
        }


        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            if (drop?.ResolvedItemstack != null)
            {
                return [drop?.ResolvedItemstack.Clone()];
            }
            return [new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, dropBlockFace)))];
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            if (drop != null)
            {
                return drop?.ResolvedItemstack.Clone();
            }

            return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, dropBlockFace)));
        }

        
        public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
        {
            // A vertically orientated block cannot be rotated horizontally.
            handled = EnumHandling.PreventDefault;

            return block.Variant[variantCode];
        }

        public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;

            var facing = BlockFacing.FromCode(block.Variant[variantCode]);
            if (facing.Axis == axis)
            {
                return block.CodeWithVariant(variantCode, facing.Opposite.Code);
            }
            return block.Code;
        }
}