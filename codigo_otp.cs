using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class CodigoOTP
{
    private string _codigoGenerado = "";
    private DateTime _tiempoExpiracion;

    private readonly string _correoRemitente = "abdielitopro4800s@gmail.com";
    private readonly string _contrasenaRemitente = "xwuw finz qowx cdtf";

    public string GenerarCodigoVerificacion()
    {
        Random random = new Random();
        _codigoGenerado = random.Next(100000,999999).ToString();
        _tiempoExpiracion = DateTime.Now.AddMinutes(5);

        return _codigoGenerado;
    }

    public bool validarCodigo(string codigoIngresado)
    {
        if(DateTime.Now > _tiempoExpiracion)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("El código ha expirado. Por favor, genera un nuevo código.");
            Console.ResetColor();
            return false;
        }
        if(codigoIngresado == _codigoGenerado)
        {
            return true;
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("El código ingresado es incorrecto. Por favor, inténtalo de nuevo.");
        Console.ResetColor();
        return false;

    }

    public async Task<bool> EnviarCorreoReal(string correoDestino, string codigo)
    {
        try
        {
            MailMessage mensaje = new MailMessage();
            mensaje.From = new MailAddress(_correoRemitente, "Nodo Blockchain Auth");
            mensaje.To.Add(correoDestino);
            mensaje.Subject = "Código de verificación - Registro de Nodo";
            mensaje.Body = $@"
                <h3>Bienvenido a la red Blockchain</h3>
                <p>Estás intentando registrar un nuevo usuario desde la consola del nodo.</p>
                <p>Tu código de acceso secreto es: <strong style='font-size: 18px; color: #007bff;'>{codigo}</strong></p>
                <p>Este código expira de forma automática en <strong>5 minutos</strong>.</p>
                <br>
                <small>Si no solicitaste este código, puedes ignorar este mensaje de seguridad.</small>";
            mensaje.IsBodyHtml = true;

            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential(_correoRemitente, _contrasenaRemitente);
                smtp.EnableSsl = true;

                await smtp.SendMailAsync(mensaje);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error al enviar el correo: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

}