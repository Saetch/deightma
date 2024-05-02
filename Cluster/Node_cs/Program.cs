using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
public class Program{

    //values relevant for operating
    public static double[][] values;
    public static int offsetX = 0;
    public static int offsetY = 0;
    public static int width = 2;
    public static int height = 2;

    public static int Main(String[] args){

        //get environment variables to determine the data fields
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

        initializeValues();

        //just information on launch
        Console.WriteLine("Starting server on port 5552 with width: " + width + " height: " + height + " offsetX: " + offsetX + " offsetY: " + offsetY);
        Console.WriteLine("Values: ");	
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Console.Write(values[i][j] + " ");
            }
            Console.WriteLine();
        }


        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        //configure the builder to accept external connections to the server ("0.0.0.0")
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 5552));


        var app = builder.Build();


        app.MapGet("/getValue/{values}", (string values) =>
        {   
            try {
                var result = getValue(values);
                return Results.Ok(result);
            } catch (Exception e) {
                return Results.BadRequest(e.Message);
            }
            
        });
        app.Run();

        return 0;
    }

    //dummy implementation to make sure the network works! TODO: implement actual initialization!
    static void initializeValues()
    {
        Random random = new Random();
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i][j] = i + offsetX + j + offsetY;
            }
        }
    }

    static double calculateInterpolatedValue(double x, double y)
    {
        double actual_x = x - offsetX;
        double actual_y = y - offsetY;

        //dummy implementation to make sure the network works! TODO: implement actual interpolation
        actual_x = Math.Round(actual_x);
        actual_y = Math.Round(actual_y);
        actual_x = Math.Max(0, Math.Min(width - 1, actual_x));
        actual_y = Math.Max(0, Math.Min(height - 1, actual_y));
        return values[(int)actual_x][(int)actual_y];
    }

    static XYValues getValue(string values)
    {
        Console.WriteLine("Received GetValie-call with params: " + values);
        string[] splitValues = values.Split("_");
        double x = Double.Parse(splitValues[0].Replace(',', '.'), CultureInfo.InvariantCulture);
        double y = Double.Parse(splitValues[1].Replace(',', '.'), CultureInfo.InvariantCulture);
        if(splitValues.Length > 2){
            throw new Exception("Too many values");
        }        
        double ret_value = calculateInterpolatedValue(x, y);
        return new XYValues { X = x, Y = y, Value = ret_value };
    }




}





class XYValues
{
    public double X { get; set; }
    public double Y { get; set; }

    public double Value { get; set; }
}




[JsonSerializable(typeof(XYValues[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
