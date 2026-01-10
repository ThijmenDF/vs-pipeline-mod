using System;
using PipelineMod.Common.PLBlockEntityBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockBehavior;

public class BlockBehaviorPipeInlet(Block block) : BlockBehavior(block)
{
    
    private static readonly NatFloat randomOffset;
    private readonly Random random = new();

    private static readonly AdvancedParticleProperties particleProps = new();
    
    private static IClientWorldAccessor? world;
    private static readonly int baseColor = ColorUtil.ColorFromRgba(255, 255, 255, 0);

    static BlockBehaviorPipeInlet()
    {
        randomOffset = new NatFloat(0f, 0.5f, EnumDistribution.INVERSEGAUSSIAN);
        
        particleProps.Color = baseColor;
        particleProps.HsvaColor = null;
        particleProps.GravityEffect = NatFloat.createUniform(0f, 0f);
        var velocity = NatFloat.createUniform(0f, 0f);
        particleProps.Velocity = [velocity, velocity, velocity];
        var evolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, 1f);
        particleProps.VelocityEvolve = [evolve, evolve, evolve];
        particleProps.ParticleModel = EnumParticleModel.Quad;
        particleProps.LifeLength = NatFloat.createUniform(1f, 0.5f);
        particleProps.Size = NatFloat.createUniform(0.5f, 0f);

        particleProps.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, 10f);
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        if (api is ICoreClientAPI capi)
            world = capi.World;
    }


    public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
    {
        var behavior = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorPipeInlet>();
        if (behavior == null || ! behavior.ShouldInletBeActive())
            return base.ShouldReceiveClientParticleTicks(world, byPlayer, pos, ref handling);

        handling = EnumHandling.Handled;
        return true;
    }

    public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
    {
        var offset = new Vec3f(randomOffset.nextFloat(), random.NextSingle() * 0.2f - 0.1f, randomOffset.nextFloat());
        var position = pos.ToVec3d().Add(0.5d).Add(offset);
        particleProps.basePos.Set(position);
        
        // Calculate the angle to which the particle needs to move in order to reach the centre.
        particleProps.baseVelocity = new Vec3f(-offset.X * 0.5f, -offset.Y, -offset.Z * 0.5f);

        // Adjust the color
        if (world != null)
            particleProps.Color = world.ApplyColorMapOnRgba("climateWaterTint", null, baseColor, pos.X, pos.Y, pos.Z);
        
        manager.Spawn(particleProps);
    }
}
