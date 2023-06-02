using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Mysqlx.Crud;

/*
FileProcessingStatus é usada para representar o status de processamento do ficheiro. 
Possui quatro estados possíveis: OPEN (quando o ficheiro é aberto), 
ERROR (quando ocorre um erro durante o processamento), IN_PROGRESS (quando o ficheiro está a ser processado) 
e DONE (quando o ficheiro é processado com sucesso).
 
 */

enum FileProcessingStatus
{
    OPEN,
    ERROR,
    IN_PROGRESS,
    DONE
}

class Server
{


    static string _FileName { get; set; }
    static Mutex mutex = new Mutex();
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        //A classe TCPListener implementa os métodos da classe Socket utilizando o protócolo TCP, permitindo uma maior abstração das etapas tipicamente associadas ao Socket.
        TcpListener ServerSocket = new TcpListener(IPAddress.Any, 8888);
        //A chamada ao método "Start" inicia o Socket para ficar à escuta de novas conexões por parte dos clientes
        ServerSocket.Start();

        while (true)
        { //Ciclo infinito para ficar à espera que um cliente Socket/TCP até quando pretender conectar-se

            TcpClient client = ServerSocket.AcceptTcpClient();

            //Só avança para esta parte do código, depois de um cliente ter se conectado ao servidor
            Console.WriteLine("A client has connected!");
            //Quando inicialmente contactado, o servidor deve responder com uma mensagem de
            //“100 OK”
            Send100(client);

            Thread clientThread = new Thread(() => handle_client(client));
            clientThread.Start();
        }
    }

    /*O método handle_client é executado em uma nova thread e é responsável por receber o 
     * arquivo de texto enviado pelo cliente e processá-lo. Ele usa um mutex para garantir que 
     * apenas uma thread por vez acesse a base de dados. 
     * Ele também envia mensagens de volta para o cliente informando o status de processamento do ficheiro.
     */

    public static void handle_client(TcpClient client)
    { // Neste método, é iniciada a gestão da comunicação do servidor com o cliente

        while (true)
        {
            //ciclo infinito de receção de mensagens por parte do cliente, até o cliente ter enviado uma mensagem vazia (só clicou no 'Enter')
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[5000];
            int byte_count = stream.Read(buffer, 0, buffer.Length);
            //passar a mensagem recebida para string 
            string message = Encoding.UTF8.GetString(buffer, 0, byte_count);

            //comparar a ver se é quit
            if (message.ToUpper() == "QUIT")
            {
                //envia a mensagem QUIT
                Send400(client);
                // código para desligar a conexão com o cliente
                client.Client.Shutdown(SocketShutdown.Both);
                client.Close();

                break;

            }
            //guardar o filename
            else if (message.ToLower().EndsWith(".csv"))
            {
              
                 _FileName = message;
                //mensagem de ack que recebeu o filename.
                Send100(client, "Filename => " + _FileName);
            }
            else
            {
                FileProcessingStatus processingStatus = FileProcessingStatus.OPEN;

                try
                {
                    // o mutex garante que apenas uma thread por vez acesse a base de dados
                    mutex.WaitOne();


                    DateTime date_process = DateTime.Now;
           
                    File_Processed file = new File_Processed
                    {
                        filename = _FileName,
                        date_proc = date_process
                    };

                    processingStatus = FileProcessingStatus.IN_PROGRESS;

                    if (Insert_File_DB(file))
                    {
                        throw new Exception("Already processed");
                    }
                    else
                    {
                        Domícilio dom = new Domícilio();
                        string[] lines = message.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        string operadora = lines[0].Split(',')[0];
                        string[] overlaps = new string[lines.Length];

                        Dictionary<string, int> addressCounts = new Dictionary<string, int>();

                        //ciclo responsável pela classificação de domicilios por municipio e sobreposições
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string[] parts = lines[i].Split(',');
                            string municipio = parts[1];

                            dom.Operadora = operadora;
                            dom.Morada = parts[2];
                            dom.Municipio = municipio;
                            if (parts[3] == "1")
                                dom.Owner = true;
                            else
                                dom.Owner = false;

                            overlaps[i] = Inserir_Domicilio_DB(dom);

                            if (!addressCounts.ContainsKey(municipio))
                            {
                                addressCounts[municipio] = 1;
                                for (int j = i + 1; j < lines.Length; j++)
                                {
                                    string[] parts_mun = lines[j].Split(',');
                                    string mun_aux = parts_mun[1];
                                    if (mun_aux == municipio)
                                    {
                                        addressCounts[municipio]++;
                                    }
                                }
                            }
                        }

                        processingStatus = FileProcessingStatus.DONE;

                        // Cria uma mensagem com as contagens e a envia de volta ao cliente
                        string statusMessage = "Estado de processamento : " + processingStatus + Environment.NewLine + Environment.NewLine + "Data de processamento : " + date_process + Environment.NewLine + Environment.NewLine +
                          "Nome da operadora : " + operadora + Environment.NewLine + Environment.NewLine +
                          "Classificacao: " + Environment.NewLine;

                        foreach (string _municipio in addressCounts.Keys)
                        {
                            statusMessage += "\t" + _municipio + ": " + addressCounts[_municipio] + Environment.NewLine;
                        }

                        statusMessage += Environment.NewLine + Environment.NewLine + "Sobreposicao(oes) : " + Environment.NewLine + Environment.NewLine;

                        foreach (string overlap in overlaps)
                        {
                            if (overlap != "")
                            {
                                statusMessage += "\t" + overlap + Environment.NewLine;
                            }
                        }

                        broadcast_file(client, statusMessage);
                        mutex.ReleaseMutex();
                    }
                }
                catch (Exception ex)
                {
                    processingStatus = FileProcessingStatus.ERROR;

                    string statusMessage = "Processing status: " + processingStatus + Environment.NewLine + Environment.NewLine + ex;
                    broadcast_file(client, statusMessage);
                }
            }
        }
    }

    //Função usada para enviar uma mensagem de volta ao cliente
    public static void broadcast_file(TcpClient c, string data)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(data + Environment.NewLine);
        NetworkStream stream = c.GetStream();
        stream.Write(buffer, 0, buffer.Length);
    }

    //função usada para enviar a mensagem de encerramento de conexão 400 - BYE
    public static void Send400(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = Encoding.ASCII.GetBytes("400 - BYE" + Environment.NewLine);
        stream.Write(buffer, 0, buffer.Length);
    }

    //função usada para mandar a mensagem 100 - OK
    public static void Send100(TcpClient client, string extraMessage = null)
    {
        NetworkStream stream = client.GetStream();
        string message = "100 - OK: " + (extraMessage ?? "") + Environment.NewLine;
        byte[] buffer = Encoding.ASCII.GetBytes(message);
        stream.Write(buffer, 0, buffer.Length);
    }

    //função responsável por adicionar domicilios não existentes à base de dados, caso exista envia uma 
    //string com informações sobre a sobreposição caso não exista retorna uma string vazia
    public static string Inserir_Domicilio_DB(Domícilio obj)
    {

        string connectionString = "Server=(localdb)\\mssqllocaldb;Database=TP1;Trusted_Connection=True;MultipleActiveResultSets=true";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string checkSql = "SELECT * FROM Domicilio WHERE Morada = @morada AND Municipio = @municipio";
            //db : Check if it already exists a record w/ comparations using morada AND municipio
            //_checksql = count records, with db as a subquery, where operator is dif. -> suposition
            // db as ( checkSQL )
            //string _checkSql = "SELECT COUNT(*) FROM Domicilio";
                //count(*) from db where obj.Operadora <> @operadora;
            // This checks first if there's the same municipio and morada, secondly, if it's 

            using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@morada", obj.Morada);
                checkCommand.Parameters.AddWithValue("@municipio", obj.Municipio);

                using (SqlDataReader reader = checkCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string existingMorada = reader.GetString(reader.GetOrdinal("Morada"));
                        string existingOperadora = reader.GetString(reader.GetOrdinal("Operadora"));

                        reader.Close();
                        connection.Close();

                        return obj.Operadora + " sobrepos com " + existingOperadora + " na seguinte morada: " + existingMorada;

                    }
                    else
                    {
                        string insertSql = "INSERT INTO Domicilio (Operadora, Morada, Municipio, Owner) VALUES (@operadora, @morada, @municipio, @owner)";

                        using (SqlCommand insertCommand = new SqlCommand(insertSql, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@operadora", obj.Operadora);
                            insertCommand.Parameters.AddWithValue("@morada", obj.Morada);
                            insertCommand.Parameters.AddWithValue("@municipio", obj.Municipio);
                            insertCommand.Parameters.AddWithValue("@owner", obj.Owner);

                            insertCommand.ExecuteNonQuery();

                            reader.Close();
                            connection.Close();

                            return string.Empty;
                        }

                    }
                }
            }
        }
    }

    //função responsável por adicionar à base de dados os ficheiros processados
    public static bool Insert_File_DB(File_Processed fp)
    {
        string connectionString = "Server=(localdb)\\mssqllocaldb;Database=TP1;Trusted_Connection=True;MultipleActiveResultSets=true";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string checkSql = "SELECT COUNT(*) FROM File_Processed WHERE filename = @filename";

            using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@filename", fp.filename);

                int _file = (int)checkCommand.ExecuteScalar();

                if (_file > 0)
                {
                    return true;
                }
                else
                {
                    string insertSql = "INSERT INTO File_Processed (filename, date_proc) VALUES (@filename, @date_proc)";

                    using (SqlCommand insertCommand = new SqlCommand(insertSql, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@filename", fp.filename);
                        insertCommand.Parameters.AddWithValue("@date_proc", fp.date_proc);

                        insertCommand.ExecuteNonQuery();
                    }

                    return false;
                }
            }

        }
    }
}

public class Domícilio
{
    public string Operadora
    {
        get;
        set;
    }
    public string Morada
    {
        get;
        set;
    }
    public string Municipio
    {
        get;
        set;
    }
    public bool Owner
    {
        get;
        set;
    }
}

public class File_Processed
{
    public string filename
    {
        get;
        set;
    }
    public DateTime date_proc
    {
        get;
        set;
    }
}