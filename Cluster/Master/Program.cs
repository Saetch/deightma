using System.Net;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Text.Json;
using System.Text;
using Swashbuckle.AspNetCore.SwaggerGen;
using Master;
using Microsoft.Extensions.ObjectPool;
public class Program{
    static int Main(String[] args){
    
        
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });
       // Add CORS services to the container
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAnyOrigin",
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });
        //configure the builder to accept external connections to the server ("0.0.0.0")
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 8080));
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
        var app = builder.Build();
        app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        app.MapGet("/", () => Results.BadRequest("No value provided. Please provide a value in the format 'x_y'"));
        app.MapGet("/getValue/{values}",  async (string values) =>
        {   
            try {

                var result = await GetValue(values);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result);
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapGet("/getValue/{x}/{y}",  async (double x, double y) =>
        {   
            try {

                var result = await GetValue(""+x+"_"+y);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result);
            
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
        app.MapGet("/getAllNodes",  async () =>
        {   
            try {
                var result = await GetAllNodes(new HttpClient());
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result);
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapGet("/getAllSavedValues",  async () =>
        {   
            Console.WriteLine("getAllSavedValues called!");
            try {
                HttpClient httpClient = new HttpClient();
                var result = await GetAllNodes(httpClient);
                List<XYValues> allValues = new List<XYValues>();
                List<Task<List<XYValues>>> tasks = new List<Task<List<XYValues>>>();
                foreach(NodeResponse node in result){
                    tasks.Add(GetAllValuesFromNode(node, httpClient));
                }
                await Task.WhenAll(tasks);
                foreach(Task<List<XYValues>> task in tasks){
                    allValues.AddRange(task.Result);
                }
                Console.WriteLine("Returning result with length: " + allValues.Count);

                return Results.Ok(allValues);
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapGet("/getValueAutoInc/{x}/{y}/{minNodesToEachSide}",  async (double x, double y, int minNodesToEachSide) =>
        {   
            try {
                
                var result = await GetValueAutoInc(x, y, minNodesToEachSide);
                Console.WriteLine("Returning result: " + result);

                return Results.Ok(result);
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapGet("/countValues",  async () =>
        {   
            try {
                var result = await GetAllNodes(new HttpClient());
                List<XYValues> allValues = new List<XYValues>();
                List<Task<List<XYValues>>> tasks = new List<Task<List<XYValues>>>();
                foreach(NodeResponse node in result){
                    tasks.Add(GetAllValuesFromNode(node, new HttpClient()));
                }
                await Task.WhenAll(tasks);
                foreach(Task<List<XYValues>> task in tasks){
                    allValues.AddRange(task.Result);
                }
                Console.WriteLine("Returning result for count: " + allValues.Count);

                return Results.Ok(allValues.Count);
            
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.MapGet("/countSavedValues",  async () =>
        {   
            try {
                var result = await GetAllNodes(new HttpClient());
                List<XYValues> allValues = new List<XYValues>();
                List<Task<List<XYValues>>> tasks = new List<Task<List<XYValues>>>();
                foreach(NodeResponse node in result){
                    tasks.Add(GetAllValuesFromNode(node, new HttpClient()));
                }
                await Task.WhenAll(tasks);
                foreach(Task<List<XYValues>> task in tasks){
                    allValues.AddRange(task.Result);
                }
                Console.WriteLine("Returning result for count: " + allValues.Count);

                return Results.Ok(allValues.Count);
            
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

    static async Task<AutoIncResponse> GetValueAutoInc(double x, double y, int minNodesToEachSide){
        using HttpClient httpClient = new HttpClient();
        List<Tuple<int, int>> positions = new List<Tuple<int, int>>();
        //Todo! Add more sophisticated method of generating relevant data points for the current position
        for(int i = (int)Math.Floor(x) - minNodesToEachSide; i <= x + minNodesToEachSide; i++){
            for(int j = (int)Math.Floor(y) -minNodesToEachSide; j <= y + minNodesToEachSide; j++){
                positions.Add(new Tuple<int, int>(i, j));
            }
        }
        Task<List<NodeResponse>> task2 = GetAllNodes(httpClient);
        Task<List<Position>> task1 = GetPositions(positions, httpClient);
        await Task.WhenAll(task1, task2);
        List<NodeResponse> nodeNames = task2.Result;
        List<Position> positionsWithHashes = task1.Result; 

        List<WanderingPosition> wanderingPositions = await QueryNodesForPositions(nodeNames, positionsWithHashes, httpClient);
        //run UpdateDistributedMapData(wanderingPositions, httpClient) in the background on a separate thread
        var t = UpdateDistributedMapData(wanderingPositions, httpClient);


        double x_double = x;
        double y_double = y;
        int x_int = (int)Math.Round(x_double);
        int y_int = (int)Math.Round(y_double);

        String node_name = find_correct_node(x_int, y_int);
        await t;
        Console.WriteLine("Response code from distributing map data: " + t.Result);
        String result_value = await get_value_from_node(node_name, x, y , httpClient);
        XYValues? result = JsonSerializer.Deserialize<XYValues>(result_value, AppJsonSerializerContext.Default.XYValues);
        List<RawPos> addedCorners = new List<RawPos>();
        foreach(WanderingPosition pos in wanderingPositions){
            addedCorners.Add(new RawPos{
                x = pos.x,
                y = pos.y
            });
        }
        AutoIncResponse response = new AutoIncResponse{
            value = result!,
            addedCorners = addedCorners
        };
        return response;
    }

    static async Task<List<XYValues>> GetAllValuesFromNode(NodeResponse node, HttpClient httpClient){
        var response = await httpClient.GetAsync("http://"+node.name+":5552/getAllSavedValues");
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
        List<XYValues>? values = JsonSerializer.Deserialize<List<XYValues>>(responseBody, options);
        return values!;
    }
    static async Task<List<WanderingPosition>> QueryNodesForPositions(List<NodeResponse> nodeNames, List<Position> positions, HttpClient httpClient){
        Console.WriteLine("Querying nodes for positions called!");
        List<WanderingPosition> wanderingPositions = new List<WanderingPosition>();
        Dictionary<String, List<Position>> nodeToPositions = new Dictionary<String, List<Position>>();
        foreach(Position position in positions){
            int index = Helper.BinarySearch(nodeNames, position.hash);
            if(nodeToPositions.ContainsKey(nodeNames[index].name)){
                nodeToPositions[nodeNames[index].name].Add(position);
            }else{
            nodeToPositions.Add(nodeNames[index].name, new List<Position>());
            nodeToPositions[nodeNames[index].name].Add(position);
            }
        }
        List<Task<List<WanderingPosition>>> tasks = new List<Task<List<WanderingPosition>>>();
        foreach((String nodeName, List<Position> toFindValues) in nodeToPositions){
            tasks.Add(FindAbandonedValuesFromNode(nodeName, toFindValues, httpClient));
        }
        await Task.WhenAll(tasks);
        for( int i = 0; i < tasks.Count; i++){
            wanderingPositions.AddRange(tasks[i].Result);
        }
        Console.WriteLine("Querying nodes for positions done!");
        return wanderingPositions;
    }
    static async Task<int> UpdateDistributedMapData(List<WanderingPosition> list ,HttpClient httpClient){
        List<WanderingPosition> wanderingPositions = list;
        Dictionary<String, List<WanderingPosition>> nodeToPositions = new Dictionary<String, List<WanderingPosition>>();
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
        String url = "http://coordinator:8080/organize/update_distributed_map_data";
        var body = new StringContent(JsonSerializer.Serialize(list, options), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, body);
        response.EnsureSuccessStatusCode();
        Console.WriteLine("Updated distributed map data: "+ await response.Content.ReadAsStringAsync());
        return ((int)response.StatusCode);
    }


    static async Task<List<WanderingPosition>> FindAbandonedValuesFromNode(String nodeName, List<Position> positions, HttpClient httpClient){
        String url = "http://"+nodeName+":5552/hasValues?vec=";
        foreach(Position position in positions){
            url += (int)Math.Floor(position.x) + "," + (int)Math.Floor(position.y) + ";";
        }
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
        List<WanderingPosition> ret = [];
        List<XYValues>? results = JsonSerializer.Deserialize<List<XYValues>>(responseBody, options);
        foreach(Position pos in positions){
            if(!results!.Any(x => x.x == pos.x && x.y == pos.y)){
                ret.Add(new WanderingPosition{
                    x = (int)Math.Floor(pos.x),
                    y = (int)Math.Floor(pos.y),
                    hashValue = (ushort)pos.hash
                });
            }
        }
        return ret!;
    }

    static async Task<List<Position>> GetPositions(List<Tuple<int, int>> positions, HttpClient httpClient){
        String url = "http://hasher-service:8080/hash_multiple?vec=";
        foreach(Tuple<int, int> position in positions){
            url += "x:"+position.Item1 + "," + "y:"+position.Item2 + ";";
        }
        url = url.TrimEnd(';');
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
        List<Position>? results = JsonSerializer.Deserialize<List<Position>>(responseBody, options);
        return results!;
    }
    

    
    static async Task<ushort> GetHashedValue(double x, double y, HttpClient httpClient){
        var response = await httpClient.GetAsync("http://coordinator:8080/organize/get_hashed_value/"+x+"/"+y);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        return ushort.Parse(responseBody);
    }
    
    static async Task<List<NodeResponse>> GetAllNodes(HttpClient httpClient){
        
        var response = await httpClient.GetAsync("http://coordinator:8080/organize/get_all_nodes");
        response.EnsureSuccessStatusCode();
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };
        var responseBody = await response.Content.ReadAsStringAsync();
        if (responseBody.Equals("Unknown") || responseBody == null)
        {
            throw new Exception("Nodes not found. Application is not properly set up, configured or running.");
        }
        List<NodeResponse>? nodes = JsonSerializer.Deserialize<List<NodeResponse>>(responseBody, options);

        return nodes!;
    }



    static async Task<XYValues> GetValue(String input){
        var inputs = input.Split('_');
        if (inputs.Length != 2)
            throw new ArgumentException("Input must be in the format 'x_y'");
        
            
        double x_double = double.Parse(inputs[0].Replace(',', '.'), CultureInfo.InvariantCulture);
        double y_double = double.Parse(inputs[1].Replace(',', '.'), CultureInfo.InvariantCulture);
        int x_int = (int)Math.Round(x_double);
        int y_int = (int)Math.Round(y_double);

        String node_name = find_correct_node(x_int, y_int);
        String result_value = await get_value_from_node(node_name, input);
        XYValues? result = JsonSerializer.Deserialize<XYValues>(result_value, AppJsonSerializerContext.Default.XYValues);
        return result!;
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

        static async Task<String> get_value_from_node(String name, double x, double y, HttpClient httpClient){
            string apiUrl = $"http://"+name+":5552/getValue/"+x+"_"+y;
            Console.WriteLine(apiUrl);            
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





class XYValues
{
    public double x { get; set; }
    public double y { get; set; }

    public double value { get; set; }
}

public class NodeResponse{
    public required String name { get; set; }
    public ushort hash { get; set; }

}

class WanderingPosition{
    public int x { get; set; }
    public int y { get; set; }
    public ushort hashValue { get; set; }
}

class Position{
    public double x { get; set; }
    public double y { get; set; }
    public int hash { get; set; }
}

class RawPos{
    public int x { get; set; }
    public int y { get; set; }
}

class AutoIncResponse{
    public XYValues value { get; set; }
    public List<RawPos> addedCorners { get; set; }

}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(XYValues[]))]
[JsonSerializable(typeof(XYValues))]
[JsonSerializable(typeof(List<XYValues>))]
[JsonSerializable(typeof(String))]
[JsonSerializable(typeof(List<String>))]
[JsonSerializable(typeof(NodeResponse))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(List<NodeResponse>))]
[JsonSerializable(typeof(Position))]
[JsonSerializable(typeof(List<Position>))]
[JsonSerializable(typeof(WanderingPosition))]
[JsonSerializable(typeof(List<WanderingPosition>))]
[JsonSerializable(typeof(RawPos))]
[JsonSerializable(typeof(List<RawPos>))]
[JsonSerializable(typeof(AutoIncResponse))]

internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

