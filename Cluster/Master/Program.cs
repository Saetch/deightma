using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
public class Program{
        static int Main(String[] args){
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        //configure the builder to accept external connections to the server ("0.0.0.0")
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 5552));


        var app = builder.Build();

        app.MapGet("/", () => Results.BadRequest("No value provided. Please provide a value in the format 'x_y'"));
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



//dummy implementation of the getValue function. This function is to be replaced by the actual logic, calling the nodes in the network
    static XYValues getValue(String input)
{
    var inputs = input.Split('_');
    if (inputs.Length != 2)
        throw new ArgumentException("Input must be in the format 'x_y'");
    
        double x = double.Parse(inputs[0].Replace(',', '.'), CultureInfo.InvariantCulture);
        double y = double.Parse(inputs[1].Replace(',', '.'), CultureInfo.InvariantCulture);
        return new XYValues
        {
            X = x,
            Y = y,
            Value = Math.Sqrt(x * x + y * y)
        };


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
