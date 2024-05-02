// See https://aka.ms/new-console-template for more information

class Client{
    static string BASE_URL = "http://127.0.0.1:5000";

    public static void Main(){
        Console.WriteLine("Please Enter your x-value");
        string x = Console.ReadLine();
        Console.WriteLine("Your x-value is: " + x + ". Please enter your y-value");
        string y = Console.ReadLine();
        Console.WriteLine("Your y-value is: " + y + ". Coordinates gathered, please wait.");

        DistributedSystemWebClient webClient = new DistributedSystemWebClient(BASE_URL);

        try
        {
            var value = webClient.GetValue(x, y);
            string returnValue=value.Result;
            Console.WriteLine($"Value received: {returnValue}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

    }
}
