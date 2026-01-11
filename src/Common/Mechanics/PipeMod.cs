using System.Collections.Generic;
using PipelineMod.Common.Mechanics.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace PipelineMod.Common.Mechanics;

public class PipeMod : ModSystem
{
    public ICoreAPI Api = null!;
    private PipeData data = new();

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Api = api;
    }


    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        
        api.World.RegisterGameTickListener(OnServerGameTick, 1000);
        
        // sapi = api;
        api.Event.SaveGameLoaded += Event_GameWorldLoad;
        api.Event.GameWorldSave += Event_GameWorldSave;
        api.Event.ChunkDirty += Event_ChunkDirty;
    }

    public int GetNodeId() => data.nextNodeId++;

    public PipeNetwork CreateNetwork()
    {
        var network = new PipeNetwork(this, data.nextNetworkId);
        
        data.networksById.Add(data.nextNetworkId, network);
        ++data.nextNetworkId;
        return network;
    }

    public void DeleteNetwork(PipeNetwork network)
    {
        Api.Logger.Notification("Network " + network.networkId + " deleted");
        data.networksById.Remove(network.networkId);
    }


    private void Event_GameWorldSave()
    {
    }
    
    private void Event_GameWorldLoad() => data = new PipeData();

    private void Event_ChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
    {
        if (reason == EnumChunkDirtyReason.MarkedDirty)
            return;
        
        // Api.Logger.Warning("ChunkDirty because:" + reason);
    }

    public void RebuildNetwork(PipeNetwork network, IPipelineNode? startNode = null)
    {
        if (network.nodes.Count == 0)
            return;

        Api.Logger.Notification("Rebuilding network " + network.networkId);
        var array = network.nodes.ToArray();

        foreach (var node in array)
            node.LeaveNetwork();

        BuildNetwork(network, startNode ?? array[0]);
    }


    private void OnServerGameTick(float delta)
    {
        ++data.tickNumber;
        
        foreach (var network in data.networksById.Values)
        {
            if (network.nodes.Count > 0)
            {
                network.ServerTick(delta);
            }
        }
    }


    public void BuildNetwork(PipeNetwork network, IPipelineNode startNode)
    {
        Api.Logger.Notification("Building network " + network.networkId);
        network.destinations.Clear();
        Queue<IPipelineNode> queue = new();
        queue.Enqueue(startNode);
        
        startNode.JoinNetwork(network);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();

            foreach (var face in node.GetConnections())
            {
                // Get the entity at the position, then fetch the behavior
                var neighbour = node.GetNeighbour(Api.World,  face);

                // No pipeline node.
                if (neighbour == null) continue;

                // Already has a network so we don't check it.
                if (neighbour.Network != null)
                    continue;

                neighbour.JoinNetwork(network);
                queue.Enqueue(neighbour);
            }
            
            if (node is IPipelineDestination destination) 
                network.destinations.Add(destination);
        }
        
        network.CalculateDistances();
    }

    /**
     * Rebuilds the connected networks and creates new ones if necessary. 
     */
    public void OnNodeRemoved(IPipelineNode node)
    {
        if (node.Network != null)
        {
            // First check is to see how many connections this node has.
            // If it's just one, it means we removed the end of the line and we can simply
            // kick the node from network.
            if (node.GetConnections().Count <= 1)
            {
                var network = node.Network;
                node.LeaveNetwork();
                
                // Remove the destination if it is one.
                if (node is IPipelineDestination destination)
                    network.destinations.Remove(destination);
                
                Api.Logger.Notification("Node was of type " + node.GetType().Name);
                // If it's neither a destination nor source, continue
                if (node is not IPipelineDestination && node is not IPipelineSource)
                {
                    Api.Logger.Notification("It was neither a source or destination");
                    return;
                }
                
                // if it IS either one, recalculate the distances.
                network.CalculateDistances();

                return;
            }
            
            DissolveNetwork(node.Network);
        }
        
        foreach (var face in node.GetConnections())
        {
            var neighbour = node.GetNeighbour(Api.World, face);

            if (neighbour == null) continue;
            
            // Be sure to disconnect the neighbour from this node.
            // neighbour.RemoveConnection(face.Opposite);
            
            // If this neighbour suddenly has a network, it means it's already part of the freshly created network.
            if (neighbour.Network != null) continue;
            
            var network = CreateNetwork();
            neighbour.JoinNetwork(network);
            BuildNetwork(network, neighbour);
            network.CalculateDistances();
        }
    }

    /**
     * Deletes the network (and tells the clients it is), then removes all its nodes from the network.
     */
    public void DissolveNetwork(PipeNetwork network)
    {
        DeleteNetwork(network);
        
        var nodes = network.nodes.ToArray();
        // Dissolve the entire network
        foreach (var node in nodes)
        {
            node.LeaveNetwork();
        }
    }
}