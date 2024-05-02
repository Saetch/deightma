using System;
using System.Net.Http;
using System.Threading.Tasks;

public class DistributedSystemWebClient
{
    private HttpClient _httpClient;
    private string _baseUrl;

    public DistributedSystemWebClient(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    public async Task<string> GetValue(string x, string y)
    {
        try
        {
            string url = $"{_baseUrl}/getValue/{x}_{y}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception($"Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error receiving the value: {ex.Message}");
        }
    }
}
