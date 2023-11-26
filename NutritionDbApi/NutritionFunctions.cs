using System.Net;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace NutritionDbApi;

public class NutritionFunctions
{
    [Function("Get all nutritions")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "nutritions")] HttpRequestData req,
        FunctionContext executionContext
    )
    {
       var response = req.CreateResponse(HttpStatusCode.OK);
        
        using IServeNutritionData serveNutritionData = new NutritionDataService();
        var allNutritions = await serveNutritionData.GetAllAsync();

        var webSerializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await response.WriteAsJsonAsync(allNutritions, webSerializer);

        return response;
    }
    
    [Function("Check if id is available")]
    public async Task<HttpResponseData> IsAvailable(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "nutritions/check/{id}")] HttpRequestData req,
        string id,
        FunctionContext executionContext
    )
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        using IServeNutritionData serveNutritionData = new NutritionDataService();
        var exists = await serveNutritionData.IdExists(id);

        var webSerializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await response.WriteAsJsonAsync(new
        {
            IsAvailable = !exists
        }, webSerializer);

        return response;
    }
    
    [Function("Update/Add nutrition")]
    public async Task<HttpResponseData> Upsert(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "nutritions/upsert")] HttpRequestData req,
        FunctionContext executionContext
    )
    {
        using var streamReader = new StreamReader(req.Body);
        var requestBody = await streamReader.ReadToEndAsync();
        // Use JsonSerializerDefaults.Web to allow camelCase.
        var nutritionDto = JsonSerializer.Deserialize<NutritionDto>(requestBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (nutritionDto == null)
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);

            await badRequestResponse.WriteAsJsonAsync(new
            {
                Message = "Invalid data passed. Unable to deserialize."
            });
            
            return badRequestResponse;
        }
        var response = req.CreateResponse(HttpStatusCode.OK);
        using IServeNutritionData serveNutritionData = new NutritionDataService();
        var updatedItem = await serveNutritionData.UpsertItemAsync(nutritionDto);

        var webSerializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await response.WriteAsJsonAsync(updatedItem, webSerializer);

        return response;
    }
}
