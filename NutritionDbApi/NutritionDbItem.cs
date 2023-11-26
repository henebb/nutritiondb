using Azure;
using Azure.Data.Tables;

namespace NutritionDbApi;

/// <summary>
/// A representation of the item as stored in the DB.
/// </summary>
public class NutritionDbItem : ITableEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double Weight { get; set; } = 0.0;
    public double Kcal { get; set; } = 0.0;
    public double Proteins { get; set; } = 0.0;
    public double Fat { get; set; } = 0.0;
    public double Carbs { get; set; } = 0.0;
    
    public string PartitionKey { get; set; }

    public string RowKey
    {
        get => Id;
        set => Id = value;
    }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    private const string PK = "NutritionDb";
    public NutritionDbItem(string id)
    {
        Id = id;
        PartitionKey = PK;
    }

    public NutritionDbItem()
    {
        RowKey = Guid.NewGuid().ToString("D");
        PartitionKey = PK;
    }
}
