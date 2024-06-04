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
        Tuple<int, int> key = new Tuple<int, int>((int)Math.Round(x), (int)Math.Round(y));
        if (!config.savedValues.ContainsKey(key))
        {
            Console.WriteLine("Closest value not found in saved values, aborting ...");
            throw new Exception("Closest value not found in saved values, aborting ...");
        }
        //dummy implementation to make sure the network works! TODO: implement actual interpolation
        int zeroed_actual_x = (int)Math.Floor(x);
        int zeroed_actual_y = (int)Math.Floor(y);
        double [][] valueArray = await NodeBehavior.GetValuesForInterpolation(zeroed_actual_x, zeroed_actual_y, config);

        Console.WriteLine("Successfully got values for interpolation");
        //request solving the interpolation from the bicubic interpolation service
        var client = new HttpClient();
        String arr = "";
        for (int i = 0; i < valueArray.Length; i++)
        {
            for (int j = 0; j < valueArray[i].Length; j++)
            {
                arr += valueArray[i][j] + (j == valueArray[i].Length -1 ? ";" : ",");
            }
        }
        arr = arr.TrimEnd(';');
        Console.WriteLine("Sending request to bicubic interpolation service with values: " + arr);
        var response = await client.GetAsync(config.BICUBIC_INTERPOLATION_SERVICE_URL + "?x="+(x -zeroed_actual_x)+"&y="+(y -zeroed_actual_y)+"&arr=" + arr);
        Console.WriteLine("Received response from bicubic interpolation service: " + response.Content.ReadAsStringAsync().Result);
        double actual_value = Double.Parse(response.Content.ReadAsStringAsync().Result, CultureInfo.InvariantCulture);
        return actual_value;
    }






    public static double? GetSavedNodeValue(int x, int y, ApiConfig config){        
        Console.WriteLine("Received SavedValue-call with params: " + x + "/" + y);
        Tuple<int, int> key = new Tuple<int, int>(x, y);
        if(config.savedValues.ContainsKey(key)){
            return config.savedValues[key];
        }
        return null;
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
        var ret_value = CalculateInterpolatedValue(x, y,config).Result;    
        Console.WriteLine("Returning value: " + ret_value);
        return new XYValues { x = x, y = y, value = ret_value };
    }
    }
}