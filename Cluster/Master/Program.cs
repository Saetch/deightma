using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
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

        var sampleTodos = new Todo[] {
            new(1, "Walk the dog"),
            new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
            new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
            new(4, "Clean the bathroom"),
            new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
        };
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
   
    double x = double.Parse(inputs[0]);
    double y = double.Parse(inputs[1]);
        return new XYValues
        {
            X = double.Parse(inputs[0]),
            Y = double.Parse(inputs[1]),
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


public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);



[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
