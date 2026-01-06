using System.Collections.Generic;

namespace PipelineMod.Common.Mechanics;

public class PipeData
{
    public Dictionary<long, PipeNetwork> networksById = new();
    public long nextNetworkId = 1;
    public long tickNumber;
}