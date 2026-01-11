using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PipelineMod.Common.PLBlockEntity;

public class BlockEntityPipeMold : BlockEntityToolMold, ILiquidMetalSink, ITemperatureSensitive
{
    public bool IsSoft => ! IsHardened && !IsLiquid;
    
    public new bool CanReceive(ItemStack metal)
    {
        if (MetalContent != null && (
            !MetalContent.Collectible.Equals(MetalContent, metal, GlobalConstants.IgnoredStackAttributes) 
            || IsFull
        )) 
            return false;
        
        var isLead = metal.Item.Code.EndVariant() == "lead";

        return isLead;
    }
    
    public new void CoolNow(float amountRel)
    {
        
        if (MetalContent == null)
            return;
        
        var temperature = Temperature;
        if (temperature > 120.0)
            Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, randomizePitch: false, range: 16f);
        
        MetalContent.Collectible.SetTemperature(Api.World, MetalContent, GameMath.Max(20f, temperature - amountRel * 20f), false);
       
        MarkDirty(true);
    }

    public new bool OnPlayerInteract(IPlayer byPlayer, BlockFacing onFace, Vec3d hitPosition)
    {
        if (Shattered || MetalContent == null || byPlayer.Entity.Controls.HandUse != EnumHandInteract.None)
            return false;

        var usingShift = byPlayer.Entity.Controls.ShiftKey;
        
        // TryTakeContents won't do anything if the content isn't hardened or not fully filled.
        if (!usingShift && TryTakeContents(byPlayer)) return true;

        // Check if the metal is 'soft' right now.
        if (!usingShift && FillLevel == 0)
        {
            Api.Logger.Notification("FillLevel is 0");
            // Picking up the mold.
            var activeStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (activeStack != null && activeStack.Collectible is not BlockToolMold and not BlockIngotMold) 
                return false;

            var itemStack = new ItemStack(Block);
            if (!byPlayer.InventoryManager.TryGiveItemstack(itemStack))
            {
                Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
            }
            Api.World.Logger.Audit("{0} Took 1x{1} mold at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);

            Api.World.BlockAccessor.SetBlock(0, Pos);

            if (Block.Sounds?.Place != null)
            {
                Api.World.PlaySoundAt(Block.Sounds.Place, Pos, -0.5, byPlayer, false);
            }

            return true;
        }
        
        var heldItem = byPlayer.Entity.ActiveHandItemSlot?.Itemstack;

        // When you're not holding the pipeshaper or the metal is not soft (probably liquid)
        if (! usingShift || !IsSoft || heldItem?.Item.Code.GetName() != "pipeshaper") return false;
        
        // Yes! You did it! Your reward is a bunch of pipelines.
        var resultItem = getMoldResult();

        // Too bad.
        if (resultItem == null) return false;
            
        var stackSize = resultItem.StackSize;
        
        if (!byPlayer.InventoryManager.TryGiveItemstack(resultItem))
            Api.World.SpawnItemEntity(resultItem, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
        
        Api.World.Logger.Audit("{0} Took {1} x {2} from pipe mold at {3}.", byPlayer.PlayerName, stackSize, resultItem.Collectible.Code, Pos);
        
        // Reset the mold
        FillLevel = 0;
        MetalContent = null;
        UpdateRenderer();
        
        return true;
    }

    private ItemStack? getMoldResult()
    {
        if (!Block.Attributes["shaperesult"].Exists)
        {
            Api.Logger.Notification("Block attribute 'shaperesult' not found.");
            return null;
        }
        
        // VS Source seems to use #nullable disable, but I don't. So yeah...
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        var jstack = Block.Attributes["shaperesult"].AsObject((JsonItemStack)null, Block.Code.Domain);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        jstack?.Resolve(Api.World, "result from soft-molding pipes");

        return jstack?.ResolvedItemstack;
    }
}