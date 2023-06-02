using Grpc.Core;
using System.Data;
using System.Data.SqlClient;
using RabbitMQ.Client;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;

namespace GrpcServer.Services
{
    public class OperadorService : Operador.OperadorBase
    {

        private readonly ILogger<OperadorService> _logger;
        private readonly string connectionString = "Server=(localdb)\\mssqllocaldb;Database=TP2;Trusted_Connection=True;MultipleActiveResultSets=true";
        public OperadorService(ILogger<OperadorService> looger)
        {
            _logger = looger;
        }

        public override Task<ResponseLoginUser> LoginUser(
        LoginUserModel request, ServerCallContext context)
        {
            ResponseLoginUser response = new ResponseLoginUser();
            bool userType = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Execute uma consulta SQL que retorna o tipo de usuário correspondente ao nome de usuário e senha fornecidos
                    string query = "SELECT Type FROM [User] WHERE Username = @Username AND Password = @Password";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", request.Username);
                        command.Parameters.AddWithValue("@Password", request.Password);

                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.HasRows)
                        {
                            // Se a consulta retornar um registro, define a saudação com base no valor do campo Tipo
                            reader.Read();
                            userType = (bool)reader["Type"];

                            switch (userType)
                            {
                                case true:
                                    response.Response = $"Bem-vindo Admin {request.Username}!";
                                    response.UserFlag = true;
                                    response.Result = true;
                                    break;

                                case false:
                                    response.Response = $"Bem-vindo Operador {request.Username}!";
                                    response.UserFlag = false;
                                    response.Result = true;
                                    break;
                            }
                        }
                        else
                        {
                            response.Response = "Credenciais inválidas.";
                            response.Result = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tentar conectar à base de dados.");
                response.Response = "Ocorreu um erro ao tentar conectar à base de dados.";
                response.Result = false;
            }

            return Task.FromResult(response);
        }

        public override Task<reservationProcessResponse> reservationProc(
        reservationProcess request, ServerCallContext context)
        {
            reservationProcessResponse response = new reservationProcessResponse();
            response.HasbeenReserved = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query_check = "SELECT COUNT(*) FROM [Domicilio] WHERE Morada = @houseAddress AND Municipio = @houseCity AND Downstream = @downstream AND Upstream = @upstream";
                    string query_reserved = "SELECT Estado, ID_Admin FROM [Domicilio] WHERE Morada = @houseAddress AND Municipio = @houseCity AND Downstream = @downstream AND Upstream = @upstream";
                    using (SqlCommand command = new SqlCommand(query_check, connection))
                    {
                        command.Parameters.AddWithValue("@houseAddress", request.HouseAddress);
                        command.Parameters.AddWithValue("@houseCity", request.HouseCity);
                        command.Parameters.AddWithValue("@upstream", request.Upstream);
                        command.Parameters.AddWithValue("@downstream", request.Downstream);
                        int count = (int)command.ExecuteScalar();

                        if (count > 0) //Existe domicilio;
                        {
                            using (SqlCommand commands = new SqlCommand(query_reserved, connection))
                            {
                                commands.Parameters.AddWithValue("@houseAddress", request.HouseAddress);
                                commands.Parameters.AddWithValue("@houseCity", request.HouseCity);
                                commands.Parameters.AddWithValue("@upstream", request.Upstream);
                                commands.Parameters.AddWithValue("@downstream", request.Downstream);

                                using (SqlDataReader reader = commands.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        string stateDom = reader["Estado"].ToString();
                                        int adminId = Convert.ToInt32(reader["ID_Admin"]);

                                        if (stateDom == "") // Não foi reservado..;
                                        {
                                            string updateQuery = "UPDATE [Domicilio] SET Estado = 'RESERVED', User_name = @user WHERE Morada = @houseAddress AND Municipio = @houseCity";
                                            using (
                                            SqlCommand updateCommand = new SqlCommand(
                                            updateQuery, connection))
                                            {
                                                updateCommand.Parameters.AddWithValue("@user", request.Username);
                                                updateCommand.Parameters.AddWithValue("@houseAddress", request.HouseAddress);
                                                updateCommand.Parameters.AddWithValue("@houseCity", request.HouseCity);

                                                int rowsAffected = updateCommand.ExecuteNonQuery();

                                                if (rowsAffected > 0)
                                                {
                                                    response.UniqueID = adminId;
                                                }
                                                else
                                                {
                                                    response.ErrorLog = "Não foi possivel modificar o estado do domicilio.";
                                                }
                                            }
                                        }
                                        else if (stateDom == "RESERVED")
                                        {
                                            response.HasbeenReserved = true;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            response.ErrorLog = "O domicilio não existe.";
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tentar conectar à base de dados.");
                response.ErrorLog = "Erro ao tentar conectar à base de dados.";
            }

            return Task.FromResult(response);
        }

        public override async Task<activationProcessResponse> activation(
        activationProcess request, ServerCallContext context)
        {
            activationProcessResponse response = new activationProcessResponse();

            string query = "SELECT COUNT(*) FROM [Domicilio] WHERE User_name = @Username AND ID_Admin = @Id ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", request.Username);
                command.Parameters.AddWithValue("@Id", request.UniqueID);

                int count = (int)command.ExecuteScalar();

                if (count > 0)
                {
                    string checkQuery = "SELECT Estado FROM [Domicilio] WHERE User_name = @Username AND ID_Admin = @Id";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", request.Username);
                        checkCommand.Parameters.AddWithValue("@Id", request.UniqueID);
                        using (SqlDataReader reader = checkCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string stateDom = reader["Estado"].ToString();

                                if (stateDom == "RESERVED" || stateDom == "DEACTIVATED")
                                {
                                    response.Success = true;
                                    // Aqui respondemos de forma sincrona, se é possivel e o tempo estimado.
                                    response.Response = "Ação de ativação possivel para o ID " + request.UniqueID + Environment.NewLine + "Tempo estimado: 10 segundos";
                                }
                                else
                                {
                                    response.Success = false;
                                    response.Response = "Ação impossivel, estado do domicilio: " + stateDom;

                                }
                            }
                        }
                    }
                }
                else
                {
                    response.Success = false;
                    response.Response = "Não existe domicilio com ID #" + request.UniqueID + " reservado por " + request.Username;
                }
                connection.Close();
            }
            return response;
        }

        public override Task<deactivationProcessResponse> deactivation(
        deactivationProcess request, ServerCallContext context)
        {
            deactivationProcessResponse response = new deactivationProcessResponse();

            string query = "SELECT COUNT(*) FROM [Domicilio] WHERE User_name = @Username AND ID_Admin = @Id ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", request.Username);
                command.Parameters.AddWithValue("@Id", request.UniqueID);

                int count = (int)command.ExecuteScalar();

                if (count > 0)
                {
                    string checkQuery = "SELECT Estado FROM [Domicilio] WHERE User_name = @Username AND ID_Admin = @Id";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", request.Username);
                        checkCommand.Parameters.AddWithValue("@Id", request.UniqueID);
                        using (SqlDataReader reader = checkCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string stateDom = reader["Estado"].ToString();

                                if (stateDom == "ACTIVATED")
                                {
                                    response.Success = true;
                                    response.Response = "Ação de desativação possivel para o ID " + request.UniqueID + Environment.NewLine + "Tempo estimado: 10 segundos";
                                }
                                else
                                {
                                    response.Success = false;
                                    response.Response = "Ação impossivel, estado do domicilio: " + stateDom;
                                }
                            }
                        }
                    }
                }
                else
                {
                    response.Success = false;
                    response.Response = "Não existe domicilio com ID #" + request.UniqueID + " E ativado por " + request.Username;
                }
                connection.Close();
            }
            return Task.FromResult(response);
        }

        public override Task<terminationProcessResponse> termination(
        terminationProcess request, ServerCallContext context)
        {
            terminationProcessResponse response = new terminationProcessResponse();

            string query = "SELECT COUNT(*) FROM [Domicilio] WHERE User_name = @Username AND ID_Admin = @Id ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", request.Username);
                command.Parameters.AddWithValue("@Id", request.UniqueID);

                int count = (int)command.ExecuteScalar();

                if (count > 0)
                {
                    string checkQuery = "SELECT Estado FROM [Domicilio] WHERE User_name = @Username AND ID_Admin = @Id";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", request.Username);
                        checkCommand.Parameters.AddWithValue("@Id", request.UniqueID);
                        using (SqlDataReader reader = checkCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string stateDom = reader["Estado"].ToString();

                                if (stateDom == "DEACTIVATED")
                                {
                                    response.Success = true;
                                    response.Response = "Ação de desativação possivel para o ID " + request.UniqueID + Environment.NewLine + "Tempo estimado: 10 segundos";
                                }
                                else
                                {
                                    response.Success = false;
                                    response.Response = "Ação impossivel, estado do domicilio: " + stateDom;
                                }
                            }
                        }
                    }
                }
                else
                {
                    response.Success = false;
                    response.Response = "Não existe domicilio com ID #" + request.UniqueID + " E desativado por " + request.Username;
                }
                connection.Close();
            }
            return Task.FromResult(response);
        }

        public override Task<ActionsProcessResponse> publishUpdt(
        ActionsProcess request, ServerCallContext context)
        {
            ActionsProcessResponse response = new ActionsProcessResponse();
            response.ErrorLog = string.Empty;

            if (request.IsSub)
            {
                try
                {
                    updateDomicilio(request.Id, request.Type);
                    Thread.Sleep(10000);
                    publishSuccess(request.Type);
                }
                catch (Exception ex)
                {
                    response.ErrorLog = ex.Message;
                }
            }
            else
            {
                try
                {
                    updateDomicilio(request.Id, request.Type);
                }
                catch (Exception ex)
                {
                    response.ErrorLog = ex.Message;
                }
            }

            return Task.FromResult(response);
        }

        private void publishSuccess(string changedState)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    Port = 5672
                };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: "direct_logs", type: ExchangeType.Direct);
                    var message = Encoding.UTF8.GetBytes("Domicílio " + changedState + " com sucesso.");

                    channel.BasicPublish(
                    exchange: "direct_logs", routingKey: "EVENTS", basicProperties: null, body: message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro na publicação: " + ex.Message);
                throw;
            }
        }

        private void updateDomicilio(int uniqueID, string newState)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE [Domicilio] SET Estado = @NewState WHERE ID_Admin = @Id";
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Id", uniqueID);
                        if (newState == "TERMINATED")
                        {
                            updateCommand.Parameters.AddWithValue("@NewState", DBNull.Value);
                        }
                        else
                        {
                            updateCommand.Parameters.AddWithValue("@NewState", newState);
                        }

                        int rowsAffected = updateCommand.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            throw new Exception("Erro na modificação do estado.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public override async Task Listing(
        ListingProcess request, IServerStreamWriter<ListingProcessModel> responseStream, ServerCallContext context)
        {
            List<ListingProcessModel> list = new List<ListingProcessModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Domicilio WHERE Estado IS NULL";

                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    ListingProcessModel domicilio = new ListingProcessModel
                    {
                        IdUnico = Convert.ToInt32(reader["ID_Admin"]),
                        Operadora = reader["Operadora"].ToString(),
                        Morada = reader["Morada"].ToString(),
                        Municipio = reader["Municipio"].ToString(),
                        Owner = Convert.ToBoolean(reader["Owner"]),
                        State = reader["Estado"].ToString(),
                        Operador = reader["User_name"].ToString(),
                        Downstream = Convert.ToInt32(reader["Downstream"]),
                        Upstream = Convert.ToInt32(reader["Upstream"])
                    };

                    domicilio.Operador = "Sem Operador ou Terminado Recentemente";
                    domicilio.State = "Sem Estado";

                    list.Add(domicilio);
                }

                reader.Close();

                foreach (var vardomicilio in list)
                {
                    await responseStream.WriteAsync(vardomicilio);
                }
            }
        }

        public override async Task<ProcessCSVResponse> CSV(
        CSVProcess request, ServerCallContext context)
        {
            var response = new ProcessCSVResponse();
            response.Response = string.Empty;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string checkQuery = "SELECT COUNT(*) FROM Domicilio WHERE Morada = @Morada AND Municipio = @Municipio AND Downstream = @Downstream AND Upstream = @Upstream";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Morada", request.Morada);
                        checkCommand.Parameters.AddWithValue("@Municipio", request.Municipio);
                        checkCommand.Parameters.AddWithValue("@Downstream", request.Downstream);
                        checkCommand.Parameters.AddWithValue("@Upstream", request.Upstream);

                        int count = (int)await checkCommand.ExecuteScalarAsync();

                        if (count > 0)
                        {
                            response.Response = $"Morada: '{request.Morada}', '{request.Municipio}' já existe na base de dados.";
                        }
                        else
                        {
                            string insertQuery = @"INSERT INTO [dbo].[Domicilio] ([Operadora], [Morada], [Municipio], [Owner], [User_name], [Downstream],[Upstream])
                                   VALUES (@Operadora, @Morada, @Municipio, @Owner, @UserName, @Downstream, @Upstream)";
                            using (
                            SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@Operadora", request.Operadora);
                                insertCommand.Parameters.AddWithValue("@Morada", request.Morada);
                                insertCommand.Parameters.AddWithValue("@Municipio", request.Municipio);
                                insertCommand.Parameters.AddWithValue("@Owner", request.Owner);
                                insertCommand.Parameters.AddWithValue("@UserName", DBNull.Value);
                                insertCommand.Parameters.AddWithValue("@Downstream", request.Downstream);
                                insertCommand.Parameters.AddWithValue("@Upstream", request.Upstream);

                                int rowsAffected = await insertCommand.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                    response.Response = $"Morada: '{request.Morada}', '{request.Municipio}' inserida na base de dados.";
                                }
                                else
                                {
                                    response.Response = $"Erro ao inserir Morada: '{request.Morada}', '{request.Municipio}' ";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar linha");
                response.Response = "Erro ao processar Linha.";
            }

            return response;
        }
        public override async Task ListingActive(
        ListingProcess request, IServerStreamWriter<ListingProcessModel> responseStream, ServerCallContext context)
        {
            List<ListingProcessModel> list = new List<ListingProcessModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Domicilio WHERE Estado IS NOT NULL AND User_name IS NOT NULL";

                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    ListingProcessModel domicilio = new ListingProcessModel
                    {
                        IdUnico = Convert.ToInt32(reader["ID_Admin"]),
                        Operadora = reader["Operadora"].ToString(),
                        Morada = reader["Morada"].ToString(),
                        Municipio = reader["Municipio"].ToString(),
                        Owner = Convert.ToBoolean(reader["Owner"]),
                        State = reader["Estado"].ToString(),
                        Operador = reader["User_name"].ToString(),
                        Downstream = Convert.ToInt32(reader["Downstream"]),
                        Upstream = Convert.ToInt32(reader["Upstream"])
                    };

                    list.Add(domicilio);
                }

                reader.Close();

                foreach (var vardomicilio in list)
                {
                    await responseStream.WriteAsync(vardomicilio);
                }
            }
        }
        public override Task<ResponseCreateUser> CreateUser(
        CreateUserModel request, ServerCallContext context)
        {
            ResponseCreateUser response = new ResponseCreateUser();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Verificar se user já existe
                    string checkQuery = "SELECT COUNT(*) FROM [User] WHERE Username = @Username";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", request.Username);

                        int count = (int)checkCommand.ExecuteScalar();

                        if (count > 0)
                        {
                            response.Response = $"Username: '{request.Username}' já existe.";
                        }
                        else
                        {
                            // Inserir novo user
                            string insertQuery = "INSERT INTO [User] (Username, Password, Type) VALUES (@Username, @Password, @Type)";
                            using (
                            SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@Username", request.Username);
                                insertCommand.Parameters.AddWithValue("@Password", request.Password);
                                insertCommand.Parameters.AddWithValue("@Type", request.Type);

                                int rowsAffected = insertCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    response.Response = $"User: '{request.Username}' criado com sucesso";
                                }
                                else
                                {
                                    response.Response = $"Erro ao criar user '{request.Username}'";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com a base de dados");
                response.Response = "Ocorreu um erro ao conectar à base de dados";
            }

            return Task.FromResult(response);
        }

        public override Task<ResponseRemoveUser> RemoveUser(
        RemoveUserRequest request, ServerCallContext context)
        {
            var response = new ResponseRemoveUser();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM [User] WHERE Username = @Username";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", request.Username);
                        int userCount = (int)checkCommand.ExecuteScalar();

                        if (userCount > 0)
                        {
                            string removeQuery = "DELETE FROM [User] WHERE Username = @Username";
                            using (
                            SqlCommand removeCommand = new SqlCommand(removeQuery, connection))
                            {
                                removeCommand.Parameters.AddWithValue("@Username", request.Username);
                                int rowsAffected = removeCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    response.Response = $"O utilizador '{request.Username}' foi removido com sucesso.";
                                }
                                else
                                {
                                    response.Response = $"Falha ao remover o utilizador '{request.Username}'.";
                                }
                            }
                        }
                        else
                        {
                            response.Response = $"O utilizador '{request.Username}' não existe.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover o utilizador.");
                response.Response = "Ocorreu um erro ao remover o utilizador.";
            }

            return Task.FromResult(response);
        }

        public override Task<ResponseEditUser> EditUser(
        EditUserRequest request, ServerCallContext context)
        {
            ResponseEditUser response = new ResponseEditUser();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM [User] WHERE Username = @Username";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", request.Username);
                        int count = (int)checkCommand.ExecuteScalar();

                        if (count == 0)
                        {
                            response.Response = $"O utilizador '{request.Username}' não existe.";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(request.NewPassword))
                            {
                                string updatePasswordQuery = "UPDATE [User] SET Password = @NewPassword WHERE Username = @Username";
                                using (
                                SqlCommand updatePasswordCommand = new SqlCommand(
                                updatePasswordQuery, connection))
                                {
                                    updatePasswordCommand.Parameters.AddWithValue("@NewPassword", request.NewPassword);
                                    updatePasswordCommand.Parameters.AddWithValue("@Username", request.Username);
                                    int rowsAffected = updatePasswordCommand.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        response.Response += "A senha do utilizador foi atualizada com sucesso. ";
                                    }
                                    else
                                    {
                                        response.Response += "Falha ao atualizar a senha do utilizador. ";
                                    }
                                }
                            }

                            if (request.IsAdmin)
                            {
                                string updateAdminQuery = "UPDATE [User] SET Type = 1 WHERE Username = @Username";
                                using (
                                SqlCommand updateAdminCommand = new SqlCommand(
                                updateAdminQuery, connection))
                                {
                                    updateAdminCommand.Parameters.AddWithValue("@Username", request.Username);
                                    int rowsAffected = updateAdminCommand.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        response.Response += "O utilizador foi definido como administrador com sucesso.";
                                    }
                                    else
                                    {
                                        response.Response += "Falha ao definir o utilizador como administrador.";
                                    }
                                }
                            }
                            else
                            {
                                string updateAdminQuery = "UPDATE [User] SET Type = 0 WHERE Username = @Username";
                                using (
                                SqlCommand updateAdminCommand = new SqlCommand(
                                updateAdminQuery, connection))
                                {
                                    updateAdminCommand.Parameters.AddWithValue("@Username", request.Username);
                                    int rowsAffected = updateAdminCommand.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        response.Response += "O utilizador foi definido como operador com sucesso.";
                                    }
                                    else
                                    {
                                        response.Response += "Falha ao definir o utilizador como operador.";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar o utilizador.");
                response.Response = "Ocorreu um erro ao editar o utilizador.";
            }

            return Task.FromResult(response);
        }

        public override Task<CheckUserResponse> CheckUser(
        CheckUserRequest request, ServerCallContext context)
        {
            CheckUserResponse response = new CheckUserResponse();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM [User] WHERE Username = @Username";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", request.Username);

                        int count = (int)checkCommand.ExecuteScalar();
                        response.Exists = count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com a base de dados");
                response.Exists = false;
            }

            return Task.FromResult(response);
        }
    }
}