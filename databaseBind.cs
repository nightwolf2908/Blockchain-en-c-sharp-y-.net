using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

public class DatabaseBind
{
    private readonly string _connectionStringMaster = "Server=localhost,1433;User Id=sa;Password=MiContraseñaSegura123!;Encrypt=False;";
    private readonly string _connectionStringBlockchain = "Server=localhost,1433;Database=BlockchainAuth;User Id=sa;Password=MiContraseñaSegura123!;Encrypt=False;";

    public DatabaseBind()
    {
        InicializarBaseDeDatos();
    }

    private void InicializarBaseDeDatos()
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(_connectionStringMaster))
            {
                connection.Open();
                string scriptBD = @"
                    IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BlockchainAuth')
                    BEGIN
                        CREATE DATABASE BlockchainAuth;
                    END";

                    using (SqlCommand command = new SqlCommand(scriptBD, connection))
                    {
                        command.ExecuteNonQuery();
                    }
            }

            using (SqlConnection connection = new SqlConnection(_connectionStringBlockchain))
            {
                connection.Open();
                string scriptTablaUsuarios = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Usuarios]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE Usuarios (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Email VARCHAR(150) UNIQUE NOT NULL,
                            PasswordHash CHAR(64) NOT NULL,
                            FechaRegistro DATETIME DEFAULT GETDATE()
                        );
                    END";

                using (SqlCommand command = new SqlCommand(scriptTablaUsuarios, connection))
                {
                    command.ExecuteNonQuery();
                }

        }}   
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error DB Inicialización] Asegúrate de que Docker esté encendido: {ex.Message}");
            Console.ResetColor();
        }
    }

    public bool GuardarUsuarioEnLaBaseDeDatos(string email, string passwordHash)
    {
        try
        {
            using(SqlConnection connection = new SqlConnection(_connectionStringBlockchain))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Usuarios (Email, PasswordHash) VALUES (@Email, @PasswordHash)";
                using(SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Email", email.Trim().ToLower());
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    command.ExecuteNonQuery();
                    return true;
                }
            }
        }
        catch(SqlException sqlEx) when (sqlEx.Number == 2627)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Error DB] El usuario con email '{email}' ya existe en la base de datos.");
            Console.ResetColor();
            return false;
        } //codigo de error para llave duplicada
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error DB] No se pudo guardar el usuario: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    public bool ValidarLoginEnLaBaseDeDatos(string email, string passwordHash)
    {
        try
        {
            using(SqlConnection connection = new SqlConnection(_connectionStringBlockchain))
            {
                connection.Open();
                string query = "SELECT PasswordHash FROM Usuarios WHERE Email = @Email";
                using(SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email.Trim().ToLower());

                    using(SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hashGuardado = reader["PasswordHash"].ToString();
                            return hashGuardado == passwordHash;
                        }
                    }
                }
            }
            return false;
        }
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error DB] No se pudo validar el login: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

}