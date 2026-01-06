using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public class BEBehaviorPipeSegment(BlockEntity blockentity) : BEBehaviorPipeBase(blockentity)
{
    private readonly Dictionary<string, MeshData> meshData = new();
    
    public MeshData? CurrentMesh { get; private set; }

    private BlockFacing? visualFakeFace;

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        if (api.Side == EnumAppSide.Client)
            UpdateConnections();
    }

    protected override void UpdateConnections()
    {
        if (Api is not ICoreClientAPI capi) return;
        
        Blockentity.MarkDirty(true);
        
        var code = GetConnectionString();
        if (meshData.TryGetValue(code, out var value))
        {
            CurrentMesh = value;
            return;
        }

        var codePrefixed = code == "" ? code : "-" + code;
        var shapePath = $"pipelinemod:shapes/pipes/pipesection{codePrefixed}.json";

        var shape = capi.Assets.TryGet(new AssetLocation(shapePath)).ToObject<Shape>();

        capi.Tesselator.TesselateShape(Block, shape, out var mesh);
        
        meshData.Add(code, mesh);
        CurrentMesh = mesh;
    }
    
    private string GetConnectionString()
    {
        var list = connections.ToList();
        
        if (connections.Count == 1)
        {
            // Show the opposite side too if there's only a single connection.
            visualFakeFace = connections[0].Opposite;
            list.Add(visualFakeFace);
        }
        else
        {
            visualFakeFace = null;
        }
        
        // Sort the list.
        list.Sort((faceOne, faceTwo) => faceOne.Index.CompareTo(faceTwo.Index));
        
        // Join into a single string, only using the first letter.
        return string.Join("", list.Select(face => face.Code[0]));
        
    }
    // 5.5 to 10.5 in all directions
    private readonly Cuboidf hubBox = new(0.34375f, 0.34375f, 0.34375f, 0.65625f, 0.65625f, 0.65625f);
    
    private readonly Cuboidf faceBox = new(0f, 0.34375f, 0.34375f, 0.34375f, 0.65625f, 0.65625f);

    private readonly Vec3i[] angles =
    [
        new(0, -90, 0),  // north
        new(0, 180, 0),  // east
        new(0, 90, 0),   // south
        new(0, 0, 0),    // west (default)
        new(0, 0, -90),  // up
        new(0, 0, 90),   // down
    ];

    private readonly Vec3d origin = new(0.5d, 0.5d, 0.5d);

    public Cuboidf[] GetSelectionBoxes()
    {
        var list = new List<Cuboidf> { hubBox };

        foreach (var connection in connections)
        {
            list.Add(faceBox.RotatedCopy(angles[connection.Index], origin));
        }

        if (visualFakeFace != null)
        {
            list.Add(faceBox.RotatedCopy(angles[visualFakeFace.Index], origin));
        }
        
        return list.ToArray();
    }
}