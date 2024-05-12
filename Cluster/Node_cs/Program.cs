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


        config.initializeConfigValues();
        {
            //just information on launch
            Console.WriteLine("Starting server on port 5552 with width: " + config.width + " height: " + config.height + " offsetX: " + config.offsetX + " offsetY: " + config.offsetY);
            Console.WriteLine("Values: ");	
            for (int i = 0; i < config.width; i++)
            {
                for (int j = 0; j < config.height; j++)
                {
                    Console.Write(config.values[i][j] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }


        var app = config.setupServer();
        app.Run();

        return 0;
    }







}










[JsonSerializable(typeof(XYValues[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
