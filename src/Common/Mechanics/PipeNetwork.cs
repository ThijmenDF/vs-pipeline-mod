using System.Collections.Generic;
using System.Linq;
using PipelineMod.Common.Mechanics.Interfaces;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics;

[ProtoContract]
public class PipeNetwork(PipeMod pipeMod, long networkId)
{
    public readonly Dictionary<BlockPos, IPipelineNode> nodes = new();

    [ProtoMember(1)]
    public readonly long networkId = networkId;
    
    // A list of chunks and how many nodes are found in them.
    [ProtoMember(2)]
    public readonly Dictionary<Vec3i, int> inChunks = new();

    private const int chunkSize = 32;

    public bool fullyLoaded;

    /**
     * Joins the given node to the network.
     */
    public void Join(IPipelineNode node)
    {
        var position = node.GetPosition();
        nodes[position] = node;
        
        // Track the chunk this node is found in, setting how many parts are found in the chunk.
        var key = new Vec3i(position.X / chunkSize, position.Y / chunkSize, position.Z / chunkSize);
        inChunks.TryGetValue(key, out var num);
        inChunks[key] = num + 1;
    }

    /**
     * The given node will leave the network.
     */
    public void Leave(IPipelineNode node)
    {
        var position = node.GetPosition();
        nodes.Remove(position);
        
        var key = new Vec3i(position.X / chunkSize, position.Y / chunkSize, position.Z / chunkSize);
        inChunks.TryGetValue(key, out var num);
        if (num <= 1)
            inChunks.Remove(key);
        else
            inChunks[key] = num - 1;
    }

    /**
     * Spread the contents of nodes.
     */
    public void ServerTick(float delta)
    {
        foreach (var node in nodes.Values)
        {
            if (node is IPipelineTicks device)
            {
                // The devices have their own update method.
                device.Tick(delta);
            }
            
        }
    }

    /**
     * Finds the first source in range for the destination.
     */
    private void FindSourceFor(IPipelineDestination destination)
    {
        var hadSource = destination.NumSources;
        destination.NumSources = 0;
        
        foreach (var node in nodes.Values)
        {
            if (node is not IPipelineSource source) continue;

            if (node == destination) // cannot loop onto itself.
                continue;

            // Inactive sources cannot be pulled from.
            if (!source.CanBeActive) continue;
            
            // Find the distance between the destination and the source.
            // The source should have a record in its device list of this destination.
            node.Devices.TryGetValue(destination, out var distance);
            
            pipeMod.Api.Logger.Notification($"destination: {destination.GetType().Name} and source {source.GetType().Name} in {networkId}, distance: {distance}, activeInputDistance: {destination.ActiveInputDistance}, activeOutputDistance: {source.ActiveOutputDistance}");

            // distance may be 0 if there is no device found, so check that first.
            if (distance > 0 && (
                distance <= destination.ActiveInputDistance // How far the destination can pull
                || distance <= source.ActiveOutputDistance // How far the source can push
                ))
            {
                pipeMod.Api.Logger.Notification($"Found source for destination {destination.GetType().Name} in {networkId}: {source.GetType().Name}");
                source.NumDestinations += 1;
                destination.NumSources += 1;
                // We keep going because there may be more sources that need updating.
            }
        }

        if (hadSource != destination.NumSources)
            destination.MarkDirty();
    }

    /**
     * Checks all destinations and checks if they have a valid source.
     */
    public void UpdateDestinations()
    {
        var previousDestinations = new Dictionary<IPipelineSource, int>();
        foreach (var node in nodes.Values)
        {
            if (node is not IPipelineSource source) continue;
            previousDestinations[source] = source.NumDestinations;
            source.NumDestinations = 0;
        }
        
        foreach (var node in nodes.Values)
        {
            if (node is not IPipelineDestination destination) continue;
            
            // Check if the destination can find a source.
            FindSourceFor(destination);
        }

        foreach (var kvp in previousDestinations)
        {
            if (kvp.Key.NumDestinations != kvp.Value)
                kvp.Key.MarkDirty();
        }
    }


    public bool TestFullyLoaded(ICoreAPI api)
    {
        return inChunks.Keys.All(
            key => api.World.BlockAccessor.GetChunk(key.X, key.Y, key.Z) != null
        );
    }


    public void DidUnload() => fullyLoaded = false;

    public void CalculateDistances()
    {
        if (!fullyLoaded) return;
        pipeMod.Api.Logger.Notification("Calculating distances of " + networkId);

        // Look at each device in the network, and walk through the connected nodes to update their distance.
        
        List<IPipelineDestination> devices = [];
        foreach (var node in nodes.Values)
        {
            node.Devices.Clear();
            
            if (node is IPipelineDestination device)
            {
                devices.Add(device);
            }
        }
        
        pipeMod.Api.Logger.Notification("Devices in network " + networkId + ": " + devices.Count);

        if (devices.Count == 0)
        {
            UpdateDestinations();
            return;
        }

        foreach (var device in devices)
        {
            // Check if their connection sides have a node.
            foreach (var face in device.GetConnections())
            {
                if (device.GetInputSide() != face) continue;
                
                var node = device.GetNeighbour(pipeMod.Api.World, face);
                if (node == null)
                {
                    pipeMod.Api.Logger.Notification("No connection found for " + face);
                    continue;
                }
                
                CheckNode(device, node, face);
            }
        }
        
        UpdateDestinations();
    }
    
    private void CheckNode(IPipelineDestination destination, IPipelineNode node, BlockFacing deviceFace)
    {
        node.Devices.Add(destination, 1);
        
        if (node is IPipelineDestination)
        {
            pipeMod.Api.Logger.Notification("Block on side " + deviceFace + " is a device, don't go further in.");
            return; //we're done here.
        }
        
        Queue<IPipelineNode> queue = new();
        List<IPipelineNode> visitedNodes = [node];

        foreach (var face in node.GetConnections())
        {
            var neighbour = node.GetNeighbour(pipeMod.Api.World, face);
            // Don't use the itself as a potential target.
            if (neighbour == null || neighbour == destination) continue;

            
            // Special rules for destinations
            if (neighbour is IPipelineDestination &&
                (
                    neighbour is not IPipelineSource || // No source? skip.
                    neighbour is IPipelineSource source && source.GetOutputSide() != face.Opposite // is a source, but wrong end? skip
                )
            )
            {
                pipeMod.Api.Logger.Notification("Block on side " + deviceFace + " is a destination and either no source, or a source at the wrong side.");
                continue;
            }
            
            queue.Enqueue(neighbour);
            visitedNodes.Add(neighbour);
        }

        while (queue.Count > 0)
        {
            node = queue.Dequeue();
            
            // Check its neighbours, get the highest distance and use the Source from that node.
            var lowest = int.MaxValue - 1;

            foreach (var face in node.GetConnections())
            {
                var neighbour = node.GetNeighbour(pipeMod.Api.World, face);
                // Don't check itself, won't be much point.
                if (neighbour == null || neighbour == destination) continue;

                // Check if the neighbour has this device
                if (neighbour.Devices.TryGetValue(destination, out var distance))
                {
                    // If they do, and their distance is lower than our lowest, update our lowest.
                    if (distance < lowest)
                        lowest = distance;
                }
                
                // Don't enqueue the neighbour if it's already been checked, or if it's a device, because we don't go through devices.
                if (visitedNodes.Contains(neighbour) || node is IPipelineDestination or IPipelineSource) continue;
                
                visitedNodes.Add(neighbour);
                queue.Enqueue(neighbour);
            }

            node.Devices.TryAdd(destination, GameMath.Min(lowest + 1, int.MaxValue - 1));
        }
    }
    
    
    
    
    
}