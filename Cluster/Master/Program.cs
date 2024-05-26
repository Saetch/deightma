using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Text;
public class Program{
    static int Main(String[] args){
    
        
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        //configure the builder to accept external connections to the server ("0.0.0.0")
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 8080));
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
        var app = builder.Build();
        
        app.MapGet("/", () => Results.BadRequest("No value provided. Please provide a value in the format 'x_y'"));
        app.MapGet("/getValue/{values}",  async (string values) =>
        {   
            try {

                var result = await GetValue(values);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result.Replace("\"", "'"));
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapGet("/getValue/{x}/{y}",  async (double x, double y) =>
        {   
            try {

                var result = await GetValue(""+x+"_"+y);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result.Replace("\"", "'"));
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapPost("/newValue/{x}/{y}/{value}",  async (int x, int y, double value) =>
        {   
            try {
                XYValues addValue = new()
                {
                    x = x,
                    y = y,
                    value = value
                };
                var result = await AddValue(addValue);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result.Replace("\"", "'"));
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapPost("/addValue/{x}/{y}/{value}",  async (int x, int y, double value) =>
        {   
            try {
                XYValues addValue = new()
                {
                    x = x,
                    y = y,
                    value = value
                };
                var result = await AddValue(addValue);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result.Replace("\"", "'"));
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapPost("/saveValue/{x}/{y}/{value}",  async (int x, int y, double value) =>
        {   
            try {
                XYValues addValue = new()
                {
                    x = x,
                    y = y,
                    value = value
                };
                var result = await AddValue(addValue);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result.Replace("\"", "'"));
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.Run();

        return 0;
    }

    static async Task<String> AddValue(XYValues value){
        using HttpClient httpClient = new HttpClient();
        string json = JsonSerializer.Serialize(
        value!, AppJsonSerializerContext.Default.XYValues);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("http://coordinator:8080/organize/add_value", content);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }



    static async Task<String> GetValue(String input){
        var inputs = input.Split('_');
        if (inputs.Length != 2)
            throw new ArgumentException("Input must be in the format 'x_y'");
        
            
        double x_double = double.Parse(inputs[0].Replace(',', '.'), CultureInfo.InvariantCulture);
        double y_double = double.Parse(inputs[1].Replace(',', '.'), CultureInfo.InvariantCulture);
        int x_int = (int)Math.Round(x_double);
        int y_int = (int)Math.Round(y_double);

        String node_name = find_correct_node(x_int, y_int);
        String result_value = await get_value_from_node(node_name, input);
        return result_value;
    }


    static String find_correct_node(int x, int y){
        using (HttpClient httpClient = new HttpClient())
        {
            var response = httpClient.GetAsync("http://coordinator:8080/organize/get_node/"+x+"/"+y).Result;
            response.EnsureSuccessStatusCode();
            var responseBody = response.Content.ReadAsStringAsync().Result;
            if (responseBody.Equals("Unknown")){
                throw new Exception("Node not found");
            }
            return responseBody;
        }
    }

    //dummy implementation of the getValue function. This function is to be replaced by the actual logic, calling the nodes in the network
    static XYValues getDummyValue(String input){
    var inputs = input.Split('_');
    if (inputs.Length != 2)
        throw new ArgumentException("Input must be in the format 'x_y'");

    double x = double.Parse(inputs[0]);
    double y = double.Parse(inputs[1]);
        return new XYValues
        {
            x = double.Parse(inputs[0]),
            y = double.Parse(inputs[1]),
            value = Math.Sqrt(x * x + y * y)
        };


    }




        static async Task<String> get_value_from_node(String name, string input)
    {
        // Construct the URL for the external API endpoint
        string apiUrl = $"http://"+name+":5552/getValue/"+input;
        Console.WriteLine(apiUrl);
        // Create an instance of HttpClient
        using (HttpClient httpClient = new HttpClient())
        {
            // Make a GET request to the external API
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            // Ensure the response is successful
            response.EnsureSuccessStatusCode();

            // Read the response content as a string
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Received response: " + responseBody);

            return responseBody;
        }
    }
}





class XYValues
{
    public double x { get; set; }
    public double y { get; set; }

    public double value { get; set; }
}




[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(XYValues[]))]
[JsonSerializable(typeof(XYValues))]
[JsonSerializable(typeof(String))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

