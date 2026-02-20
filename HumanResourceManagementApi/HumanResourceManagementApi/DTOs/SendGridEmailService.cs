// Core.HumanResourceManagementApi.DTOs/SendGridEmailService.cs
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Core.HumanResourceManagementApi.DTOs
{
    public class SendGridEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _sendGridApiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public SendGridEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _sendGridApiKey = _configuration["SendGridSettings:ApiKey"] ?? throw new ArgumentNullException("SendGridSettings:ApiKey not found in configuration.");
            _senderEmail = _configuration["SendGridSettings:SenderEmail"] ?? throw new ArgumentNullException("SendGridSettings:SenderEmail not found in configuration.");
            _senderName = _configuration["SendGridSettings:SenderName"] ?? "System";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                Console.WriteLine($"Email sent to {toEmail} with status code: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"SendGrid error response: {body}");
                    // Optionally, throw an exception or log a more severe error
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email via SendGrid: {ex.Message}");
                // Log the exception
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            string subject = "Password Reset Request";
            string frontendResetUrl = _configuration["FrontendSettings:ResetPasswordUrl"] ?? "http://localhost:5000/enter-token";
            string resetLink = $"{frontendResetUrl}?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";

            string plainTextContent = $"Hello,\n\nYou have requested a password reset. Please use the following token to reset your password: {resetToken}\n\nOr click on this link: {resetLink}\n\nThis token will expire in 1 hour.\n\nIf you did not request this, please ignore this email.\n\nRegards,\nPSL DEV TEAM";
            string htmlContent = $@"
                <p>Hello,</p>
                <p>You have requested a password reset. Please use the following token to reset your password:</p>
                <h3><strong>{resetToken}</strong></h3>
                <p>Or click on the link below to proceed:</p>
                <p><a href='{resetLink}'>Reset Your Password</a></p>
                <p>This token will expire in 1 hour.</p>
                <p>If you did not request this, please ignore this email.</p>
                <p>Regards,<br/>PSL DEV TEAM</p>
            ";

            await SendEmailAsync(toEmail, subject, plainTextContent, htmlContent);
        }
    }
}