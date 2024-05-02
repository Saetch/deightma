// See https://aka.ms/new-console-template for more information

BASE_URL = localhost:5000;

class Client{

    public static async void Main(){
        Console.WriteLine("Please Enter your x-value");
        string x = Console.ReadLine();
        Console.WriteLine("Your x-value is: " + x + ". Please enter your y-value");
        string y = Console.ReadLine();
        Console.WriteLine("Your y-value is: " + y + ". Coordinates gathered, please wait.");

        DistributedSystemWebClient webClient = new DistributedSystemWebClient(BASE_URL);

        try
        {
            string value = await webClient.GetValue(x, y);
            Console.WriteLine($"Value received: {value}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

    }
}
