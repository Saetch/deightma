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
        Console.WriteLine("Filling in matrix values for "+zeroed_actual_x+"/"+ zeroed_actual_y+" ... ");
        using (HttpClient httpClient = new HttpClient()){
            double? [][] values = new double?[4][];
            for (int i = 0; i < 4; i++)
            {
                values[i] = new double?[4];
            }
            String apiUrl;
            HttpResponseMessage response;
            var current_x = zeroed_actual_x;
            var current_y = zeroed_actual_y;
            bool found = false;
            TcpClient tcpClient = new TcpClient();
            for (int i = -1; i < 3; i++)
            {   
                Console.WriteLine("Filling in row " + i);
                for (int j = -1; j < 3; j++){
                    found = false;
                    current_x = zeroed_actual_x + i;
                    current_y = zeroed_actual_y + j;
                    //in order to correctly identify negative values, it is needed to offset them by one, otherwise the division will not work correctly
                    var nodePoints = GetNodePoints(current_x, current_y, config);
                    int node_x = nodePoints[0];
                    int node_y = nodePoints[1];
                    Console.WriteLine("Filling in value for " + current_x + "/" + current_y);
                    //check wether or not there is a connection
                    try
                        {
                            Console.WriteLine("Trying to connect to node_x_" + node_x + "_y_" + node_y);
                            tcpClient.Connect("node_x_" + node_x  + "_y_" + node_y, 5552);
                            found = true;
                            Console.WriteLine("Port open");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Port closed");
                        }
                    if (current_x >= config.offsetX && current_x < config.offsetX + config.width && current_y >= config.offsetY && current_y < config.offsetY + config.height){
                        values[i + 1][j + 1] = GetSavedNodeValue(current_x, current_y, config);
                        continue;
                    }
                    Console.WriteLine("Found: " + found);
                    if (found){
                        apiUrl = GetNodeURL(zeroed_actual_x + i, zeroed_actual_y + j, config);
                        Console.WriteLine("Making request to: " + apiUrl+" while trying to catch HTTPRequestExceptions");

                        // Make a GET request to the external API
                        response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, apiUrl));
                    }else{
                        Console.WriteLine("No server running, setting value to null!");
                        values[i + 1][j + 1] = null;
                        continue;
                    }

                    // Read the response content as a string
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Received response: " + responseBody);
                    values[i + 1][j + 1] = Double.Parse(responseBody, CultureInfo.InvariantCulture);
                }
            }        
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (values[i][j] != null){
                        Console.Write(values[i][j] + " ");
                    } else {
                        Console.Write("null ");
                    }
                }
                Console.WriteLine();
            }
            tcpClient.Close();
        }
        return config.values[(int)relative_x][(int)relative_y];
    }


//as of yet it is hardcoded since the nodes are hardcoded, too. Assuming that all nodes have the same height and width
    static String GetNodeURL(int x, int y, ApiConfig config){

        var nodePoints = GetNodePoints(x, y, config);

        return "http://node_x_" + nodePoints[0] + "_y_" + nodePoints[1] +":5552/getSavedValue/" + x + "/" + y + "/";
    }

    static int[] GetNodePoints(int x, int y, ApiConfig config){
        var ret = new int[2];
        if (x < 0) {
            ret [0] = (x-1) / config.width;
        }else{
            ret [0] = x / config.width;
        }
        if (y < 0) {
            ret [1] = (y-1) / config.height;
        }else{
            ret [1] = y / config.height;
        }
        return ret;
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