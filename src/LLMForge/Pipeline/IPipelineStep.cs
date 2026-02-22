namespace LLMForge.Pipeline;

/// <summary>
/// Defines a single step in the LLMForge pipeline.
/// </summary>
public interface IPipelineStep
{
    /// <summary>
    /// Gets the name of this pipeline step.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes this step using the pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context carrying state between steps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default);
}
