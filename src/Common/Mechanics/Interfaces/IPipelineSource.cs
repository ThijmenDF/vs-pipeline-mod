#nullable disable
namespace PipelineMod.Common.Mechanics.Interfaces;

public interface IPipelineSource : IPipelineNode
{
    bool IsSubmerged { get; }
}