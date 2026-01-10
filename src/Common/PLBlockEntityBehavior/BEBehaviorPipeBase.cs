using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using PipelineMod.Common.Mechanics;
using PipelineMod.Common.Mechanics.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.PLBlockEntityBehavior;

public abstract class BEBehaviorPipeBase(BlockEntity blockentity) : BlockEntityBehavior(blockentity), IPipelineNode
{

    protected PipeMod? manager;
    
    protected PipeNetwork? network;

    public int nodeId { get; private set; }
    
    // Stored separately for save/load purposes.
    public long NetworkId { get; private set; }

    public PipeNetwork? Network
    {
        get => network;
        set
        {
            network = value;
            NetworkId = network?.networkId ?? 0L;
        }
    }

    public BlockPos Position => Blockentity.Pos;
    
    protected readonly List<BlockFacing> connections = [];

    public Dictionary<IPipelineDestination, int> Destinations { get; } = [];

    public BlockPos GetPosition() => Position;

    private bool loadedFromChunk;

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        
        manager = Api.ModLoader.GetModSystem<PipeMod>();

        // if (api.Side == EnumAppSide.Client)
        // {
        //     findSourceBlock(api as ICoreClientAPI);
        // }

        if (api.Side == EnumAppSide.Server && loadedFromChunk) // Can initialize as this block wasn't placed but loaded from an existing chunk.
            CreateJoinAndDiscoverNetwork();

        if (api.Side == EnumAppSide.Server)
            nodeId = manager.GetNodeId();

    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        CreateJoinAndDiscoverNetwork();
    }

    public void JoinNetwork(PipeNetwork newNetwork)
    {
        if (network != null && network != newNetwork)
            LeaveNetwork();

        if (network == null)
        {
            network = newNetwork;
            newNetwork.Join(this);
        }

        NetworkId = newNetwork.networkId;
        Blockentity.MarkDirty();
    }

    public void LeaveNetwork()
    {
        network?.Leave(this);
        network = null;
        NetworkId = 0L;
        Blockentity.MarkDirty();
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        
        // Remove the connections
        foreach (var face in connections)
        {
            var node = GetNeighbour(Api.World, face);
            node?.RemoveConnection(face.Opposite);
        }
        
        if (network != null && Api.Side == EnumAppSide.Server)
            manager?.OnNodeRemoved(this);
        
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        network?.DidUnload();
        Api.Logger.Notification("Pipe segment was unloaded, network fullyLoaded = false");
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        
        dsc.AppendLine("PipeNetworkID: " + NetworkId);
        dsc.AppendLine("Connections: " + GetConnections().Join(connection => connection.Code));
        dsc.AppendLine("NodeID: " + nodeId);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        
        if (worldAccessForResolve.Side == EnumAppSide.Client) NetworkId = tree.GetLong("networkId");

        // Set connections
        connections.Clear();
        foreach (var face in BlockFacing.ALLFACES)
        {
            if (tree.GetBool("connected" + face.Code))
            {
                connections.Add(face);
            }
        }
        
        nodeId = tree.GetInt("nodeId");

        if (worldAccessForResolve.Side == EnumAppSide.Client)
        {
            UpdateConnections();
        }
        
        Blockentity.MarkDirty();

        if (worldAccessForResolve.Side == EnumAppSide.Server)
            loadedFromChunk = true;
    }

    /*private void findSourceBlock(ICoreClientAPI? api)
    {
        if (sourcePos == null || api == null)
        {
            Source = null;
            return;
        }
        
        Source = api.World.BlockAccessor.GetBlockEntity(sourcePos)?.GetBehavior<IPipelineDevice>();
    }*/

    protected virtual void UpdateConnections()
    {}

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetLong("networkId", NetworkId);

        foreach (var face in BlockFacing.ALLFACES)
        {
            tree.SetBool("connected" + face.Code, connections.Contains(face));
        }
        
        tree.SetInt("nodeId", nodeId);
    }

    public List<BlockFacing> GetConnections()
    {
        return connections;
    }

    public IPipelineNode? GetNeighbour(IWorldAccessor world, BlockFacing face)
    {
        return world.BlockAccessor
            .GetBlockEntity(GetPosition().AddCopy(face))
            ?.GetBehavior<IPipelineNode>();
    }

    public void RemoveConnection(BlockFacing connection, bool updateMesh = true)
    {
        connections.Remove(connection);
        Blockentity.MarkDirty();
        
        if (updateMesh) 
            UpdateConnections();
    }

    public void AddConnection(BlockFacing connection)
    {
        if (connections.Contains(connection)) return;
        
        connections.Add(connection);
        Blockentity.MarkDirty();
    }

    public void CreateJoinAndDiscoverNetwork()
    {
        // This shouldn't be possible but you never know xD
        if (Block is not IPipelineBlock block) return;

        connections.Clear();

        // Check all sides this block can be connected with.
        var neighbourNetworks = new Dictionary<PipeNetwork, IPipelineNode>();
        
        foreach (var face in block.GetConnectableFacings())
        {
            // Check if that side of the block exists, is a pipeline block and can be connected.
            var entity = Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(face));

            if (entity?.Block is not IPipelineBlock block2) continue;
            
            if (block2.IsConnectable(face.Opposite))
            {
                // Yey! This side connects :)
                connections.Add(face);

                var node = entity.GetBehavior<BEBehaviorPipeBase>();
                if (node == null) continue;
                
                // Also mark the connected block as 'facing' this one
                node.AddConnection(face.Opposite);

                if (node.Network != null)
                    neighbourNetworks.TryAdd(node.Network, node);

            }
        }
        
        UpdateConnections();
        // The rest is for the server to figure out.
        if (Api.Side == EnumAppSide.Client || manager == null) return;

        if (neighbourNetworks.Count == 0)
        {
            // No neighbours, or none of them with a network. Create a new network.
            var newNetwork = manager.CreateNetwork();
            manager.BuildNetwork(newNetwork, this);
        }
        else if (neighbourNetworks.Count == 1)
        {
            // Only a single neighbour. Join that one and call it quits
            var net = neighbourNetworks.First().Key;
            JoinNetwork(net);
            
            if (this is IPipelineDestination destination && !net.destinations.Contains(destination))
                net.destinations.Add(destination);
                
            net.CalculateDistances();
        }
        else
        {
            // Multiple neighbour networks. Dissolve them all aside from one, then rebuild that one.
            var keepNetwork = neighbourNetworks.First().Key;
            foreach (var otherNetwork in neighbourNetworks.Keys)
            {
                // Skip the last one
                if (otherNetwork == keepNetwork) continue;
                
                manager.DissolveNetwork(otherNetwork);
            }

            manager.RebuildNetwork(keepNetwork, this);
        }
    }

    /**
     * Quick way to update the BE.
     */
    public void MarkDirty()
    {
        Blockentity.MarkDirty();
    }
}