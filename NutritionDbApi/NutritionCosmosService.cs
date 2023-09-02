using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace NutritionDbApi;

public class NutritionCosmosService : IServeNutritionCosmos
{
    private CosmosClient? _client;
    
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("CosmosDb:ConnectionString")!;
    private static readonly string DatabaseId = Environment.GetEnvironmentVariable("CosmosDb:DatabaseId")!;
    private static readonly string ContainerId = Environment.GetEnvironmentVariable("CosmosDb:ContainerId")!;

    public async Task<List<NutritionDto>> GetAllAsync()
    {
        var itemsToReturn = new List<NutritionDto>();
        var client = GetCosmosClient();
        var container = client.GetContainer(DatabaseId, ContainerId);
        var queryable = container.GetItemLinqQueryable<NutritionDbItem>();
        using var feedIterator = queryable.ToFeedIterator();
        while (feedIterator.HasMoreResults) 
        {
            var feedResponse = await feedIterator.ReadNextAsync();

            itemsToReturn.AddRange(
                feedResponse.Select(item => new NutritionDto 
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

        return itemsToReturn;
    }

    public async Task<bool> IdExists(string id)
    {
        var client = GetCosmosClient();
        var container = client.GetContainer(DatabaseId, ContainerId);
        try
        {
            var idToCheck = id.ToLowerInvariant();
            var response = await container.ReadItemAsync<NutritionDbItem>(idToCheck, new PartitionKey(idToCheck));
            return response.Resource.Id == idToCheck;
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<NutritionDto> UpsertItemAsync(NutritionDto nutritionDto)
    {
        var nutritionToUpdate = new NutritionDbItem
        {
            Id = nutritionDto.Short.ToLowerInvariant(),
            Name = nutritionDto.Name.ToLowerInvariant(),
            Description = nutritionDto.Description,
            Weight = nutritionDto.Weight,
            Kcal = nutritionDto.Kcal,
            Proteins = nutritionDto.Proteins,
            Fat = nutritionDto.Fat,
            Carbs = nutritionDto.Carbs
        };
        var client = GetCosmosClient();
        var container = client.GetContainer(DatabaseId, ContainerId);
        var upsertResult = await container.UpsertItemAsync(nutritionToUpdate, new PartitionKey(nutritionToUpdate.Id));
        var updatedItem = upsertResult.Resource;
        
        return new NutritionDto
        {
            Short = updatedItem.Id,
            Name = updatedItem.Name,
            Description = updatedItem.Description,
            Weight = updatedItem.Weight,
            Kcal = updatedItem.Kcal,
            Proteins = updatedItem.Proteins,
            Fat = updatedItem.Fat,
            Carbs = updatedItem.Carbs
        };
    }

    private CosmosClient GetCosmosClient()
    {
        if (_client != null)
        {
            return _client;
        }
        _client = new CosmosClient(
            ConnectionString,
            // Use camel-casing
            new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            }
        );

        return _client;
    }

    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
    }
}

public interface IServeNutritionCosmos : IDisposable
{
    Task<List<NutritionDto>> GetAllAsync();
    Task<bool> IdExists(string id);
    Task<NutritionDto> UpsertItemAsync(NutritionDto nutritionDto);
}
