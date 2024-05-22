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
        for(int j = 0; j < 4; j++){
            double? ToFillValue = null;
            for(int i = j == 0 ? 1 : j == 3 ? 1 : 0 ; i < (j==3 ? 3 : j == 0 ? 3 : 4); i++){
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


    public static String GetHoldingNode(int x, int y, ApiConfig config){
        Tuple<int, int> key = new Tuple<int, int>(x, y);
        
        //this was the old way of getting the node faster, but since the values are now redistributed automatically, this needs to be reworked        
/*        if (config.exteriorValuesInNodes.TryGetValue(key, out string? value)){
            TcpClient tcpClient = new TcpClient();
            var result = tcpClient.BeginConnect(value, 5552, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(22));
            if (success)
            {
                tcpClient.EndConnect(result);
                tcpClient.Close();
                return value;
            }
            else{
                Console.WriteLine("Failed to connect to known node, requesting new information ... ");
                tcpClient.Close();
            }
        }  else {
            Console.WriteLine("No information about node found, requesting new information ... ");
        } */
        
        TcpClient tcpClient2 = new TcpClient();
        var result2 = tcpClient2.BeginConnect(config.COORDINATOR_SERVICE_URL, config.PORT, null, null);
        var success2 = result2.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(150));
        if (success2)
        {
            tcpClient2.EndConnect(result2);
        }
        else{
            while (!success2){
                Console.WriteLine("Failed to connect to coordinator service ... ");
                //sleep for 1 second
                System.Threading.Thread.Sleep(1000);
                result2 = tcpClient2.BeginConnect(config.COORDINATOR_SERVICE_URL, config.PORT, null, null);
                success2 = result2.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(150));
            }
            tcpClient2.EndConnect(result2);
        }
        tcpClient2.Close();
        HttpClient client = new HttpClient();
        var response = client.GetAsync("http://" + config.COORDINATOR_SERVICE_URL + ":"+ config.PORT+"/organize/get_node/"+x+"/"+y).Result;
        var node = response.Content.ReadAsStringAsync().Result;
        return node;
        
    }

    //as of yet it is hardcoded since the nodes are hardcoded, too. Assuming that all nodes have the same height and width
    static String? GetNodeURL(int x, int y, ApiConfig config){

        var nodePoints = GetHoldingNode(x, y, config);

        if (nodePoints.Equals("Unknown")){
            Console.WriteLine("Node not found!");
            return null; 
        }
        return "http://"+nodePoints + ":5552" +"/getSavedValue/"+x+"/"+y;
    }


    static async Task<double? [][]> GetActualValuesFromNodes(int zeroed_actual_x, int zeroed_actual_y, ApiConfig config){
         using (HttpClient httpClient = new HttpClient()){
            Tuple<int, int> key = new Tuple<int, int>(zeroed_actual_x, zeroed_actual_y);
            double? [][] values = new double?[4][];
            for (int i = 0; i < 4; i++)
            {
                values[i] = new double?[4];
            }
            String apiUrl;
            String? node;
            HttpResponseMessage response;
            for (int i = -1; i < 3; i++)
            {   
                Console.WriteLine("Filling in row " + i);
                for (int j = -1; j < 3; j++){
                    Tuple<int, int> current_key = new Tuple<int, int>(zeroed_actual_x + i, zeroed_actual_y + j);
                    if (config.savedValues.ContainsKey(current_key)){
                        Console.WriteLine("Value found in saved values, using it ... ");
                        Console.WriteLine("Key: "+current_key+"   ->   " + config.savedValues[current_key]);
                        values[i+1][j+1] = config.savedValues.GetValueOrDefault(current_key);
                        continue;
                    }
                    node = GetNodeURL(zeroed_actual_x + i, zeroed_actual_y + j, config);
                    if (node == null){
                        Console.WriteLine("Node not found, setting value to null!");
                        values[i + 1][j + 1] = null;
                        continue;
                    }
                    apiUrl = node;
                    Console.WriteLine("Making request to: " + apiUrl+" while trying to catch HTTPRequestExceptions");

                    // Make a GET request to the external API
                    response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, apiUrl));


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