using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntityRenderer;

/**
 * Given the asset location and base block for textures, this component loads the shape into the buffer.
 *
 * You also need to provide callback functions for transformation on the X and Z axis, as well as the 'centre' point
 * An optional translation function can also be provided.
 *
 * UseInitialTransform was taken from the source (CreativeRotorRenderer)
 */
public class PipelineMPRComponent
{
    public readonly MeshRef Mesh;
    public readonly CustomMeshDataPartFloat Buffer;
    public required System.Func<IPipelineMPPumpEngineRenderable, float> RotXFunc;
    public required System.Func<IPipelineMPPumpEngineRenderable, float> RotZFunc;
    public required System.Func<IPipelineMPPumpEngineRenderable, Vec3f> AxisFunc;
    public System.Func<IPipelineMPPumpEngineRenderable, Vec3f>? TranslationFunc;
    public bool UseInitialTransform;

    public PipelineMPRComponent(ICoreClientAPI api, CompositeShape shapeLoc, Block textureSource, string assetLocation, int count)
    {
        var shapePath = new AssetLocation(assetLocation);
        var shape = Shape.TryGet(api, shapePath);
        var meshRotationDeg = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);
        api.Tesselator.TesselateShape(textureSource, shape, out var modelData, meshRotationDeg);

        modelData.CustomFloats = Buffer = createCustomFloats(count);
        Mesh = api.Render.UploadMesh(modelData);
    }

    private CustomMeshDataPartFloat createCustomFloats(int count)
    {
        CustomMeshDataPartFloat data = new(count)
        {
            Instanced = true,
            InterleaveOffsets = [0, 16, 32, 48, 64],
            InterleaveSizes = [4, 4, 4, 4, 4],
            InterleaveStride = 80,
            StaticDraw = false
        };
        data.SetAllocationSize(count);
        return data;
    }
}