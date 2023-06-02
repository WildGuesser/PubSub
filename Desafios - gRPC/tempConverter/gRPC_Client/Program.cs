using System;
using Grpc.Net.Client;
using GrpcService1;

namespace gRPC_Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var input = new HelloRequest { Name = "Diogo" };
            var celsius = 35;
            var input = new TemperatureRequest { Celsius = celsius };
            var channel = GrpcChannel.ForAddress("https://localhost:7138");

            //var client = new Greeter.GreeterClient(channel);
            var client = new TemperatureConverter.TemperatureConverterClient(channel);
            var reply = await client.ConvertToFahrenheitAsync(input);

            Console.WriteLine(celsius + " => " + reply.Fahrenheit);

            Console.ReadLine();
        }
    }
}

