namespace NutritionDbApi;

/// <summary>
/// A representation of the item as stored in the DB.
/// </summary>
public class NutritionDbItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double Weight { get; set; } = 0.0;
    public double Kcal { get; set; } = 0.0;
    public double Proteins { get; set; } = 0.0;
    public double Fat { get; set; } = 0.0;
    public double Carbs { get; set; } = 0.0;
}
