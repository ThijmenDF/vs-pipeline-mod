using System.Collections.Generic;
using System.Linq;
using PipelineMod.Common.Mechanics.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace PipelineMod.Common.Mechanics;

public class PipeMod : ModSystem
{
    // private ICoreClientAPI capi;
    // private ICoreServerAPI sapi;
    
    // private IClientNetworkChannel clientNwChannel;
    // private IServerNetworkChannel serverNwChannel;
    public ICoreAPI Api = null!;
    private PipeData data = new();

    private bool allNetworksFullyLoaded = true;

    private List<PipeNetwork> nowFullyLoaded = [];

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Api = api;
        
        // if (api.World is IClientWorldAccessor)
        // {
            // (api as ICoreClientAPI)?.Event
            //     .RegisterRenderer(this, EnumRenderStage.Before, "mechanicalpowertick");
            
            // Register client network handlers
            // clientNwChannel = ((ICoreClientAPI)api).Network
            //     .RegisterChannel("vspipelinenetwork")
                // .RegisterMessageType(typeof(PipelineNetworkPacket))
                // .RegisterMessageType(typeof(NetworkRemovedPacket))
                // .RegisterMessageType(typeof(MechClientRequestPacket))
                // .SetMessageHandler(new NetworkServerMessageHandler<PipelineNetworkPacket>(OnPacket))
                // .SetMessageHandler(new NetworkServerMessageHandler<NetworkRemovedPacket>(OnNetworkRemovePacket))
            // ;
        // }
        // else
        // {
            // Register server network handlers
            
            // serverNwChannel = ((ICoreServerAPI) api).Network
            //     .RegisterChannel("vspipelinenetwork")
                // .RegisterMessageType(typeof (PipelineNetworkPacket))
                // .RegisterMessageType(typeof (NetworkRemovedPacket))
                // .RegisterMessageType(typeof (MechClientRequestPacket))
                // .SetMessageHandler(new NetworkClientMessageHandler<MechClientRequestPacket>(OnClientRequestPacket))
            // ;
        // }
    }

    // public override void StartClientSide(ICoreClientAPI api)
    // {
    //     base.StartClientSide(api);
    //     // capi = api;
    //     //api.Event.LeaveWorld += () => Renderer?.Dispose();
    // }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        
        api.World.RegisterGameTickListener(OnServerGameTick, 1000);
        
        // sapi = api;
        api.Event.SaveGameLoaded += Event_GameWorldLoad;
        api.Event.GameWorldSave += Event_GameWorldSave;
        api.Event.ChunkDirty += Event_ChunkDirty;
        api.Event.ChunkColumnLoaded += Event_ChunkLoaded;
    }

    /**
     * When the client receives this packet.
     */
    // private void OnPacket(PipelineNetworkPacket packet)
    // {
    //     var isNew = !data.networksById.ContainsKey(packet.networkId);
    //     
    //     GetOrCreateNetwork(packet.networkId).UpdateFromPacket(packet, isNew);
    // }

    /**
     * When the client receives the network removed packet.
     */
    // private void OnNetworkRemovePacket(NetworkRemovedPacket packet)
    // {
        // data.networksById.Remove(packet.networkId);
    // }

    /**
     * When the server receives the network request packet.
     */
    // private void OnClientRequestPacket(IServerPlayer player, MechClientRequestPacket packet)
    // {
        // if (! data.networksById.TryGetValue(packet.networkId, out var network))
            // return;
        // network.SendBlocksUpdateToClient(player);
    // }



    /**
     * Retrieves or creates a network by id. Also tests if it's fully loaded or not.
     */
    public PipeNetwork GetOrCreateNetwork(long networkId)
    {
        if (!data.networksById.TryGetValue(networkId, out var network))
            data.networksById[networkId] = network = new PipeNetwork(this, networkId);
        
        TestFullyLoaded(network);
        return network;
    }

    /**
     * Tests the given network for if it's fully loaded (server side only).
     * Updates the allNetworksFullyLoaded
     */
    public void TestFullyLoaded(PipeNetwork network)
    {
        if (Api.Side != EnumAppSide.Server || network.fullyLoaded)
            return;

        network.fullyLoaded = network.TestFullyLoaded(Api);
        allNetworksFullyLoaded &= network.fullyLoaded;
        Api.Logger.Notification("Network " + network.networkId + " fully loaded: " + network.fullyLoaded + ", allFullLoaded: "  + allNetworksFullyLoaded);
    }

    public PipeNetwork CreateNetwork()
    {
        var network = new PipeNetwork(this, data.nextNetworkId)
        {
            fullyLoaded = true
        };
        
        data.networksById.Add(data.nextNetworkId, network);
        ++data.nextNetworkId;
        return network;
    }

    public void DeleteNetwork(PipeNetwork network)
    {
        Api.Logger.Notification("Network " + network.networkId + " deleted");
        data.networksById.Remove(network.networkId);
        // var packet = new NetworkRemovedPacket()
        // {
        //     networkId = network.networkId
        // };
        // var players = Array.Empty<IServerPlayer>();
        // serverNwChannel.BroadcastPacket(packet, players);
    }


    private void Event_GameWorldSave()
    {
    }
    
    private void Event_GameWorldLoad() => data = new PipeData();

    private void Event_ChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
    {
        if (allNetworksFullyLoaded || reason == EnumChunkDirtyReason.MarkedDirty)
            return;
        
        Api.Logger.Notification("ChunkDirty because:" + reason);
        
        allNetworksFullyLoaded = true;
        nowFullyLoaded.Clear();

        foreach (var network in data.networksById.Values)
        {
            if (network.fullyLoaded) continue;
            
            allNetworksFullyLoaded = false;
            if (!network.inChunks.ContainsKey(chunkCoord)) continue;
            
            TestFullyLoaded(network);
            if (network.fullyLoaded) 
                nowFullyLoaded.Add(network);
        }
        
        for (var i = 0; i < nowFullyLoaded.Count; ++i)
        {
            RebuildNetwork(nowFullyLoaded[i]);
            CalculateDistances(nowFullyLoaded[i]);
        }
    }

    private void Event_ChunkLoaded(Vec2i columnCoord, IWorldChunk[] chunks)
    {
        // Api.Logger.Notification("Chunk column loaded: " + columnCoord);
        
        // TestFullyLoaded();
    }

    public void RebuildNetwork(PipeNetwork network, IPipelineNode? startNode = null)
    {
        if (network.nodes.Count == 0)
            return;

        Api.Logger.Notification("Rebuilding network " + network.networkId);
        var array = network.nodes.Values.ToArray();

        foreach (var node in array)
            node.LeaveNetwork();

        BuildNetwork(network, startNode ?? array[0]);
    }


    private void OnServerGameTick(float delta)
    {
        ++data.tickNumber;
        
        foreach (var network in data.networksById.Values)
        {
            if (network.fullyLoaded && network.nodes.Count > 0)
            {
                network.ServerTick(delta);
            }
        }
    }
    
    
    // we'll get to this later
    // public void SendNetworkBlocksUpdateRequestToServer(long networkId)
    // {
    //     clientNwChannel.SendPacket(new MechClientRequestPacket()
    //     {
    //         networkId = networkId
    //     });
    // }

    // public void OnRenderFrame(float delta, EnumRenderStage stage)
    // {
    //     if (capi.IsGamePaused) return;
    //
    //     foreach (var network in data.networksById.Values)
    //         network.ClientTick(delta);
    // }

    //public double RenderOrder => 0.0;
    //public int RenderRange => 9999;


    public void BuildNetwork(PipeNetwork network, IPipelineNode startNode)
    {
        Api.Logger.Notification("Building network " + network.networkId);
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
        }
        
        TestFullyLoaded(network);
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
                if (node is IPipelineDestination)
                {
                    CalculateDistances(network);
                }
                
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
            CalculateDistances(network);
        }
    }

    /**
     * Deletes the network (and tells the clients it is), then removes all its nodes from the network.
     */
    public void DissolveNetwork(PipeNetwork network)
    {
        DeleteNetwork(network);
            
        // Dissolve the entire network
        foreach (var node in network.nodes.Values)
        {
            node.LeaveNetwork();
        }
    }

    public void CalculateDistances(PipeNetwork network)
    {
        network.CalculateDistances();
    }

    
    
    
}