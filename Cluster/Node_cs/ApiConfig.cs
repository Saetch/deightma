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

        public const String BICUBIC_INTERPOLATION_SERVICE_URL = "http://bicubic_interpolation_service:8080/calculate";
        public String hostname = Environment.GetEnvironmentVariable("HOSTNAME");
        public String COORDINATOR_SERVICE_URL = "coordinator-1";

        public int initializeConfigValues()
        {
            Console.WriteLine("hostname is: "+ this.hostname);
            
            TcpClient tcpClient = new TcpClient();
            var result = tcpClient.BeginConnect(this.COORDINATOR_SERVICE_URL, 8080, null, null);

            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(15));
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
            var response = client.GetAsync("http://" + this.COORDINATOR_SERVICE_URL + ":8080/register/").Result;
            Console.WriteLine("Received response from coordinator service: " + response.Content.ReadAsStringAsync().Result);
            return 1;  //TODO! Update this if the Node is configured correctly according to the response from the coordinator service
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
        
    }

    public class XYValues
{
    public double x { get; set; }
    public double y { get; set; }

    public double value { get; set; }
}

}