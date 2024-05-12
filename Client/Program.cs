// See https://aka.ms/new-console-template for more information

class Client{
    static string BASE_URL = "http://127.0.0.1:5000";

    public static void Main(){
        while(true){
            Console.WriteLine("Do you want to (1) Get a value or (2) Change a value or (3) Exit")
            string input= Console.ReadLine();
            if input=="1"{
                getValue();
            }elif input=="2"{

                
            }elif input=="3"{
                break;
            }else{
                Console.WriteLine("You have selected an invalid option")
            }
        }
    }

    public static void getValue(){
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

        return;
    }

    public static void setValue(){
        Console.WriteLine("Please Enter your x-value");
        string x = Console.ReadLine();
        Console.WriteLine("Your x-value is: " + x + ". Please enter your y-value");
        string y = Console.ReadLine();
        Console.WriteLine("Your y-value is: " + y + ". Please enter to what value you want it to change.");
        string z = Console.ReadLine();
        Console.WriteLine("Informations gathered, please wait.")

    }

    DistributedSystemWebClient webClient = new DistributedSystemWebClient(BASE_URL);

        try
        {
            var value = webClient.SetValue(x, y, z);
            string returnValue=value.Result;
            Console.WriteLine($"Value received: {returnValue}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return;
}
