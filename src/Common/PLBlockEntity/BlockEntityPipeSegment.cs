using PipelineMod.Common.PLBlockEntityBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PipelineMod.Common.PLBlockEntity;

public class BlockEntityPipeSegment : BlockEntity
{
    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        var behavior = GetBehavior<BEBehaviorPipeSegment>();
        if (behavior?.CurrentMesh == null)
            return base.OnTesselation(mesher, tessThreadTesselator);
        
        mesher.AddMeshData(behavior.CurrentMesh);
        return true;
    }
}