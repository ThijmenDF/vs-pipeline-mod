using System.Collections.Generic;
using PipelineMod.Common.Mechanics.Interfaces;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics;

public class PipeNetwork(PipeMod pipeMod, long networkId)
{
    public readonly List<IPipelineNode> nodes = [];

    public readonly List<IPipelineDestination> destinations = [];

    public readonly long networkId = networkId;

    /**
     * Joins the given node to the network.
     */
    public void Join(IPipelineNode node)
    {
        if (nodes.Contains(node)) return;
        nodes.Add(node);
    }

    /**
     * The given node will leave the network.
     */
    public void Leave(IPipelineNode node)
    {
        nodes.Remove(node);
    }

    /**
     * Spread the contents of nodes.
     */
    public void ServerTick(float delta)
    {
        foreach (var node in nodes)
        {
            if (node is IPipelineTicks device)
            {
                // These nodes have their own update method.
                device.Tick(delta);
            }

            if (node is IPipelineSource source) ProcessSource(source);
        }

        foreach (var destination in destinations)
        {
            destination.MarkTickComplete();
        }
    }

    private void ProcessSource(IPipelineSource source)
    {
        // A source needs to run a few cycles:
        // 1. check how many destinations are reachable.
        // Get the output (and empty any buffers)
        var outputProvided = source.GetOutput();
        // pipeMod.Api.Logger.Notification($"Source: {source.GetType().Name}({source.nodeId}) ProvidedOutput {outputProvided}");

        List<IPipelineDestination> availableDestinations = [];
        foreach (var (destination, distance) in source.Destinations)
        {
            // pipeMod.Api.Logger.Notification($"Destination {destination.nodeId}, distance: {distance}, pulling: {destination.ActiveInputDistance}, pushing: {source.ActiveOutputDistance}, outputAvailable: {outputProvided}");

            if (
                outputProvided > 0.005f &&
                (destination.ActiveInputDistance >= distance
                 || source.ActiveOutputDistance >= distance)
            )
            {
                availableDestinations.Add(destination);
            }
            else
            {
                // Provide nothing, updating their state.
                destination.ProvideInput(0f);
            }
        }
        
        // No outflow or no destination? don't do anything else.
        if (availableDestinations.Count == 0) return; 
        
        // 2. divide it's potential output over those destinations
        var outputPotential = outputProvided / availableDestinations.Count;

        // 3. Provide the divided output to each destination.
        foreach (var destination in availableDestinations)
            destination.ProvideInput(outputPotential);
    }

    public void CalculateDistances()
    {
        pipeMod.Api.Logger.Notification("Calculating distances of " + networkId);

        // Look at each device in the network, and walk through the connected nodes to update their distance.
        
        foreach (var node in nodes)
        {
            node.Destinations.Clear();
        }
        
        pipeMod.Api.Logger.Notification("Destinations in network " + networkId + ": " + destinations.Count);

        foreach (var destination in destinations)
        {
            destination.Sources.Clear();
            // Check if their connection sides have a node.
            foreach (var face in destination.GetConnections())
            {
                // pipeMod.Api.Logger.Notification($"Checking {face} for destination {destination.nodeId}");
                if (destination.GetInputSide() != face) continue;
                // pipeMod.Api.Logger.Notification("Face is input side");
                
                var node = destination.GetNeighbour(pipeMod.Api.World, face);
                if (node == null)
                {
                    pipeMod.Api.Logger.Notification("No connection found for " + face);
                    continue;
                }
                
                CheckNode(destination, node, face);
            }
        }
    }
    
    private void CheckNode(IPipelineDestination destination, IPipelineNode node, BlockFacing deviceFace)
    {
        if (node is IPipelineDestination)
        {
            // pipeMod.Api.Logger.Notification("Block on side " + deviceFace + " is a device, don't go further in.");
            return; //we're done here.
        }
        
        node.Destinations.Add(destination, 1);
        
        Queue<IPipelineNode> queue = new();
        queue.Enqueue(node);
        // Don't look back at itself or the destination we're coming from.
        List<IPipelineNode> visitedNodes = [node];
        var firstNode = true;

        while (queue.Count > 0)
        {
            node = queue.Dequeue();
            
            // Check its neighbours, get the highest distance and use the Source from that node.
            var lowest = int.MaxValue - 2;
            if (firstNode)
            {
                lowest = 1;
                firstNode = false;
            }
            
            var markedSources = new List<IPipelineSource>();

            foreach (var face in node.GetConnections())
            {
                var neighbour = node.GetNeighbour(pipeMod.Api.World, face);
                switch (neighbour)
                {
                    // Don't check itself, won't be much point.
                    case null:
                        continue;
                    // If the node is a source, check if the input side is correct
                    case IPipelineSource source:
                    {
                        if (source.GetOutputSide() == face.Opposite && source != destination)
                            markedSources.Add(source);
                        continue;
                    }
                    // Destinations are always skipped
                    case IPipelineDestination:
                        continue;
                }

                // Check if the neighbour has this device
                if (neighbour.Destinations.TryGetValue(destination, out var distance))
                {
                    // If they do, and their distance is lower than our lowest, update our lowest.
                    if (distance < lowest)
                        lowest = distance;
                }

                // Don't use this node further if it's already been checked.
                if (visitedNodes.Contains(neighbour)) continue;
                
                if (neighbour == destination)
                {
                    lowest = 0;
                    continue;
                }
                
                visitedNodes.Add(neighbour);
                queue.Enqueue(neighbour);
            }
            
            // Attached sources have a distance 1 greater than the current node.
            markedSources.ForEach(source => source.Destinations.TryAdd(destination, GameMath.Min(lowest + 2, int.MaxValue)));

            node.Destinations.TryAdd(destination, GameMath.Min(lowest + 1, int.MaxValue));
            
            if (node is IPipelineSource src)
                destination.Sources.Add(src);
        }
    }
    
    
    
    
    
}