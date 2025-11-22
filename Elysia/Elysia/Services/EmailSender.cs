using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Elysia.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailToSend = new MimeMessage();
            emailToSend.From.Add(MailboxAddress.Parse("daophuochai2004@gmail.com")); // <-- Email thật của bạn
            emailToSend.To.Add(MailboxAddress.Parse(email));
            emailToSend.Subject = subject;
            emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlMessage };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                // QUAN TRỌNG: Dùng App Password ở đây
                await client.AuthenticateAsync("daophuochai2004@gmail.com", "kqvz vfoz vtcc ukun");

                await client.SendAsync(emailToSend);
                await client.DisconnectAsync(true);
            }
        }
    }
}