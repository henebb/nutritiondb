namespace NutritionDbApi;

/// <summary>
/// The structure to return to the clients.
/// </summary>
public class NutritionDto
{
    public string Short { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double Weight { get; set; } = 0.0;
    public double Kcal { get; set; } = 0.0;
    public double Proteins { get; set; } = 0.0;
    public double Fat { get; set; } = 0.0;
    public double Carbs { get; set; } = 0.0;
}
