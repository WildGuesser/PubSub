using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Send
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: "direct_logs", type: ExchangeType.Direct);

            Console.WriteLine("Topico para o qual queres mandar uma mensagem : ");
            var topic = Console.ReadLine();
            Console.WriteLine("Mensagem : ");
            var message = Console.ReadLine();

            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "direct_logs",
                            routingKey: topic,
                            basicProperties: null,
                            body: body);
            Console.WriteLine($" [x] Sent '{topic}':'{message}'");

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
