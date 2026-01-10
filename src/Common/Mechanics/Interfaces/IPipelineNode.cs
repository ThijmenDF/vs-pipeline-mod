using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineNode
{
    PipeNetwork? Network { get; set; }
    
    public int nodeId { get; }
    
    /**
     * Called when the node leaves the network (is disconnected / removed)
     */
    void LeaveNetwork();

    /**
     * Called when a node enters the network.
     */
    void JoinNetwork(PipeNetwork newNetwork);
    
    /**
     * The location of the node.
     */
    BlockPos GetPosition();

    /**
     * Which sides of the node is connected to this node.
     */
    List<BlockFacing> GetConnections();

    /**
     * Gets the neighbour node at the given face, or null.
     */
    IPipelineNode? GetNeighbour(IWorldAccessor world, BlockFacing face);

    /**
     * Removes a specified connection.
     */
    void RemoveConnection(BlockFacing connection, bool updateMesh = true);
    
    /**
     * Adds a new connection to the list.
     */
    void AddConnection(BlockFacing connection);
    
    /**
     * Destinations that connect to this node.
     */
    Dictionary<IPipelineDestination, int> Destinations { get; }

    /**
     * Quick access to the MarkDirty() method of blockEntities.
     */
    void MarkDirty();

}