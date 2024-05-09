using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace Node_cs
{
    public class GetRequests
    {
        
    static async Task<double> CalculateInterpolatedValue(double x, double y, ApiConfig config)
    {
        Console.WriteLine("Received request for x: " + x + " y: " + y);
        double relative_x = Math.Round(x - config.offsetX);
        double relative_y = Math.Round(y - config.offsetY);
        Console.WriteLine("Calculating value for inner x: " + relative_x + " y: " + relative_y);
        if(relative_x < 0 || relative_y < 0 || relative_x >= config.width || relative_y >= config.height ){
            throw new Exception("Requested value is out of bounds");
        }
        Console.WriteLine("Value is within bounds, calculating ... ");
        //dummy implementation to make sure the network works! TODO: implement actual interpolation
        int zeroed_actual_x = (int)Math.Floor(x);
        int zeroed_actual_y = (int)Math.Floor(y);
        double? [][] valueArray = await NodeBehavior.GetValuesForInterpolation(zeroed_actual_x, zeroed_actual_y, config);
        return config.values[(int)relative_x][(int)relative_y];
    }






    public static double? GetSavedNodeValue(int x, int y, ApiConfig config){
        x = x - config.offsetX;
        y = y - config.offsetY;
        
        if (x < 0 || y < 0 || x >= config.width || y >= config.height){
            return null;
        }

        return config.values[x][y];
    }

    public static XYValues GetValue(string values, ApiConfig config)
    {
        Console.WriteLine("Received GetValue-call with params: " + values);
        string[] splitValues = values.Split("_");
        double x = Double.Parse(splitValues[0].Replace(',', '.'), CultureInfo.InvariantCulture);
        double y = Double.Parse(splitValues[1].Replace(',', '.'), CultureInfo.InvariantCulture);
        if(splitValues.Length > 2){
            throw new Exception("Too many values");
        }    
        var ret_value = CalculateInterpolatedValue(x, y,config);    
        ret_value.Wait();
        double ret_val = ret_value.Result;
        return new XYValues { x = x, y = y, value = ret_val };
    }
    }
}