
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.ObjectPool;

namespace Node_cs
{
    class NodeBehavior{
        
        public static async Task<double [][]> GetValuesForInterpolation(int zeroed_actual_x, int zeroed_actual_y, ApiConfig config){
        var task1 = QueryAllNodesWithHashes(config);
        double? [][] nullableMatrix;
        var task2  = CheckMatrixValues(zeroed_actual_x, zeroed_actual_y, config);
        await Task.WhenAll(task1, task2);
        
        
        List<HashedPosition> toFindList = task2.Result.Item1;
        nullableMatrix = task2.Result.Item2;
        List<NodeResponse> nodeHashes = task1.Result;


        bool foundAllValues = await FillInQueriedValues(toFindList, nodeHashes, nullableMatrix, zeroed_actual_x -1 , zeroed_actual_y -1);
        return FillInNullValues(nullableMatrix);
        
        
    }

    private static async Task<List<NodeResponse>> QueryAllNodesWithHashes(ApiConfig config){
        Console.WriteLine("Querying hashes for all nodes ... ");
        using (HttpClient httpClient = new HttpClient()){
            string baseUrl = "http://"+config.COORDINATOR_SERVICE_URL+":"+config.PORT+"/organize/get_all_nodes";
            Console.WriteLine("Making request to: " + baseUrl);
            HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, baseUrl));
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Received response for values from nodes: " + responseBody);
            //deserialize responseBody to List<NodeResponse>
            var ret = ParseNodeResponsesFromResponse(responseBody);

            return ret;
        }
    }

    //get the input parameters from the query parameters in ASP.NET
    public static async Task<List<XYValues>> GetMultipleSavedValues(String query, ApiConfig config){
        var retList = new List<XYValues>();
        List<Point> positions = [];

        string[] positionString = query.Split(";");
        foreach (string pos in positionString){
            if( pos == "" ){
                continue;
            }
            string[] parts = pos.Split(",");
            if (parts.Length != 2){
                throw new Exception("Invalid query parameters: " + query);
            }
            int x = Int32.Parse(parts[0]);
            int y = Int32.Parse(parts[1]);
            positions.Add(new Point { x = x, y = y });
        }

        foreach (Point pos in positions){
            Console.WriteLine("Try getting saved value for: " + pos.x + "/" + pos.y);
            
            if (config.savedValues.ContainsKey(new Tuple<int, int>(pos.x, pos.y))){
                retList.Add(new XYValues { x = pos.x, y = pos.y, value = config.savedValues[new Tuple<int, int>(pos.x, pos.y)] });
            }
        }

        return retList;
    }

    private static async Task<bool> FillInQueriedValues( List<HashedPosition>  toFindLisT, List<NodeResponse> hashValues, double? [][] values, int starting_x, int starting_y){
        
        List<List<int>> parrallelListForNodes = [];
        for (int i = 0; i < hashValues.Count; i++)
        {
            parrallelListForNodes.Add([]);
        }
        int index = 0;
        foreach (HashedPosition point in toFindLisT){
            parrallelListForNodes[BinarySearchFittingNode(hashValues, point.hash)].Add(index);
            index++;
        }
        List<Task> tasks = [];
        for(int i = 0; i < hashValues.Count; i++){
            if (parrallelListForNodes[i].Count == 0){
                continue;
            }
            tasks.Add(FillInValuesForNode(parrallelListForNodes[i], toFindLisT, hashValues[i].name, values, starting_x, starting_y));
        }


        await Task.WhenAll(tasks);
        bool successfullyConnected = true;
        
        


        
        return successfullyConnected;
    }


    private static async Task FillInValuesForNode(List<int> positionIndices, List<HashedPosition> positions, String node, double? [][] values, int starting_x, int starting_y){
        Console.WriteLine("Filling in values for node: " + node);
        using (HttpClient httpClient = new HttpClient()){
            string apiUrl = "http://"+node+":5552/getMultipleSavedValues?parameters=";
            foreach (int index in positionIndices){
                apiUrl += positions[index].x + "," + positions[index].y + ";";
            }
            Console.WriteLine("Making request to: " + apiUrl);
            HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, apiUrl));
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Received response: " + responseBody);
            //deserialize responseBody to List<XYValues>
            List<Position> responseValues = ParseSavedValuesFromResponse(responseBody);
            foreach (Position value in responseValues){
                Console.WriteLine("Filling in value: " + value.value + " at position: " + value.x + "/" + value.y);
                Point pos = new Point { x = value.x, y = value.y };
                if (values[pos.x-starting_x][pos.y-starting_y] != null){
                    Console.WriteLine("Value already filled in, skipping ... Something must have gone wrong!!");
                }
                values[pos.x-starting_x][pos.y-starting_y] = value.value;

            }
        }
    }

    private static int BinarySearchFittingNode(List<NodeResponse> hashValues, int aimedValue){
        int start = hashValues[0].hash;
        int end = hashValues[^1].hash;
        if(aimedValue < start || aimedValue > end){
            return 0;
        }
        int len = hashValues.Count;
        int index = len/2;
        int upperBound = len -1;
        int lowerBound = 0;
        Console.WriteLine("Trying to find node for hashValue: "+aimedValue+" with binary search ... ");
        while (true){
            int currentValue = hashValues[index].hash;
            int nextValue = hashValues[index + 1].hash;
            if (aimedValue > currentValue && aimedValue <= nextValue){
                Console.WriteLine("Returning value from binary search: "+(index+1));
                return index + 1;
            }
            if(aimedValue > currentValue){
                lowerBound = index+1;
                index += Math.Max(1,(upperBound - index)/2);
            }else{
                upperBound = index -1;
                index -= Math.Max(1,(index - lowerBound)/2);
            }
        }

    }

    public static double[][] FillInNullValues(double? [][] values){
        bool allValues = true;
        //having null values is relatively unlikely, so we should check wether this is needed at all
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (values[i][j] == null){
                    Console.WriteLine("Null value found at: " + i + "/" + j);
                    allValues = false;
                    
                }
            }
        }

        if(!allValues){
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

        Console.WriteLine("Filled in values: ");


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


    private static List<NodeResponse> ParseNodeResponsesFromResponse(String responseBody){
        if (responseBody == null){
            throw new Exception("Response body is null");
        }
        if (responseBody == "" || responseBody == "[]"){
            return [];
        }
        List<NodeResponse> ret = [];
        string[] objects = responseBody.Replace("[{","").Replace("}]","").Split("},{");
        foreach (string obj in objects){
            string[] parts = obj.Split(",");
            var nameInputs = parts[0].Split(":");
            var hashInputs = parts[1].Split(":");
            if (!nameInputs[0].Equals("\"name\"") || !hashInputs[0].Equals("\"hash\"")){
                throw new Exception("Invalid response from coordinator service: " + responseBody);
            }
            String name = nameInputs[1].Replace("\"","");
            ushort hash = UInt16.Parse(hashInputs[1]);
            ret.Add(new NodeResponse { name = name, hash = hash });
        }
        return ret;
    }

    private static async  Task<Tuple<List<HashedPosition>, double? [][]>>CheckMatrixValues(int zeroed_actual_x, int zeroed_actual_y, ApiConfig config){
        var values = new double?[4][];
        for (int i = 0; i < 4; i++)
        {
            values[i] = new double?[4];
        }
        Console.WriteLine("Checking matrix values ... ");
        List<Point> toFindList = [];
        for (int i = -1; i < 3; i++)
        {
            for (int j = -1; j < 3; j++)
            {
                var tuple = new Tuple<int, int>(zeroed_actual_x + i, zeroed_actual_y + j);
                if (config.savedValues.ContainsKey(tuple)){
                    values[i + 1][j + 1] = config.savedValues[tuple];
                }else{
                    toFindList.Add(new Point { x = zeroed_actual_x + i, y = zeroed_actual_y + j });
                }
            }
        }

        var ret = await QueryHasherForPoints(toFindList);


        return Tuple.Create(ret, values);
    }



    public static String GetHoldingNode(int x, int y, ApiConfig config){
        Tuple<int, int> key = new Tuple<int, int>(x, y);
        
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
                    
                    values[i + 1][j + 1] = responseBody.Equals("\"null\"") ? null : Double.Parse(responseBody, CultureInfo.InvariantCulture);
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



        internal static async Task<List<XYValues>> DeleteSavedValuesBelow(string hash, ApiConfig api){
            
            //Get all possible hashes for the given points
            List<Point> hashTasks = new List<Point>();

            foreach (Tuple<int, int> key in api.savedValues.Keys){
                hashTasks.Add(new Point { x = key.Item1, y = key.Item2 });
            }
            var result = await QueryHasherForPoints(hashTasks);

            short hashVal;
            try
            {
                hashVal = short.Parse(hash);
            }catch (Exception e){
                throw new Exception("Invalid hash value: " + hash +" Exception: " + e.Message);
            }
            
            var resultXYValues = new List<XYValues>();
            //fin all values that are below the given hash value in the ringhash
            foreach (HashedPosition pointHash in result)
            {
                //check wether or not the hash barrier wraps around
                if (hashVal < api.ownerHash){
                    if (pointHash.hash > hashVal && pointHash.hash < api.ownerHash ){     //value is above the hash value, no wrapping
                        continue;  
                    }
                }else{
                    if(pointHash.hash > hashVal || pointHash.hash < api.ownerHash){  //value is between hashvalue and owner hash, wrapping
                        continue;
                    }
                }
                var tuple = new Tuple<int, int>(pointHash.x, pointHash.y);
                var ret = api.savedValues.ContainsKey(tuple);
                if (!ret) throw new Exception("Tried removing nonexistent value");

                double outp;
                api.savedValues.TryGetValue(new Tuple<int, int>(pointHash.x, pointHash.y), out outp);
                var removed = api.savedValues.TryRemove(new KeyValuePair<Tuple<int, int>, double>(new Tuple<int, int>(pointHash.x, pointHash.y), outp));
                if (!removed) throw new Exception("Failed to remove value");
                resultXYValues.Add(new XYValues { x = pointHash.x, y = pointHash.y, value = outp });
                
            }

            return resultXYValues;
            
        }


        private static async Task<List<HashedPosition>> QueryHasherForPoints(List<Point> positions){
            if (positions.Count == 0){
                return [];
            }
            Console.WriteLine("Querying hasher service for points ... ");
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = AppJsonSerializerContext.Default
            };
            using (HttpClient httpClient = new HttpClient()){
                string baseUrl = "http://hasher-service:8080/hash_multiple";
                Console.WriteLine("Making request to: " + baseUrl);
                var json = JsonSerializer.Serialize(positions, options);
                var query = "?vec=" + json.Replace("[{","").Replace("}]","").Replace("},{",";").Replace("\"","");
                string apiUrl = baseUrl + query;
                Console.WriteLine("Making request to: " + apiUrl);
                HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, apiUrl));
                string? responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Received response: " + responseBody);
                //deserialize into list of HashedPosition
                List<HashedPosition> result = ParseHashedPositionsFromResponse(responseBody ?? ""); // Use the null-coalescing operator to provide a default value in case the response body is null
                Console.WriteLine("Returning result: " + result);
                return result ?? [];
            };
        }

        internal static List<Position> ParseSavedValuesFromResponse(String responseBody){
            if (responseBody == null){
                throw new Exception("Response body is null");
            }
            if (responseBody == "" || responseBody == "[]"){
                return [];
            }
            List<Position> ret = [];
            string[] objects = responseBody.Replace("[{","").Replace("}]","").Split("},{");
            foreach (string obj in objects){
                string[] parts = obj.Split(",");
                var xInputs = parts[0].Split(":");
                var yInputs = parts[1].Split(":");
                var valueInputs = parts[2].Split(":");
                if (!xInputs[0].Equals("\"x\"") || !yInputs[0].Equals("\"y\"") || !valueInputs[0].Equals("\"value\"")){
                    throw new Exception("Invalid response from hasher service: " + responseBody);
                }
                int x = Int32.Parse(xInputs[1]);
                int y = Int32.Parse(yInputs[1]);
                double value = Double.Parse(valueInputs[1]);
                ret.Add(new Position { x = x, y = y, value = value });
            }
            return ret;
        }


        internal static List<HashedPosition> ParseHashedPositionsFromResponse(String responseBody){
        if (responseBody == null){
            throw new Exception("Response body is null");
        }
        if (responseBody == "" || responseBody == "[]"){
            return [];
        }
        List<HashedPosition> ret = [];
        string[] objects = responseBody.Replace("[{","").Replace("}]","").Split("},{");
        foreach (string obj in objects){
            string[] parts = obj.Split(",");
            var xInputs = parts[0].Split(":");
            var yInputs = parts[1].Split(":");
            var hashInputs = parts[2].Split(":");
            if (!xInputs[0].Equals("\"x\"") || !yInputs[0].Equals("\"y\"") || !hashInputs[0].Equals("\"hash\"")){
                throw new Exception("Invalid response from hasher service: " + responseBody);
            }
            int x = Int32.Parse(xInputs[1]);
            int y = Int32.Parse(yInputs[1]);
            ushort hash = UInt16.Parse(hashInputs[1]);
            ret.Add(new HashedPosition { x = x, y = y, hash = hash });
        }
        return ret;
    }   
    }




}

