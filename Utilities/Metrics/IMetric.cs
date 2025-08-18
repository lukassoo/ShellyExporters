namespace Utilities.Metrics;

public interface IMetric
{
    public bool IsPublished { get; }
    
    public void Update();
    public void Publish();
    public void Unpublish();
}