
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;


namespace Node_cs
{
    public class ApiConfig
    {

        public ConcurrentDictionary<Tuple<int, int>, double> savedValues = new ConcurrentDictionary<Tuple<int, int>, double>();
        public String BICUBIC_INTERPOLATION_SERVICE_URL = "http://bicubic:8080/calculate";
        public String hostname = Environment.GetEnvironmentVariable("HOSTNAME");
        public String COORDINATOR_SERVICE_URL = "coordinator";
        public int PORT = 8080;

        public String PODENV = Environment.GetEnvironmentVariable("PODENV");
        public ushort ownerHash = 0;
        public int initializeConfigValues()
        {
            Console.WriteLine("hostname is: "+ this.hostname);
            String env = Environment.GetEnvironmentVariable("CLUSTER_ENVIRONMENT");
            Console.WriteLine("CLUSTER_ENVIRONMENT is: "+ env);
            if (env == "local"){
                Console.WriteLine("CLUSTER_ENVIRONMENT is: "+ env);
                    this.COORDINATOR_SERVICE_URL = "localhost";
                    this.PORT = 5003;
                    this.BICUBIC_INTERPOLATION_SERVICE_URL = "http://localhost:5555/calculate";
                    this.hostname = "node1";
            }
            if (env == "pod"){
                try
                {
                    // Start a new process to run the "hostname -i" command
                    ProcessStartInfo processStartInfo = new ProcessStartInfo()
                    {
                        FileName = "/bin/sh",
                        Arguments = "-c \"hostname -i\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = processStartInfo;
                        process.Start();

                        // Read the output (which should be the pod IP address)
                        string podIP = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();

                        // Print or use the pod IP as needed
                        Console.WriteLine("Pod IP: " + podIP);
                        this.hostname = podIP;
                    }
                    System.Threading.Thread.Sleep(5000);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error trying to execute hostname command: " + ex.Message);
                }
            }
            TcpClient tcpClient = new TcpClient();
            var result = tcpClient.BeginConnect(this.COORDINATOR_SERVICE_URL, this.PORT, null, null);

            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(150));
            if (success)
            {
                tcpClient.EndConnect(result);
            }
            else{
                Console.WriteLine("Failed to connect to coordinator service ... ");
                return 1;
            }
            tcpClient.Close();


            HttpClient client = new HttpClient();
            var response = client.PostAsync("http://" + this.COORDINATOR_SERVICE_URL + ":"+ this.PORT+"/organize/register/"+this.hostname, null).Result;
            DealWithResponse(response);
            return 0;  
        }

        public WebApplication setupServer(){

            var builder = WebApplication.CreateSlimBuilder();
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });

            //configure the builder to accept external connections to the server ("0.0.0.0")
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 5552));
            var app = builder.Build();
            registerRequests(app);
            return app;
        }

        private void registerRequests(WebApplication app)
        {
            app.MapGet("/getValue/{values}", (string values) =>
            {
                try {
                    var result = GetRequests.GetValue(values, this);
                    return Results.Ok(result);
                } catch (Exception e) {
                    return Results.BadRequest(e.Message);
                }
            });
            app.MapGet("/getSavedValue/{x}/{y}", (int x, int y) =>
            {
                double? result = GetRequests.GetSavedNodeValue(x, y, this);
                Console.WriteLine("Received getSavedValue-call with params: " + x + " " + y + " returning: " + result);
                if (result == null){
                    return Results.Ok("null");
                }
                return Results.Ok(result.Value);
            });
            app.MapPost("/setSavedValue/{x}/{y}/{value}", (int x, int y, double value) =>
            {
                try {

                    return Results.Ok("TODO! Implement this!");
                } catch (Exception e) {
                    return Results.BadRequest(e.Message);
                }
            });
            app.MapPost("/setCoordinator/{ip}", (string ip) =>
            {
                try {
                    Console.WriteLine("Received setCoordinator-call with params: " + ip );
                    this.COORDINATOR_SERVICE_URL = ip;
                    return Results.Ok("Coordinator set to: " + ip);
                } catch (Exception e) {
                    return Results.BadRequest(e.Message);
                }
            });
            app.MapGet("/setCoordinator/{ip}", (string ip) => //TODO! Remove this after testing
            {
                try {
                    Console.WriteLine("Received setCoordinator-call with params: " + ip );
                    this.COORDINATOR_SERVICE_URL = ip;
                    return Results.Ok("Coordinator set to: " + ip);
                } catch (Exception e) {
                    return Results.BadRequest(e.Message);
                }
            });
            app.MapGet("/getAllSavedValues", () =>
            {
                //go through all keys in savedvalues
                //return a list of all keys and values
                List<XYValues> values = new List<XYValues>();
                foreach (KeyValuePair<Tuple<int, int>, double> entry in savedValues)
                {
                    values.Add(new XYValues { x = entry.Key.Item1, y = entry.Key.Item2, value = entry.Value });
                }
                return Results.Ok(values);
            });
            app.MapPost("/deleteSavedValuesBelow/{hash}", async (String hash) =>
            {
                try {
                    Console.WriteLine("Received deleteSavedValuesBelow-call with params: " + hash );
                    var retVal = await NodeBehavior.DeleteSavedValuesBelow(hash, this);
                    return Results.Ok(retVal);
                } catch (Exception e) {
                    return Results.BadRequest(e.Message);
                }
            });
            app.MapGet("/getMultipleSavedValues", async (String parameters) => {
                try {
                    Console.WriteLine("Received getMultipleSavedValues-call with params: " + parameters );
                    var retVal = await NodeBehavior.GetMultipleSavedValues(parameters, this);
                    return Results.Ok(retVal);
                } catch (Exception e) {
                    return Results.BadRequest(e.Message);
                }
            });
            app.MapPost("/addValue/", (Position toAdd) =>
            {
                try {
                    Console.WriteLine("Received addValue-call with params: " + toAdd.x + " " + toAdd.y + " " + toAdd.value);
                    var key = new Tuple<int, int>(toAdd.x, toAdd.y);
                    if (!savedValues.TryAdd(key, toAdd.value)){
                        savedValues[key] = toAdd.value;
                    }

                    return Results.Ok("Value added: " + toAdd.x + " " + toAdd.y + " " + toAdd.value);
                } catch (Exception e) {
                    return Results.BadRequest(e.Message);
                }
            });
            app.MapGet("/hasValues", (String vec) =>
            {
                List<Tuple<int, int>> values = InterpretHasValues(vec);
                Console.WriteLine("Received hasValues-call with params: " + vec);
                List<XYValues> returnValues = new List<XYValues>();
                foreach (Tuple<int, int> value in values){
                    if (savedValues.ContainsKey(value)){
                        returnValues.Add(new XYValues { x = value.Item1, y = value.Item2, value = savedValues[value] });
                    }
                }
                return Results.Ok(returnValues);
            });
            
            
        }


        private static List<Tuple<int, int>> InterpretHasValues(String vec){
            List<Tuple<int, int>> values = new List<Tuple<int, int>>();
            String [] parts = vec.Split(";");
            foreach (String part in parts){
                if (part.Length < 2){
                    continue;
                }
                String [] xy = part.Split(",");
                int x = Int32.Parse(xy[0]);
                int y = Int32.Parse(xy[1]);
                values.Add(new Tuple<int, int>(x, y));
            }
            return values;
        }


        //This expects the response to be in exact format
        private void DealWithResponse(HttpResponseMessage response){
            String responseString = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Received response from coordinator service: " + responseString);
            if (responseString.Contains("WAIT")){
                Console.WriteLine("Received WAIT command ... ");
                return;
            }
            if (!responseString.Contains("HANDLE")){
                Console.WriteLine("Received invalid response from coordinator service: " + responseString);
                throw new Exception("Received invalid response from coordinator service: " + responseString);
            }
            //This is an example response: {"HANDLE":{"hash_value":8782,"positions":[{"x":0,"y":0,"value":0.6816054984788531},{"x":1,"y":0,"value":0.6952797360508614},{"x":0,"y":1,"value":3.0950656335878035},{"x":1,"y":1,"value":2.0697533239357435}]}}
            if (!responseString.Contains("x") || !responseString.Contains("y") || !responseString.Contains("value")){
                Console.WriteLine("Received null response from coordinator service ... ");
                return;
            }
            String hashVal = responseString.Split("\"hash_value\":")[1].Split(",")[0];
            UInt16 hash = UInt16.Parse(hashVal);
            this.ownerHash = hash;
            Console.WriteLine("Set owner hashValue to: " + hash);
            String valuesPart = responseString.Split("[{")[1];
            String [] valuesStrings = valuesPart.Split("{");
            foreach (String valueString in valuesStrings){
                if (valueString.Length < 3){
                    continue;
                }
                String [] valueParts = valueString.Split(",");
                int x = Int32.Parse(valueParts[0].Split(":")[1]);
                int y = Int32.Parse(valueParts[1].Split(":")[1]);
                double value = Double.Parse(valueParts[2].Replace("}","").Replace("]","").Split(":")[1]);
                savedValues.TryAdd(new Tuple<int, int>(x, y), value);
            }
        
            Console.WriteLine("Finished computing the response from coordinator service ... ");
        }
        
    }


public class Position{
    public int x { get; set; }
    public int y { get; set; }

    public double value { get; set; }
}

public class XYValues
{
    public double x { get; set; }
    public double y { get; set; }

    public double value { get; set; }
}
public class HashedPosition{
    public int x  { get; set; }
    public int y { get; set; }
    public ushort hash { get; set; }
}

public class Point{
    public int x { get; set; }
    public int y { get; set; }
}

public class NodeResponse{
    public required String name {get; set;}

    public ushort hash {get; set;}
}

[JsonSerializable(typeof(String))]
[JsonSerializable(typeof(List<XYValues>))]
[JsonSerializable(typeof(List<Point>))]
[JsonSerializable(typeof(Point[]))]
[JsonSerializable(typeof(Point))]
[JsonSerializable(typeof(XYValues))]
[JsonSerializable(typeof(XYValues[]))]
[JsonSerializable(typeof(HashedPosition))]
[JsonSerializable(typeof(List<HashedPosition>))]
[JsonSerializable(typeof(Tuple<int, int>))]
[JsonSerializable(typeof(List<Tuple<int, int>>))]
[JsonSerializable(typeof(NodeResponse))]
[JsonSerializable(typeof(List<NodeResponse>))]
[JsonSerializable(typeof(Position))]
[JsonSerializable(typeof(List<Position>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

}