using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Node_cs
{
    public class ApiConfig
    {

        public double[][] values;
        public int offsetX = 0;
        public int offsetY = 0;
        public int width = 2;
        public int height = 2;

        public static String bicubic_interpolation_service_url = "http://bicubic_interpolation_service:8080/calculate";

        public void initializeConfigValues()
        {
            try{
                #pragma warning disable CS8604 // Possible null reference argument.
                width = int.Parse(Environment.GetEnvironmentVariable("WIDTH"));
                height = int.Parse(Environment.GetEnvironmentVariable("HEIGHT"));
                offsetX = int.Parse(Environment.GetEnvironmentVariable("OFFSET_X"));
                offsetY = int.Parse(Environment.GetEnvironmentVariable("OFFSET_Y"));   
                #pragma warning restore CS8604 // Possible null reference argument.
            } catch (Exception e){
                Console.WriteLine("Error while parsing environment variables: " + e.Message);
            }
            //TODO! remove this, this is just for static implementation, e.g. every node knows the width and height of all nodes. How the offsets are prepared will need to be updated
            offsetX = offsetX * width;
            offsetY = offsetY * height;

            values = new double[width][];
            for (int i = 0; i < width; i++)
            {
                values[i] = new double[height];
            }
            Random random = new Random();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    values[i][j] = offsetX + i + offsetY + j + random.NextDouble();
                }
            }
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
                    x = x - this.offsetX;
                    y = y - this.offsetY;
                    values[x][y] = value;
                    return Results.Ok();
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