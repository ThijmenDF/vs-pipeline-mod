#nullable disable
using System.Collections.Generic;
using System.Linq;
using PipelineMod.Common.Mechanics.Interfaces;
using PipelineMod.Common.Mechanics.Packets;
using PipelineMod.Common.PLBlockEntityBehavior;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace PipelineMod.Common.Mechanics;

[ProtoContract]
public class PipeNetwork
{
    public Dictionary<BlockPos, IPipelineNode> nodes = new();

    private PipeMod pipeMod;
    
    [ProtoMember(1)]
    public long networkId;
    
    // A list of chunks and how many nodes are found in them.
    [ProtoMember(2)]
    public Dictionary<Vec3i, int> inChunks = new();

    private const int chunkSize = 32;

    public bool fullyLoaded;

    private bool firstTick = true;

    public PipeNetwork(PipeMod pipeMod, long networkId)
    {
        this.networkId = networkId;
        Init(pipeMod);
    }

    public void Init(PipeMod pipeMod)
    {
        this.pipeMod = pipeMod;
    }

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

    public void ClientTick(float delta)
    {
        if (firstTick)
        {
            firstTick = false;
            pipeMod.SendNetworkBlocksUpdateRequestToServer(networkId);
        }
        
        // todo: handle animations and effects
    }

    /**
     * Spread the contents of nodes.
     */
    public void ServerTick(float delta, long tickNumber)
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


    public void SendBlocksUpdateToClient(IServerPlayer player)
    {
        foreach (var node in nodes.Values)
        {
            if (node is BEBehaviorPipeBase behavior)
                behavior.Blockentity.MarkDirty();
        }
    }

    public void UpdateFromPacket(PipelineNetworkPacket packet, bool isNew)
    {
        // todo update the state of the network
    }

    public bool TestFullyLoaded(ICoreAPI api)
    {
        return inChunks.Keys.All(
            key => api.World.BlockAccessor.GetChunk(key.X, key.Y, key.Z) != null
        );
    }

    public void ReadFromTreeAttribute(ITreeAttribute tree)
    {
        networkId = tree.GetLong("networkId");
    }

    public void WriteToTreeAttribute(ITreeAttribute tree)
    {
        tree.SetLong("networkId", networkId);
    }


    public void DidUnload(IPipelineNode device) => fullyLoaded = false;

    internal void AwaitChunkThenDiscover(Vec3i missingChunkPos)
    {
        inChunks[missingChunkPos] = 1;
        fullyLoaded = false;
    }
}