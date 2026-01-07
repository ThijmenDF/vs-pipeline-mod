using PipelineMod.Common.PLBlockEntityBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockBehavior;

public class BlockBehaviorPipeSprinkler(Block block) : BlockBehavior(block)
{
    private static readonly SimpleParticleProperties particleProps = new(3, 6, ColorUtil.ColorFromRgba(255, 255, 255, 127), new Vec3d(), new Vec3d(), new Vec3f(-3, 4, -3), new Vec3f(6, 0, 6));

    private static readonly Vec3d sourcePos = new(0.5d, 0.5d, 0.5d);

    static BlockBehaviorPipeSprinkler()
    {
        particleProps.MinSize = 0.2f;
        particleProps.MaxSize = 0.25f;
        particleProps.ParticleModel = EnumParticleModel.Quad;
        particleProps.ClimateColorMap = "climateWaterTint";
        particleProps.LifeLength = 0.4f;
        particleProps.addLifeLength = 0f;
        particleProps.ShouldDieInLiquid = true;
        particleProps.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.CLAMPEDPOSITIVESINUS, 1.5f);
    }

    public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
    {
        var sprinkler = world.BlockAccessor.GetBlockEntity(pos).GetBehavior<BEBehaviorPipeSprinkler>();
        if (sprinkler == null || ! sprinkler.ShouldBeActive())
        {
            return base.ShouldReceiveClientParticleTicks(world, byPlayer, pos, ref handling);
        }
     
        handling = EnumHandling.Handled;
        return true;
    }

    public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
    {
        particleProps.MinPos = pos.ToVec3d().Add(sourcePos);
        var entity = manager.BlockAccess.GetBlockEntity(pos);
        var sprinkler = entity.GetBehavior<BEBehaviorPipeSprinkler>();
        if (sprinkler != null && sprinkler.Source != null)
        {
            var effectiveDistance = sprinkler.Source.ActiveTravelDistance - sprinkler.DistToNearestSource;
            
            // Measure the strength at this sprinkler
            var strength = GameMath.Clamp(
                (float)effectiveDistance, 0, 5) / 3f;
            
            if (effectiveDistance >= 0)
            {
                strength += 4f;
                // set how 'far' it could travel.
                particleProps.MinVelocity.X = -strength / 2;
                particleProps.MinVelocity.Z = -strength / 2;
                particleProps.AddVelocity.X = strength;
                particleProps.AddVelocity.Z = strength;
            }
            else // No particle can spawn
                return;

            particleProps.MinVelocity.Y = entity.Block.Code.EndVariant() == "down" ? 1 : 4;
        }

        manager.Spawn(particleProps);
    }
}