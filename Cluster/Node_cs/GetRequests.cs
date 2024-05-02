using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Node_cs
{
    public class GetRequests
    {
        
    static double calculateInterpolatedValue(double x, double y, ApiConfig config)
    {
        Console.WriteLine("Received request for x: " + x + " y: " + y);
        double actual_x = x - config.offsetX;
        double actual_y = y - config.offsetY;
        Console.WriteLine("Calculating value for inner x: " + actual_x + " y: " + actual_y);
        //dummy implementation to make sure the network works! TODO: implement actual interpolation
        actual_x = Math.Round(actual_x);
        actual_y = Math.Round(actual_y);
        actual_x = Math.Max(0, Math.Min(config.width - 1, actual_x));
        actual_y = Math.Max(0, Math.Min(config.height - 1, actual_y));
        Console.WriteLine("Bounded indices are Actual x: " + actual_x + " y: " + actual_y);
        Console.WriteLine("Casted to int these are : " + (int)actual_x + " y: " + (int)actual_y);
        Console.WriteLine("Returning value: " + config.values[(int)actual_x][(int)actual_y]);
        return config.values[(int)actual_x][(int)actual_y];
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
        double ret_value = calculateInterpolatedValue(x, y,config);
        return new XYValues { x = x, y = y, value = ret_value };
    }
    }
}