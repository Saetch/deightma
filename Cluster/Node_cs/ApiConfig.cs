using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Node_cs
{
    public class ApiConfig
    {

        public Dictionary<Tuple<int, int>, double> savedValues = new Dictionary<Tuple<int, int>, double>();
        public Dictionary<Tuple<int, int>, String> exteriorValuesInNodes = new Dictionary<Tuple<int, int>, String>();
        public String BICUBIC_INTERPOLATION_SERVICE_URL = "http://bicubic_interpolation_service:8080/calculate";
        public String hostname = Environment.GetEnvironmentVariable("HOSTNAME");
        public String COORDINATOR_SERVICE_URL = "coordinator";
        public int PORT = 8080;
        public int initializeConfigValues()
        {
            Console.WriteLine("hostname is: "+ this.hostname);
            String env = Environment.GetEnvironmentVariable("CLUSTER_ENVIRONMENT");
            if (env == "local"){
                Console.WriteLine("CLUSTER_ENVIRONMENT is: "+ env);
                    this.COORDINATOR_SERVICE_URL = "localhost";
                    this.PORT = 5003;
                    this.BICUBIC_INTERPOLATION_SERVICE_URL = "http://localhost:5555/calculate";
                    this.hostname = "node1";
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
            return 0;  //TODO! Update this if the Node is configured correctly according to the response from the coordinator service
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
        }

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
            //This is an example response: {"HANDLE":{"positions":[{"x":0,"y":0,"value":0.6816054984788531},{"x":1,"y":0,"value":0.6952797360508614},{"x":0,"y":1,"value":3.0950656335878035},{"x":1,"y":1,"value":2.0697533239357435}]}}
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
                savedValues.Add(new Tuple<int, int>(x,y), value);
            }
        

        }
        
    }

    public class XYValues
{
    public double x { get; set; }
    public double y { get; set; }

    public double value { get; set; }
}

}