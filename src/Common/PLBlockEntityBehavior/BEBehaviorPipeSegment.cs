using System;
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

    private readonly Dictionary<string, Tuple<string, Vec3f>> modelConfig = new()
    {
        // Order is N E S W U D
        // Dict as connectionString -> filename + rotation
        
        // 2-sided
        // Straight
        ["ns"] = Tuple.Create("ns", new Vec3f(0, 0, 0)), // north south 
        ["ew"] = Tuple.Create("ns", new Vec3f(0, 90, 0)),  // east west 
        ["ud"] = Tuple.Create("ns", new Vec3f(90, 0, 0)), // up down
        // Elbow horizontal
        ["ne"] = Tuple.Create("ne", new Vec3f(0, 0, 0)), // north east
        ["nw"] = Tuple.Create("ne", new Vec3f(0, 90, 0)), // north west
        ["es"] = Tuple.Create("ne", new Vec3f(0, -90, 0)), // east south
        ["sw"] = Tuple.Create("ne", new Vec3f(0, 180, 0)), // south west
        // Elbow vertical up
        ["nu"] = Tuple.Create("ne", new Vec3f(0, 0, 90)), // north up
        ["eu"] = Tuple.Create("ne", new Vec3f(0, -90, 90)), // east up
        ["su"] = Tuple.Create("ne", new Vec3f(0, 180, 90)), // south up
        ["wu"] = Tuple.Create("ne", new Vec3f(0, 90, 90)), // west up
        // Elbow vertical down
        ["nd"] = Tuple.Create("ne", new Vec3f(0, 0, -90)), // north down 
        ["ed"] = Tuple.Create("ne", new Vec3f(0, -90, -90)), // east down
        ["sd"] = Tuple.Create("ne", new Vec3f(0, 180, -90)), // south down
        ["wd"] = Tuple.Create("ne", new Vec3f(0, 90, -90)), // west down
        
        // 3-sided
        // T piece horizontal x 3
        ["nes"] = Tuple.Create("nes", new Vec3f(0, 0, 0)), // north east south
        ["new"] = Tuple.Create("nes", new Vec3f(0, 90, 0)), // north east west
        ["nsw"] = Tuple.Create("nes", new Vec3f(0, 180, 0)), // north south west
        ["esw"] = Tuple.Create("nes", new Vec3f(0, -90, 0)), // east south west
        // T piece vertical x 2 horizontal x 1
        ["nud"] = Tuple.Create("nes", new Vec3f(-90, 0, 90)), // north up down
        ["eud"] = Tuple.Create("nes", new Vec3f(90, 0, 0)), // east up down
        ["sud"] = Tuple.Create("nes", new Vec3f(90, 0, 90)), // south up down
        ["wud"] = Tuple.Create("nes", new Vec3f(90, 180, 0)), // west up down
        // T piece vertical x 1 horizontal x 2
        ["nsu"] = Tuple.Create("nes", new Vec3f(0, 0, 90)), // north south up
        ["nsd"] = Tuple.Create("nes", new Vec3f(0, 0, -90)), // north south down
        ["ewu"] = Tuple.Create("nes", new Vec3f(0, 90, 90)), // east west up
        ["ewd"] = Tuple.Create("nes", new Vec3f(0, 90, -90)), // east west down
        // Corner piece, up
        ["neu"] = Tuple.Create("neu", new Vec3f(0, 0, 0)), // north east up
        ["nwu"] = Tuple.Create("neu", new Vec3f(0, 90, 0)), // north west up
        ["esu"] = Tuple.Create("neu", new Vec3f(0, -90, 0)), // east south up
        ["swu"] = Tuple.Create("neu", new Vec3f(0, 180, 0)), // south west up
        // Corner piece, down
        ["ned"] = Tuple.Create("neu", new Vec3f(0, -90, 180)), // north east down
        ["nwd"] = Tuple.Create("neu", new Vec3f(0, 0, 180)), // north west down
        ["esd"] = Tuple.Create("neu", new Vec3f(0, 180, 180)), // east south down
        ["swd"] = Tuple.Create("neu", new Vec3f(0, 90, 180)), // south west down
        
        // 4-sided
        // cross piece
        ["nesw"] = Tuple.Create("nesw", new Vec3f(0, 0, 0)), // north east south west
        ["nsud"] = Tuple.Create("nesw", new Vec3f(0, 0, 90)), // north south up down
        ["ewud"] = Tuple.Create("nesw", new Vec3f(90, 0, 0)), // east west up down
        // Four-way, corner + straight, north-south
        ["nesu"] = Tuple.Create("nesu", new Vec3f(0, 0, 0)), // north east south up
        ["nesd"] = Tuple.Create("nesu", new Vec3f(0, 0, -90)), // north east south down
        ["nswu"] = Tuple.Create("nesu", new Vec3f(0, 0, 90)), // north south west up
        ["nswd"] = Tuple.Create("nesu", new Vec3f(0, 0, 180)), // north south west down
        // Four-way, corner + straight, east-west
        ["newu"] = Tuple.Create("nesu", new Vec3f(0, 90, 0)), // north east west up
        ["newd"] = Tuple.Create("nesu", new Vec3f(-90, 90, 0)), // north east west down
        ["eswu"] = Tuple.Create("nesu", new Vec3f(0, -90, 0)), // east south west up
        ["eswd"] = Tuple.Create("nesu", new Vec3f(90, -90, 0)), // east south west down
        // Four-way, corner + straight, up-down
        ["neud"] = Tuple.Create("nesu", new Vec3f(-90, 0, 0)), // north east up down
        ["nwud"] = Tuple.Create("nesu", new Vec3f(-90, 0, 90)), // north west up down <---
        ["esud"] = Tuple.Create("nesu", new Vec3f(90, 0, 0)), // east south up down
        ["swud"] = Tuple.Create("nesu", new Vec3f(90, 0, 90)), // south west up down <---
        
        // 5-sided
        // horizontal cross + vertical handle
        ["neswu"] = Tuple.Create("neswu", new Vec3f(0, 0, 0)), // north east south west up
        ["neswd"] = Tuple.Create("neswu", new Vec3f(180, 0, 0)), // north east south west down
        // vertical cross + horizontal handle
        ["nesud"] = Tuple.Create("neswu", new Vec3f(0, 0, -90)), // north east south up down
        ["newud"] = Tuple.Create("neswu", new Vec3f(-90, 0, 0)), // north east west up down
        ["nswud"] = Tuple.Create("neswu", new Vec3f(0, 0, 90)), // north south west up down
        ["eswud"] = Tuple.Create("neswu", new Vec3f(90, 0, 0)), // east south west up down
        
        // 6-sided
        ["neswud"] = Tuple.Create("neswud", new Vec3f(0, 0, 0)), // north east south west up down

    };

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        if (api.Side == EnumAppSide.Client)
            UpdateConnections();
    }

    protected override void UpdateConnections()
    {
        if (Api is not ICoreClientAPI capi) return;
        
        
        var code = GetConnectionString();
        if (code == "") code = "ns";
        
        if (meshData.TryGetValue(code, out var value))
        {
            CurrentMesh = value;
            Blockentity.MarkDirty(true);
            return;
        }


        if (!modelConfig.TryGetValue(code, out var config))
        {
            Api.Logger.Warning("Unable to locate pipe shape config for " + code);
            return;
        }
        
        var shapePath = $"pipelinemod:shapes/pipes/pipesection-{config.Item1}.json";;
        var rotation = config.Item2;

        var shape = capi.Assets.TryGet(new AssetLocation(shapePath)).ToObject<Shape>();

        capi.Tesselator.TesselateShape(Block, shape, out var mesh, rotation);
        
        meshData.Add(code, mesh);
        CurrentMesh = mesh;
        
        Blockentity.MarkDirty(true);
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