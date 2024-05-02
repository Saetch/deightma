using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Node_cs
{
    public class ApiConfig
    {

        public double[][] values;
        public int offsetX = 0;
        public int offsetY = 0;
        public int width = 2;
        public int height = 2;



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
        }
        
    }

    public class XYValues
{
    public double x { get; set; }
    public double y { get; set; }

    public double value { get; set; }
}
}