#define TABLE_STORAGE

#if TABLE_STORAGE
using Azure.Data.Tables;
#elif COSMOS_DB
using System.Net;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
#endif

namespace NutritionDbApi;

public class NutritionDataService : IServeNutritionData
{
#if TABLE_STORAGE
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("StorageAccount:ConnectionString")!;
    private static readonly string DatabaseId = Environment.GetEnvironmentVariable("CosmosDb:DatabaseId")!;
    private static readonly string ContainerId = Environment.GetEnvironmentVariable("CosmosDb:ContainerId")!;

    public const string TableName = "nutritiondb";
    public const string PartitionKey = "NutritionDb";
#elif COSMOS_DB
    private CosmosClient? _client;

    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("CosmosDb:ConnectionString")!;
    private static readonly string DatabaseId = Environment.GetEnvironmentVariable("CosmosDb:DatabaseId")!;
    private static readonly string ContainerId = Environment.GetEnvironmentVariable("CosmosDb:ContainerId")!;
#endif
    
#if INIT_TABLE_STORAGE_DB_WITH_DATA
        var rawJsonData = await File.ReadAllTextAsync("data.json");
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var jsonData = JsonSerializer.Deserialize<List<NutritionDbItem>>(rawJsonData, serializerOptions);
        if (jsonData == null) return new List<NutritionDto>();
        
        foreach (var item in jsonData)
        {
            await table.AddEntityAsync(item);
        }

#endif
    
    public async Task<List<NutritionDto>> GetAllAsync()
    {
#if TABLE_STORAGE
        var table = GetStorageTableClient();

        var allItems = new List<NutritionDto>();
        var asyncQuery = table.QueryAsync<NutritionDbItem>(item => item.PartitionKey == PartitionKey);

        await foreach (var page in asyncQuery.AsPages())
        {
            var pageItems = page.Values
                .Select(item => new NutritionDto
                {
                    Short = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Weight = item.Weight,
                    Kcal = item.Kcal,
                    Proteins = item.Proteins,
                    Fat = item.Fat,
                    Carbs = item.Carbs
                });
            
            allItems.AddRange(pageItems);
        }

        return allItems;
#elif COSMOS_DB
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
#endif
    }

    public async Task<bool> IdExists(string id)
    {
#if TABLE_STORAGE
        var rowKey = id.ToLowerInvariant();
        var table = GetStorageTableClient();
        var item = await table.GetEntityIfExistsAsync<NutritionDbItem>(PartitionKey, rowKey);
        return item.HasValue;
#elif COSMOS_DB
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
#endif
    }

    public async Task<NutritionDto> UpsertItemAsync(NutritionDto nutritionDto)
    {
        var id = nutritionDto.Short.ToLowerInvariant();
        var nutritionToUpsert = new NutritionDbItem(id)
        {
            Name = nutritionDto.Name.ToLowerInvariant(),
            Description = nutritionDto.Description,
            Weight = nutritionDto.Weight,
            Kcal = nutritionDto.Kcal,
            Proteins = nutritionDto.Proteins,
            Fat = nutritionDto.Fat,
            Carbs = nutritionDto.Carbs
        };
#if TABLE_STORAGE
        var table = GetStorageTableClient();

        var response = await table.UpsertEntityAsync(nutritionToUpsert);

        if (response.IsError)
        {
            if (response.ContentStream != null)
            {
                using var sr = new StreamReader(response.ContentStream);
                var content = await sr.ReadToEndAsync();
                throw new Exception($"Error. Content: {content}");
            }

            throw new Exception(response.ReasonPhrase);
        }
        
        // Read item to get latest values.
        var updatedItemResponse = await table.GetEntityIfExistsAsync<NutritionDbItem>(PartitionKey, id);
        if (updatedItemResponse.HasValue)
        {
            var updatedItem = updatedItemResponse.Value!;
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

        throw new Exception("Item could not be read after update"); 
        
#elif COSMOS_DB
        var client = GetCosmosClient();
        var container = client.GetContainer(DatabaseId, ContainerId);
        var upsertResult = await container.UpsertItemAsync(nutritionToUpsert, new PartitionKey(nutritionToUpsert.Id));
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
#endif
    }

#if TABLE_STORAGE
    private TableClient GetStorageTableClient()
    {
        var serviceClient = new TableServiceClient(ConnectionString);
        var table = serviceClient.GetTableClient(TableName);
        table.CreateIfNotExists();
        return table;
    }    
#elif COSMOS_DB
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
#endif
    public void Dispose()
    {
#if COSMOS_DB
        _client?.Dispose();
        _client = null;
#endif
    }
}

public interface IServeNutritionData : IDisposable
{
    Task<List<NutritionDto>> GetAllAsync();
    Task<bool> IdExists(string id);
    Task<NutritionDto> UpsertItemAsync(NutritionDto nutritionDto);
}
