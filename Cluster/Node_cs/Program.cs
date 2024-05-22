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
using Node_cs;
public class Program{



    public static int Main(String[] args){

        ApiConfig config = new ApiConfig();
        //get environment variables to determine the data fields

        Console.WriteLine("Starting server on port 5552... ");
        int returnCode = config.initializeConfigValues();
        while (returnCode != 0)
        {
            Console.WriteLine("Failed to connect to coordinator service, retrying in 2 seconds ... ");
            System.Threading.Thread.Sleep(2000);
            returnCode = config.initializeConfigValues();
            
        }


        var app = config.setupServer();
        app.Run();

        return 0;
    }







}








[JsonSerializable(typeof(List<XYValues>))]
[JsonSerializable(typeof(XYValues))]
[JsonSerializable(typeof(XYValues[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
