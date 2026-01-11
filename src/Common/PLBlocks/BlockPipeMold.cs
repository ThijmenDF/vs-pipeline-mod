using PipelineMod.Common.PLBlockEntity;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace PipelineMod.Common.PLBlocks;

public class BlockPipeMold : BlockToolMold
{
    protected WorldInteraction[] interactions = null!;
    
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        if (api.Side != EnumAppSide.Client) return;
        
        // Register our interaction override
        interactions = ObjectCacheUtil.GetOrCreate(api, "pipemoldBlockInteractions", (CreateCachableObjectDelegate<WorldInteraction[]>) (() =>
        {
            var tool = new ItemStack[1];

            foreach (var obj in api.World.Collectibles)
            {
                if (obj.Code.GetName() == "pipeshaper")
                {
                    tool[1] = new ItemStack(obj);
                    break;
                }
            }
            
            return new WorldInteraction[]
            {
                new()
                {
                    ActionLangCode = "pipelinemod:blockhelp-pipemold-fold",
                    HotKeyCode = "shift",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = tool,
                    GetMatchingStacks = (_, block, _) => api.World.BlockAccessor.GetBlockEntity(block.Position) is BlockEntityPipeMold
                        {
                            IsSoft: true, IsFull: true
                        }
                            ? tool : null
                }
            };
        }));
        
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }

    public override void OnHeldInteractStart(
        ItemSlot itemslot,
        EntityAgent byEntity,
        BlockSelection? blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handHandling)
    {
        if (blockSel == null)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }
        else
        {
            if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite)) is not BlockEntityPipeMold blockEntity)
                return;
            if (byEntity is not EntityPlayer player) return;
            
            var byPlayer = player.World.PlayerByUid(player.PlayerUID);
            
            if (byPlayer == null || !blockEntity.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition))
                return;
            
            handHandling = EnumHandHandling.PreventDefault;
        }
    }

    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection? blockSel)
    {
        return blockSel != null && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityPipeMold blockEntity 
            ? blockEntity.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition) 
            : base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}