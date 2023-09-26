using System.Net.Mail;

namespace SharpAutomation
{
    /// <summary>
    /// Utility for sending notifications via SMTP.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Sends a notification email using the specified SMTP server and other email parameters.
        /// </summary>
        /// <param name="smtpServerAddress">The SMTP server address for sending the email.</param>
        /// <param name="fromAddress">The sender's email address.</param>
        /// <param name="toAddresses">A list of recipient email addresses.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="htmlBody">The HTML content of the email body (optional).</param>
        /// <param name="ccAddresses">A list of CC (carbon copy) recipient email addresses (optional).</param>
        /// <param name="attachments">A list of file paths to be attached to the email (optional).</param>
        /// <exception cref="ArgumentException">Thrown when the 'toAddresses' list is empty.</exception>
        public static void Send(string smtpServerAddress, string fromAddress, List<string> toAddresses, string subject, string htmlBody = "", List<string>? ccAddresses = null, List<string>? attachments = null)
        {
            if (toAddresses.Count == 0)
                throw new ArgumentException("The 'toAddresses' list must not be empty.");

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            foreach (var toAddress in toAddresses) message.To.Add(toAddress);

            if (ccAddresses != null)
                foreach (var ccAddress in ccAddresses)
                    if (!string.IsNullOrEmpty(ccAddress))
                        message.CC.Add(ccAddress);


            if (attachments != null)
                foreach (var attachment in attachments)
                    message.Attachments.Add(new Attachment(attachment));

            new SmtpClient(smtpServerAddress).Send(message);
        }
    }
}
