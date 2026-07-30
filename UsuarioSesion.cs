using System;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
public class UsuarioSesion
    {
        public string Email { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;

        private readonly string _connectionStringBlockchain = "Server=localhost,1433;Database=BlockchainAuth;User Id=sa;Password=MiContraseñaSegura123!;Encrypt=False;";

        public UsuarioSesion? ObtenerDatosSesion(string email)
    {
        try
        {
            using(SqlConnection connection = new SqlConnection(_connectionStringBlockchain))
            {
                connection.Open();
                string query = "SELECT Email, PublicKey, PrivateKey FROM Usuarios WHERE Email = @Email";
                
                using(SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email.Trim().ToLower());
                    using(SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UsuarioSesion
                            {
                                Email = reader["Email"].ToString()!,
                                PublicKey = reader["PublicKey"].ToString()!,
                                PrivateKey = reader["PrivateKey"].ToString()!
                            };
                        }
                    }
                }
            }
        }
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error DB] No se pudo obtener los datos de sesión: {ex.Message}");
            Console.ResetColor();
        }
        return null;
    }
    }

    