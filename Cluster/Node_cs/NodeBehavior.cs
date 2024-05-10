using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Node_cs
{
    class NodeBehavior{
        
        public static async Task<double [][]> GetValuesForInterpolation(int zeroed_actual_x, int zeroed_actual_y, ApiConfig config){

        Console.WriteLine("Filling in matrix values for "+zeroed_actual_x+"/"+ zeroed_actual_y+" ... ");
        double? [][] nullableMatrix = await GetActualValuesFromNodes(zeroed_actual_x, zeroed_actual_y, config);
        return FillInNullValues(nullableMatrix);
        
        
    }


    public static double[][] FillInNullValues(double? [][] values){
        //check wether or not the direct neighbors to the corners are null
        bool[] corners = new bool[4];
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = false;
        }
        if (values[0][0] == null ){
            if(values[0][1] == null && values[1][0] == null){
                corners[0] = true;
            }
            else{
                values[0][0] = values[0][1] == null ? values[1][0] : values[0][1];
            }
        }
        if (values[0][3] == null){
            if(values[0][2] == null && values[1][3] == null){
                corners[1] = true;
            }
            else{
                values[0][3] = values[0][2] == null ? values[1][3] : values[0][2];
            }
        }
        if (values[3][0] == null){
            if(values[3][1] == null && values[2][0] == null){
                corners[2] = true;
            }
            else{
                values[3][0] = values[3][1] == null ? values[2][0] : values[3][1];
            }
        }
        if (values[3][3] == null){
            if(values[3][2] == null && values[2][3] == null){
                corners[3] = true;
            }
            else{
                values[3][3] = values[3][2] == null ? values[2][3] : values[3][2];
            }
        }




        //fill in the values
        for(int i = 0; i < 4; i++){
            double? ToFillValue = null;
            for(int j = i == 0 ? 1 : i == 3 ? 1 : 0; j < (i == 3 ? 3 : i == 0 ? 3 : 4); j++){
                if (values[i][j] == null){
                    values[i][j] = ToFillValue;
                }else{
                    ToFillValue = values[i][j];
                }
            }
            for (int j = 2; j >= 0; j--){
                if (values[i][j] == null){
                    values[i][j] = ToFillValue;
                }else{
                    ToFillValue = values[i][j];
                }
            }
        }
        for(int j = 1; j <3; j++){
            double? ToFillValue = null;
            for(int i = 0; i < 4; i++){
                if (values[i][j] == null){
                    values[i][j] = ToFillValue;
                }else{
                    ToFillValue = values[i][j];
                }
            }
            for (int i = 2; i >= 0; i--){
                if (values[i][j] == null){
                    values[i][j] = ToFillValue;
                }else{
                    ToFillValue = values[i][j];
                }
            }
        }
        //handle corners now
        if (values[0][0] == null){
            values[0][0] = (values[0][1] + values[1][0]) / 2;
        }
        if (values[0][3] == null){
            values[0][3] = (values[0][2] + values[1][3]) / 2;
        }
        if (values[3][0] == null){
            values[3][0] = (values[3][1] + values[2][0]) / 2;
        }
        if (values[3][3] == null){
            values[3][3] = (values[3][2] + values[2][3]) / 2;
        }
        double[][] ret = new double[4][];
        for (int i = 0; i < 4; i++)
        {
            ret[i] = new double[4];
            for (int j = 0; j < 4; j++)
            {
#pragma warning disable CS8629 // Nullable value type may be null.
                    ret[i][j] = values[i][j].Value;
#pragma warning restore CS8629 // Nullable value type may be null.
                }
        }
        Console.WriteLine("Filled in values: ");
        //print the values
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Console.Write(ret[i][j] + " ");
            }
            Console.WriteLine();
        }
        return ret;
    }
    public static int[] GetNodePoints(int x, int y, ApiConfig config){
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

    //as of yet it is hardcoded since the nodes are hardcoded, too. Assuming that all nodes have the same height and width
    static String GetNodeURL(int x, int y, ApiConfig config){

        var nodePoints = GetNodePoints(x, y, config);

        return "http://node_x_" + nodePoints[0] + "_y_" + nodePoints[1] +":5552/getSavedValue/" + x + "/" + y + "/";
    }


    static async Task<double? [][]> GetActualValuesFromNodes(int zeroed_actual_x, int zeroed_actual_y, ApiConfig config){
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
        
            for (int i = -1; i < 3; i++)
            {   
                Console.WriteLine("Filling in row " + i);
                for (int j = -1; j < 3; j++){
                    TcpClient tcpClient = new TcpClient();
                    found = false;
                    current_x = zeroed_actual_x + i;
                    current_y = zeroed_actual_y + j;
                    //in order to correctly identify negative values, it is needed to offset them by one, otherwise the division will not work correctly
                    var nodePoints = GetNodePoints(current_x, current_y, config);
                    int node_x = nodePoints[0];
                    int node_y = nodePoints[1];
                    Console.WriteLine("Filling in value for " + current_x + "/" + current_y);
                    //check wether or not there is a connection

                    Console.WriteLine("Trying to connect to node_x_" + node_x + "_y_" + node_y);
                    var result = tcpClient.BeginConnect("node_x_" + node_x  + "_y_" + node_y, 5552, null, null);

                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(22));
                    if (success)
                    {
                        found = true;
                        tcpClient.EndConnect(result);
                    }
                    tcpClient.Close();
    

                    if (current_x >= config.offsetX && current_x < config.offsetX + config.width && current_y >= config.offsetY && current_y < config.offsetY + config.height){
                        values[i + 1][j + 1] = GetRequests.GetSavedNodeValue(current_x, current_y, config);
                        continue;
                    }
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
                    
                    values[i + 1][j + 1] = responseBody == "null" ? null : Double.Parse(responseBody, CultureInfo.InvariantCulture);
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
            return values;    
        }
    }
    }

}