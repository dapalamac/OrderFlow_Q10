namespace OrderFlow.Inventory.Worker.Entities;

public class ProcessedEvent
{
    public Guid EventId { get; set; }

    public DateTime ProcessedAt { get; set; }
}