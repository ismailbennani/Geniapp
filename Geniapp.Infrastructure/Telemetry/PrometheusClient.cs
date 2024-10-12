using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Geniapp.Infrastructure.Telemetry;

public class PrometheusClient(Uri prometheusServerUri, ILogger<PrometheusClient> logger)
{
    public async Task<JsonDocument> QueryAsync(string promQlQuery)
    {
        using HttpClient httpClient = new();
        httpClient.BaseAddress = prometheusServerUri;

        using HttpResponseMessage response = await httpClient.GetAsync($"/api/v1/query?query={HttpUtility.UrlEncode(promQlQuery)}");

        response.EnsureSuccessStatusCode();

        byte[] responseStr = await response.Content.ReadAsByteArrayAsync();
        JsonDocument responseJson = await JsonDocument.ParseAsync(new MemoryStream(responseStr));

        if (responseJson.RootElement.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String)
        {
            string? errorType = responseJson.RootElement.TryGetProperty("errorType", out JsonElement errorTypeElt) && errorTypeElt.ValueKind == JsonValueKind.String
                ? errorTypeElt.GetString()
                : null;

            if (errorType != null)
            {
                logger.LogError("Prometheus API [ERROR]: {Type} - {Message}", errorType, error.GetString());
            }
            else
            {
                logger.LogError("Prometheus API [ERROR]: {Message}", error.GetString());
            }
        }

        if (responseJson.RootElement.TryGetProperty("warnings", out JsonElement warnings) && warnings.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement warning in warnings.EnumerateArray())
            {
                logger.LogInformation("Prometheus API [WARN]: {Message}", warning.GetString());
            }
        }

        if (responseJson.RootElement.TryGetProperty("infos", out JsonElement infos) && infos.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement info in infos.EnumerateArray())
            {
                logger.LogInformation("Prometheus API [INFO]: {Message}", info.GetString());
            }
        }

        if (responseJson.RootElement.GetProperty("status").GetString() != "success")
        {
            throw new Exception($"Prometheus query failed.{Environment.NewLine}Response:{Environment.NewLine}{responseJson.RootElement.GetRawText()}");
        }

        return responseJson;
    }
}
