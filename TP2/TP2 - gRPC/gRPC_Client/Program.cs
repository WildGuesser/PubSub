using System;
using System.Text;
using Grpc.Net.Client;
using GrpcServer;
using Grpc.Core;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Channels;

namespace GrpcClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool loginresult;


            do
            {
                Console.WriteLine("Digite o nome de usuário:");
                string username = Console.ReadLine();
                Console.WriteLine("Digite a senha:");
                string password = Console.ReadLine();

                var input = new LoginUserModel { Username = username, Password = password };

                var httpHandler = new HttpClientHandler();
                httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                //var channel = GrpcChannel.ForAddress("https://25.50.88.40:7060", new GrpcChannelOptions { HttpHandler = httpHandler });
                var channel = GrpcChannel.ForAddress("https://localhost:7060", new GrpcChannelOptions { HttpHandler = httpHandler });
                var client = new Operador.OperadorClient(channel);
                var response = await client.LoginUserAsync(input);

                loginresult = response.Result;

                if (loginresult == true)
                {
                    Console.Clear();
                    Console.WriteLine(response.Response + Environment.NewLine);
                    if (response.UserFlag)
                    {
                        while (true)
                        {
                            Console.Clear();
                            Console.Write("MENU" + Environment.NewLine);
                            Console.WriteLine("1. Listar cobertura disponivel");
                            Console.WriteLine("2. Listar dados de processos em curso");
                            Console.WriteLine("3. Criar Utilizador");
                            Console.WriteLine("4. Apagar Utilizador");
                            Console.WriteLine("5. Editar Utilizador");
                            Console.WriteLine("6. Exit");
                            string choice = Console.ReadLine();

                            switch (choice)
                            {
                                case "1":
                                    Console.Clear();
                                    string owner;
                                    Console.WriteLine("\n");
                                    Console.WriteLine(
                                        "Lista de Todos os Domícilios Disponíveis: \n"
                                    );
                                    using (var call = client.Listing(new ListingProcess()))
                                    {
                                        while (await call.ResponseStream.MoveNext())
                                        {
                                            Thread.Sleep(1000);
                                            
                                            var currentdom = call.ResponseStream.Current;
                                            string Modalidade = currentdom.Downstream.ToString() + "_" + currentdom.Upstream.ToString();
                                            if (currentdom.Owner == true)
                                            {
                                                owner = "Sim";
                                            }
                                            else
                                            {
                                                owner = "Não";
                                            }
                                            Console.WriteLine(
                                                "ID Unico: "
                                                    + currentdom.IdUnico.ToString()
                                                    + ";\n"
                                                    + "Operadora: "
                                                    + currentdom.Operadora
                                                    + ";\n"
                                                    + "Morada: "
                                                    + currentdom.Morada
                                                    + ";\n"
                                                    + "Município: "
                                                    + currentdom.Municipio
                                                    + ";\n"
                                                    + "Owner: "
                                                    + owner
                                                    + ";\n"
                                                    + "Estado: "
                                                    + currentdom.State
                                                    + ";\n"
                                                    + "Operador: "
                                                    + currentdom.Operador
                                                    + ";\n"
                                                    + "Modalidade: "
                                                    + Modalidade
                                                    + ";\n"
                                            );
                                        }
                                    }
                                    Console.WriteLine("Press enter");
                                    Console.ReadLine();
                                    break;
                                case "2":
                                    Console.Clear();
                                    string owner2;
                                    Console.WriteLine("\n");
                                    Console.WriteLine(
                                        "Lista de Todos os Domícilios com Processos em Curso: \n"
                                    );
                                    using (var call = client.ListingActive(new ListingProcess()))
                                    {
                                        while (await call.ResponseStream.MoveNext())
                                        {
                                            Thread.Sleep(1000);
                                            var currentdom = call.ResponseStream.Current;
                                            string Modalidade = currentdom.Downstream.ToString() + "_" + currentdom.Upstream.ToString();
                                            if (currentdom.Owner == true)
                                            {
                                                owner2 = "Sim";
                                            }
                                            else
                                            {
                                                owner2 = "Não";
                                            }
                                            Console.WriteLine(
                                                "ID Unico: "
                                                    + currentdom.IdUnico.ToString()
                                                    + ";\n"
                                                    + "Operadora: "
                                                    + currentdom.Operadora
                                                    + ";\n"
                                                    + "Morada: "
                                                    + currentdom.Morada
                                                    + ";\n"
                                                    + "Município: "
                                                    + currentdom.Municipio
                                                    + ";\n"
                                                    + "Owner: "
                                                    + owner2
                                                    + ";\n"
                                                    + "Estado: "
                                                    + currentdom.State
                                                    + ";\n"
                                                    + "Operador: "
                                                    + currentdom.Operador
                                                    + ";\n"
                                                     + "Modalidade: "
                                                    + Modalidade
                                                    + ";\n"
                                            );
                                        }
                                    }
                                    Console.WriteLine("\nPrima Enter");
                                    break;
                                case "3":
                                    string newUserUsername;
                                    string newUserPassword;
                                    int newUserType;

                                    Console.WriteLine("Digite o nome do novo Utilizador:");
                                    newUserUsername = Console.ReadLine();
                                    Console.WriteLine("Digite a senha do novo Utilizador:");
                                    newUserPassword = Console.ReadLine();

                                    bool isValidUserType = false;
                                    do
                                    {
                                        Console.WriteLine(
                                            "Digite o tipo de Utilizador (1 - Admin, 0 - Operador):"
                                        );
                                        string userInput = Console.ReadLine();

                                        if (int.TryParse(userInput, out newUserType))
                                        {
                                            if (newUserType == 1 || newUserType == 0)
                                            {
                                                isValidUserType = true;
                                            }
                                            else
                                            {
                                                Console.WriteLine(
                                                    "Tipo de Utilizador inválido. Digite 1 para Admin ou 2 para Operador."
                                                );
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine(
                                                "Entrada inválida. Digite 1 para Admin ou 2 para Operador."
                                            );
                                        }
                                    } while (!isValidUserType);

                                    var createUserRequest = new CreateUserModel
                                    {
                                        Username = newUserUsername,
                                        Password = newUserPassword,
                                        Type = ((newUserType == 1) ? true : false),
                                    };

                                    var createUserResponse = await client.CreateUserAsync(
                                        createUserRequest
                                    );
                                    Console.WriteLine(createUserResponse.Response);
                                    Console.WriteLine("\nPressione Enter");
                                    break;

                                case "4":
                                    Console.WriteLine(
                                        "Digite o nome do utilizador que deseja remover: "
                                    );
                                    string utilizadorToRemove = Console.ReadLine();

                                    Console.WriteLine(
                                        "Tem certeza de que deseja remover o utilizador "
                                            + utilizadorToRemove
                                            + "? (sim/nao)"
                                    );
                                    string confirmacao = Console.ReadLine();

                                    if (confirmacao.ToLower() == "sim")
                                    {
                                        var removeUserRequest = new RemoveUserRequest
                                        {
                                            Username = utilizadorToRemove
                                        };
                                        var removeUserResponse = await client.RemoveUserAsync(
                                            removeUserRequest
                                        );

                                        Console.WriteLine(removeUserResponse.Response);
                                    }
                                    else
                                    {
                                        Console.WriteLine(
                                            "Operação de remoção do utilizador cancelada."
                                        );
                                    }

                                    Console.WriteLine("\nPressione Enter");
                                    break;

                                case "5":
                                    Console.WriteLine("Editar Utilizador");
                                    Console.WriteLine("Insira o nome do utilizador:");
                                    string editUsername = Console.ReadLine();

                                    var checkUserRequest = new CheckUserRequest
                                    {
                                        Username = editUsername
                                    };
                                    var checkUserResponse = await client.CheckUserAsync(
                                        checkUserRequest
                                    );

                                    if (!checkUserResponse.Exists)
                                    {
                                        Console.WriteLine(
                                            $"O utilizador '{editUsername}' não existe."
                                        );
                                        Console.WriteLine("\nPressione Enter");
                                        break;
                                    }

                                    Console.WriteLine("Escolha o que deseja editar:");
                                    Console.WriteLine("1. Senha");
                                    Console.WriteLine(
                                        "2. Tipo de utilizador (1 para admin, 0 para operador)"
                                    );
                                    Console.WriteLine("0. Sair");

                                    string editChoice = Console.ReadLine();

                                    switch (editChoice)
                                    {
                                        case "1":
                                            Console.WriteLine("Insira a nova senha:");
                                            string newPassword = Console.ReadLine();

                                            var editUserRequest = new EditUserRequest
                                            {
                                                Username = editUsername,
                                                NewPassword = newPassword
                                            };

                                            var editUserResponse = await client.EditUserAsync(
                                                editUserRequest
                                            );
                                            Console.WriteLine(editUserResponse.Response);
                                            break;
                                        case "2":
                                            Console.WriteLine(
                                                "Insira o novo tipo de utilizador (1 para admin, 0 para operador):"
                                            );
                                            string userTypeInput = Console.ReadLine();

                                            bool isValidUser = false;
                                            bool isAdmin = false;

                                            while (!isValidUser)
                                            {
                                                if (userTypeInput == "1")
                                                {
                                                    isAdmin = true;
                                                    isValidUser = true;
                                                }
                                                else if (userTypeInput == "0")
                                                {
                                                    isAdmin = false;
                                                    isValidUser = true;
                                                }
                                                else
                                                {
                                                    Console.WriteLine(
                                                        "Tipo de utilizador inválido. Digite 1 para admin ou 0 para operador."
                                                    );
                                                    userTypeInput = Console.ReadLine();
                                                }
                                            }
                                            break;
                                        case "0":
                                            Console.WriteLine(
                                                "Operação de edição do utilizador cancelada."
                                            );
                                            break;
                                        default:
                                            Console.WriteLine("Opção inválida.");
                                            break;
                                    }

                                    Console.WriteLine("\nPressione Enter");
                                    break;

                                case "6":
                                    await channel.ShutdownAsync();

                                    Console.WriteLine("Desconectado do Servidor");
                                    Thread.Sleep(4000);

                                    return;
                                default:
                                    Console.WriteLine(
                                        "Escolha invalida. Por favor, tente novamente."
                                    );
                                    break;
                            }

                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        while (true)
                        {
                            Console.Clear();
                            Console.Write("MENU" + Environment.NewLine);
                            Console.WriteLine("1. Reservar domicilio");
                            Console.WriteLine("2. Ativação");
                            Console.WriteLine("3. Desativação");
                            Console.WriteLine("4. Terminação");
                            Console.WriteLine("5. Ler CSV");
                            Console.WriteLine("6. Exit");
                            string choice = Console.ReadLine();

                            switch (choice)
                            {
                                case "1":
                                    Console.Clear();
                                    Console.WriteLine("Morada do domicilio que deseja reservar: ");
                                    string houseAddress = Console.ReadLine();
                                    Console.WriteLine("Municpio correspondente: ");
                                    string houseCity = Console.ReadLine();
                                    bool validInput = false;
                                    int downstream = 0;
                                    int upstream = 0;
                                    while (!validInput)
                                    {
                                        Console.WriteLine("Insira o valor de Downstream: ");
                                        string down = Console.ReadLine();

                                        Console.WriteLine("Insira o valor de Upstream: ");
                                        string up = Console.ReadLine();

                                        if (int.TryParse(down, out downstream) && int.TryParse(up, out upstream))
                                        {
                                            validInput = true;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Valor inválido. Insira novamente.");
                                        }
                                    }

                                    var reserveDomi = new reservationProcess
                                    {
                                        Username = username,
                                        HouseAddress = houseAddress,
                                        HouseCity = houseCity,
                                        Upstream = upstream,
                                        Downstream = downstream
                                    };
                                    var reserveDomiResp = await client.reservationProcAsync(
                                        reserveDomi
                                    );

                                    if (reserveDomiResp.HasbeenReserved)
                                    {
                                        Console.WriteLine(
                                            "O domicilio correspondente jà foi reservado."
                                        );
                                        Console.WriteLine("\nPrima Enter");
                                        break;
                                    }
                                    else if (reserveDomiResp.ErrorLog != "")
                                    {
                                        Console.WriteLine("O domicilio não existe.");
                                        Console.WriteLine("\nPrima Enter");
                                    }
                                    else
                                    {
                                        Console.WriteLine(
                                            "Para ativar o serviço, use o seguinte ID : "
                                                + reserveDomiResp.UniqueID
                                        );
                                        Console.WriteLine("\nPrima Enter");
                                        Console.ReadLine();
                                    }
                                    break;
                                case "2":
                                    Console.Clear();
                                    Console.WriteLine(
                                        "Antes de inserir o ID único fornecido no processo de reserva verifique : \n"
                                            + "-> Que o domícilio não está ativado; \n"
                                            + "-> Que o seu nome de usário esta associado ao ID único; \n"
                                            + "Caso Contrário esta operação será inútil!!"
                                    );
                                    Console.WriteLine(
                                        "Insira o ID único fornecido no processso de Reserva: "
                                    );
                                    var id_string = Console.ReadLine();
                                    int id = int.Parse(id_string);
                                    var ativaService = new activationProcess
                                    {
                                        UniqueID = id,
                                        Username = username
                                    };
                                    var ativaresponse = await client.activationAsync(ativaService);
                                    Console.Clear();
                                    Console.WriteLine(ativaresponse.Response + Environment.NewLine);
                                    if (ativaresponse.Success)
                                    {
                                        Console.WriteLine(
                                            "Deseja inscrever-se no topico 'EVENTS'? (sim/nao)"
                                        );
                                        string escolha = Console.ReadLine();
                                        if (escolha == "sim") // Waits for a successful message.
                                        {
                                            var factory = new ConnectionFactory
                                            {
                                                HostName = "localhost",
                                                Port = 5672
                                            };
                                            using (var connection = factory.CreateConnection())
                                            using (var _channel = connection.CreateModel())
                                            {
                                                _channel.ExchangeDeclare(
                                                    exchange: "direct_logs",
                                                    type: ExchangeType.Direct
                                                );

                                                // Declare a server-named queue
                                                var queueName = _channel.QueueDeclare().QueueName;

                                                // Bind the queue to the exchange
                                                _channel.QueueBind(
                                                    queue: queueName,
                                                    exchange: "direct_logs",
                                                    routingKey: "EVENTS"
                                                );

                                                // Update and PUBLISH;
                                                var asyncAct = new ActionsProcess
                                                {
                                                    Id = id,
                                                    IsSub = true,
                                                    Type = "ACTIVATED"
                                                };
                                                var asyncActResp = await client.publishUpdtAsync(
                                                    asyncAct
                                                );

                                                if (asyncActResp.ErrorLog != String.Empty) // Error handling
                                                {
                                                    Console.WriteLine(asyncActResp.ErrorLog);
                                                }
                                                else
                                                {
                                                    var consumer = new EventingBasicConsumer(_channel);

                                                    consumer.Received += (model, ea) =>
                                                    {
                                                        var body = ea.Body.ToArray();
                                                        var message = Encoding.UTF8.GetString(body);
                                                        Console.WriteLine($" [x] {message}");
                                                    };

                                                    // Start consuming messages only after setting up the consumer
                                                    _channel.BasicConsume(
                                                        queue: queueName,
                                                        autoAck: true,
                                                        consumer: consumer
                                                    );
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var asyncAct = new ActionsProcess
                                            {
                                                Id = id,
                                                IsSub = false,
                                                Type = "ACTIVATED"
                                            };
                                            var asyncActResp = await client.publishUpdtAsync(asyncAct);
                                            if (asyncActResp.ErrorLog != String.Empty)
                                            {
                                                Console.WriteLine(asyncActResp.ErrorLog);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Domicilio ativado com successo.");
                                            }
                                        }
                                    }

                                    Console.WriteLine("\nPrima Enter");
                                    break;
                                case "3":
                                    Console.Clear();
                                    Console.WriteLine(
                                        "Antes de inserir o ID único fornecido no processo de reserva verifique : \n"
                                            + "-> Que o domícilio está ativado; \n"
                                            + "-> Que o seu nome de usário esta associado ao ID único; \n"
                                            + "Caso Contrário esta operação será inútil!!"
                                    );
                                    Console.WriteLine(
                                        "Insira o ID_único fornecido no processso de Reserva: "
                                    );
                                    string id_stringd = Console.ReadLine();
                                    int id_d = int.Parse(id_stringd);
                                    var desativaService = new deactivationProcess
                                    {
                                        UniqueID = id_d,
                                        Username = username
                                    };
                                    var desativaresponse = await client.deactivationAsync(
                                        desativaService
                                    );
                                    Console.Clear();
                                    Console.WriteLine(
                                        desativaresponse.Response + Environment.NewLine
                                    );
                                    if (desativaresponse.Success)
                                    {
                                        Console.WriteLine(
                                        "Deseja inscrever-se no topico 'EVENTS'? (sim/nao)"
                                    );
                                        string escolha_2 = Console.ReadLine();
                                        if (escolha_2 == "sim") // Waits for a successful message.
                                        {
                                            var factory = new ConnectionFactory
                                            {
                                                HostName = "localhost",
                                                Port = 5672
                                            };
                                            using (var connection = factory.CreateConnection())
                                            using (var _channel = connection.CreateModel())
                                            {
                                                _channel.ExchangeDeclare(
                                                    exchange: "direct_logs",
                                                    type: ExchangeType.Direct
                                                );

                                                // Declare a server-named queue
                                                var queueName = _channel.QueueDeclare().QueueName;

                                                // Bind the queue to the exchange
                                                _channel.QueueBind(
                                                    queue: queueName,
                                                    exchange: "direct_logs",
                                                    routingKey: "EVENTS"
                                                );

                                                // Update and PUBLISH;
                                                var asyncDeact = new ActionsProcess
                                                {
                                                    Id = id_d,
                                                    IsSub = true,
                                                    Type = "DEACTIVATED"
                                                };
                                                var asyncDeactResp = await client.publishUpdtAsync(
                                                    asyncDeact
                                                );

                                                if (asyncDeactResp.ErrorLog != String.Empty) // Error handling
                                                {
                                                    Console.WriteLine(asyncDeactResp.ErrorLog);
                                                }
                                                else
                                                {
                                                    var consumer = new EventingBasicConsumer(_channel);

                                                    consumer.Received += (model, ea) =>
                                                    {
                                                        var body = ea.Body.ToArray();
                                                        var message = Encoding.UTF8.GetString(body);
                                                        Console.WriteLine($" [x] {message}");
                                                    };

                                                    // CONSUME
                                                    _channel.BasicConsume(
                                                        queue: queueName,
                                                        autoAck: true,
                                                        consumer: consumer
                                                    );
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var asyncAct = new ActionsProcess
                                            {
                                                Id = id_d,
                                                IsSub = false,
                                                Type = "DEACTIVATED"
                                            };
                                            var asyncActResp = await client.publishUpdtAsync(asyncAct);

                                            if (asyncActResp.ErrorLog != String.Empty)
                                            {
                                                Console.WriteLine(asyncActResp.ErrorLog);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Domicilio desativado com successo.");
                                            }
                                        }
                                    }

                                    Console.WriteLine("\nPrima Enter");
                                    break;
                                case "4":
                                    Console.Clear();
                                    Console.WriteLine(
                                        "Antes de inserir o ID único fornecido no processo de reserva verifique : \n"
                                            + "-> Que o domícilio está desativado; \n"
                                            + "-> Que o seu nome de usuário está associado ao ID único; \n"
                                            + "Caso contrário, esta operação será inútil!!"
                                    );
                                    Console.WriteLine(
                                        "Insira o ID único fornecido no processo de reserva: "
                                    );
                                    string id_stringt = Console.ReadLine();
                                    int id_t = int.Parse(id_stringt);
                                    var terminationService = new terminationProcess
                                    {
                                        UniqueID = id_t,
                                        Username = username
                                    };
                                    var terminationResponse = await client.terminationAsync(
                                        terminationService
                                    );
                                    Console.Clear();
                                    Console.WriteLine(terminationResponse.Response + Environment.NewLine);
                                    if (terminationResponse.Success)
                                    {
                                        Console.WriteLine(
                                        "Deseja inscrever-se no topico 'EVENTS'? (sim/nao)"
                                    );
                                        string escolha_3 = Console.ReadLine();
                                        if (escolha_3 == "sim")
                                        {
                                            var factory = new ConnectionFactory
                                            {
                                                HostName = "localhost",
                                                Port = 5672
                                            };
                                            using (var connection = factory.CreateConnection())
                                            using (var _channel = connection.CreateModel())
                                            {
                                                _channel.ExchangeDeclare(
                                                    exchange: "direct_logs",
                                                    type: ExchangeType.Direct
                                                );

                                                // Declare a server-named queue
                                                var queueName = _channel.QueueDeclare().QueueName;

                                                // Bind the queue to the exchange
                                                _channel.QueueBind(
                                                    queue: queueName,
                                                    exchange: "direct_logs",
                                                    routingKey: "EVENTS"
                                                );

                                                // Update and PUBLISH;
                                                var asyncTerm = new ActionsProcess
                                                {
                                                    Id = id_t,
                                                    IsSub = true,
                                                    Type = "TERMINATED"
                                                };
                                                var asyncTermResp = await client.publishUpdtAsync(
                                                    asyncTerm
                                                );

                                                if (asyncTermResp.ErrorLog != String.Empty) // Error handling
                                                {
                                                    Console.WriteLine(asyncTermResp.ErrorLog);
                                                }
                                                else
                                                {
                                                    var consumer = new EventingBasicConsumer(_channel);

                                                    consumer.Received += (model, ea) =>
                                                    {
                                                        var body = ea.Body.ToArray();
                                                        var message = Encoding.UTF8.GetString(body);
                                                        Console.WriteLine($" [x] {message}");
                                                    };

                                                    // CONSUME
                                                    _channel.BasicConsume(
                                                        queue: queueName,
                                                        autoAck: true,
                                                        consumer: consumer
                                                    );
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var asyncAct = new ActionsProcess
                                            {
                                                Id = id_t,
                                                IsSub = false,
                                                Type = "TERMINATED"
                                            };
                                            var asyncActResp = await client.publishUpdtAsync(asyncAct);
                                            if (asyncActResp.ErrorLog != String.Empty)
                                            {
                                                Console.WriteLine(asyncActResp.ErrorLog);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Domicilio terminado com successo.");
                                            }
                                        }
                                    }

                                    Console.WriteLine("\nPrima Enter");
                                    break;

                                case "5":
                                    Console.Clear();
                                    Console.WriteLine("Inserir Path do CSV:");
                                    string filePath = Console.ReadLine();

                                    using (StreamReader reader = new StreamReader(filePath))
                                    {
                                        string line;
                                        while ((line = reader.ReadLine()) != null)
                                        {
                                            // Parse the CSV row and create a CSVRow object
                                            string[] fields = line.Split(',');

                                            if (fields.Length >= 4)
                                            {
                                                var readCSV = new CSVProcess
                                                {
                                                    Operadora = fields[0],
                                                    Morada = fields[2],
                                                    Municipio = fields[1],
                                                    Owner = (fields[3] == "1") ? true : false,
                                                    Downstream = Convert.ToInt32(fields[4]),
                                                    Upstream = Convert.ToInt32(fields[5]),
                                                    Username = username
                                                };

                                                // Send the CSVRow to the gRPC server
                                                var readCSVResp = await client.CSVAsync(readCSV);
                                                Console.WriteLine(readCSVResp);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Linha invalida: {line}");
                                            }
                                        }
                                    }

                                    Console.WriteLine("\nPress Enter");
                                    break;
                                case "6":
                                    await channel.ShutdownAsync();

                                    Console.WriteLine("Desconectado do Servidor");
                                    Thread.Sleep(4000);

                                    return;
                            }

                            Console.ReadLine();
                        }
                    }
                }
            } while (loginresult == false);
        }
    }
}


