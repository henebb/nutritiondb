using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace NutritionDbApi;

public static class NutritionFunctions
{
    [Function("Get all nutritions")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "nutritions")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        var connectionString = Environment.GetEnvironmentVariable("CosmosDb:ConnectionString")!;
        var databaseId = Environment.GetEnvironmentVariable("CosmosDb:DatabaseId")!;
        var containerId = Environment.GetEnvironmentVariable("CosmosDb:ContainerId")!;

        using var client = new CosmosClient(
            connectionString,
            // Use camel-casing
            new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            }
        );

        var itemsToReturn = new List<NutritionReturnItem>();

        var container = client.GetContainer(databaseId, containerId);
        var queryble = container.GetItemLinqQueryable<NutritionItem>();
        using var feedIterator = queryble.ToFeedIterator();
        while (feedIterator.HasMoreResults) 
        {
            var feedResponse = await feedIterator.ReadNextAsync();

            itemsToReturn.AddRange(
                feedResponse.Select(item => new NutritionReturnItem 
                {
                    Short = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Weight = item.Weight,
                    Kcal = item.Kcal,
                    Proteins = item.Proteins,
                    Fat = item.Fat,
                    Carbs = item.Carbs
                })
            );
        }

        await response.WriteAsJsonAsync<List<NutritionReturnItem>>(itemsToReturn);

        return response;
    }
}

internal class NutritionItem
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

internal class NutritionReturnItem
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