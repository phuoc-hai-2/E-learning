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
            emailToSend.From.Add(MailboxAddress.Parse("YOUR_EMAIL@gmail.com")); // Điền email của bạn
            emailToSend.To.Add(MailboxAddress.Parse(email));
            emailToSend.Subject = subject;
            emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlMessage };

            using (var client = new SmtpClient())
            {
                // Kết nối tới Gmail SMTP
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                // Đăng nhập (Lưu ý: Phải dùng "App Password" của Gmail, không phải mật khẩu thường)
                await client.AuthenticateAsync("YOUR_EMAIL@gmail.com", "YOUR_APP_PASSWORD");

                await client.SendAsync(emailToSend);
                await client.DisconnectAsync(true);
            }
        }
    }
}