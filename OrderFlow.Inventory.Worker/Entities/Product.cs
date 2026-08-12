namespace OrderFlow.Inventory.Worker.Entities;

public class Product
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int AvailableStock { get; set; }
}