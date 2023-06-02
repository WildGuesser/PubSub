using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Client
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        //Aks the client for the ip
        Console.Write("Enter the server IP address: ");
        //Saves ip written by client in the string
        string ipAddress = Console.ReadLine();

        // Establish a connection with the server using the ip provided by the client
        TcpClient client = new TcpClient(ipAddress, 8888);

        //Receber a mensagem de OK
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int byte_count = await stream.ReadAsync(buffer, 0, buffer.Length);

        string response = Encoding.ASCII.GetString(buffer, 0, byte_count);
        Console.WriteLine("Server response: " + response);

        while (true)

        {
            // Read input from the user.
            Console.Write("Local path CSV file OR 'QUIT': ");
            string message = Console.ReadLine();

            if (message.ToUpper() == "QUIT")
            {
                byte[] _buffer = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(_buffer, 0, _buffer.Length);

                // Wait for a response from the server.
                _buffer = new byte[1026];
                int _byte_count = await stream.ReadAsync(_buffer, 0, _buffer.Length);

                // Print the response to the console.
                string _response = Encoding.UTF8.GetString(_buffer, 0, _byte_count);
                Console.WriteLine("Server response: " + _response);

                await Task.Delay(3000);

                // Close the connection.
                client.Client.Shutdown(SocketShutdown.Both);
                client.Close();

                break;
            }

            if (File.Exists(message))
            {
                string fileName = Path.GetFileName(message); // Recebe o filename do PATH

                // Enviar of filename para o server
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

                // Esperar resposta se recebeu filename
                buffer = new byte[1026];
                byte_count = await stream.ReadAsync(buffer, 0, buffer.Length);
                response = Encoding.UTF8.GetString(buffer, 0, byte_count);
                Console.WriteLine(response);

                using (var fileStream = new FileStream(message, FileMode.Open))
                {
                    await fileStream.CopyToAsync(stream);
                }

                // Resposta com o Resultado da leitura do ficheiro
                buffer = new byte[1026];
                byte_count = await stream.ReadAsync(buffer, 0, buffer.Length);
                response = Encoding.UTF8.GetString(buffer, 0, byte_count);
                Console.WriteLine();
                Console.WriteLine(response);
            }
            else
            {
                Console.WriteLine("O ficheiro nao existe." + Environment.NewLine);
                continue;
            }
        }
    }
}