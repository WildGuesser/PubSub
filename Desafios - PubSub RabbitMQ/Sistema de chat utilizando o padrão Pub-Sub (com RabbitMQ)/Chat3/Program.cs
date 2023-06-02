using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;

namespace Chat3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Define o nome do tópico
            var topic = "chat_topic";

            // Declara um exchange do tipo 'fanout' para o tópico
            channel.ExchangeDeclare(exchange: topic, type: ExchangeType.Direct);

            // Cria uma fila temporária exclusiva para cada receptor
            var queueName = channel.QueueDeclare().QueueName;

            var routingKey = "logs";

            // Faz o binding da fila ao exchange associado ao tópico
            channel.QueueBind(queue: queueName, exchange: topic, routingKey: routingKey);

            Console.WriteLine("Digite sua mensagem (ou 'sair' para sair):");

            // Cria um consumidor para receber as mensagens
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Mensagem recebida: {message}");
            };

            // Inicia o consumo das mensagens na fila
            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            while (true)
            {
                // Lê a mensagem digitada pelo usuário
                var message = Console.ReadLine();

                if (message.ToLower() == "sair")
                    break;

                // Converte a mensagem em bytes
                var body = Encoding.UTF8.GetBytes(message);

                // Publica a mensagem no exchange associado ao tópico e com a routing key especifica
                channel.BasicPublish(exchange: topic, routingKey: routingKey, basicProperties: null, body: body);
            }
        }
    }
}