using System.Net.Mail;

namespace SharpAutomation
{
    /// <summary>
    /// Utility for sending notifications via SMTP.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Sends a notification email using the specified SMTP server and notification configuration.
        /// </summary>
        /// <param name="smtpConfiguration">The SMTP server configuration.</param>
        /// <param name="notificationConfiguration">The notification configuration.</param>
        /// <exception cref="ArgumentException">Thrown when the 'toAddresses' list in notification configuration is empty.</exception>
        public static void Send(SMTPServerConfiguration smtpConfiguration, NotificationConfiguration notificationConfiguration)
        {
            if (notificationConfiguration.ToAddresses.Count == 0)
                throw new ArgumentException("The 'toAddresses' list must not be empty.");

            var message = new MailMessage
            {
                From = new MailAddress(smtpConfiguration.FromAddress),
                Subject = notificationConfiguration.Subject,
                Body = notificationConfiguration.HTMLBody,
                IsBodyHtml = true
            };

            foreach (var replyTo in notificationConfiguration.ReplyTo)
            {
                message.ReplyToList.Add(replyTo);
            }

            foreach (var toAddress in notificationConfiguration.ToAddresses) message.To.Add(toAddress);

            if (notificationConfiguration.CCAddresses != null)
                foreach (var ccAddress in notificationConfiguration.CCAddresses)
                    if (!string.IsNullOrEmpty(ccAddress))
                        message.CC.Add(ccAddress);


            if (notificationConfiguration.Attachments != null)
                foreach (var attachment in notificationConfiguration.Attachments)
                    message.Attachments.Add(new Attachment(attachment));

            new SmtpClient(smtpConfiguration.SMTPServerAddress).Send(message);
        }
    }

    /// <summary>
    /// Configuration for the SMTP server and sender address for sending emails.
    /// </summary>
    public class SMTPServerConfiguration
    {
        /// <summary>
        /// Gets or sets the SMTP server address.
        /// </summary>
        public string SMTPServerAddress { get; set; }

        /// <summary>
        /// Gets or sets the sender's email address.
        /// </summary>
        public string FromAddress { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SMTPServerConfiguration"/> class with specified SMTP server address and sender address.
        /// </summary>
        /// <param name="smtpServerAddress">The SMTP server address.</param>
        /// <param name="fromAddress">The sender's email address.</param>
        public SMTPServerConfiguration(string smtpServerAddress, string fromAddress)
        {
            SMTPServerAddress = smtpServerAddress;
            FromAddress = fromAddress;
        }
    }

    /// <summary>
    /// Configuration for a notification to be sent via email.
    /// </summary>
    public class NotificationConfiguration
    {
        /// <summary>
        /// Gets the list of recipient email addresses.
        /// </summary>
        public List<string> ToAddresses { get; private set; }

        /// <summary>
        /// Gets the subject of the notification email.
        /// </summary>
        public string Subject { get; private set; }

        /// <summary>
        /// Gets or sets the HTML content of the notification email (optional).
        /// </summary>
        public string? HTMLBody { get; private set; } = "";

        /// <summary>
        /// Gets the list of CC (carbon copy) recipient email addresses.
        /// </summary>
        public List<string> CCAddresses { get; private set; } = [];

        /// <summary>
        /// Gets the list of file paths to be attached to the notification email.
        /// </summary>
        public List<string> Attachments { get; private set; } = [];

        /// <summary>
        ///  Gets the list of reply to email addresses.
        /// </summary>
        public List<string> ReplyTo { get; private set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationConfiguration"/> class with specified parameters.
        /// </summary>
        /// <param name="toAddresses">The list of recipient email addresses.</param>
        /// <param name="subject">The subject of the notification email.</param>
        /// <param name="htmlBody">The HTML content of the notification email (optional).</param>
        /// <param name="ccAddresses">The list of CC (carbon copy) recipient email addresses (optional).</param>
        /// <param name="attachments">The list of file paths to be attached to the notification email (optional).</param>
        /// <param name="replyTo">The list of reply to email addresses (optional).</param>
        public NotificationConfiguration(List<string> toAddresses, string subject, string htmlBody, List<string>? ccAddresses = null, List<string>? attachments = null, List<string>? replyTo = null)
        {
            ToAddresses = toAddresses;
            Subject = subject;
            HTMLBody = htmlBody;
            CCAddresses = ccAddresses ?? [];
            Attachments = attachments ?? [];
            ReplyTo = replyTo ?? [];
        }
    }

    /// <summary>
    /// Extension methods for sending notifications using the provided notification configuration and SMTP server configuration.
    /// </summary>
    public static class NotificationConfigurationExtensions
    {
        /// <summary>
        /// Sends a notification using the provided notification configuration and SMTP server configuration.
        /// </summary>
        /// <param name="notificationConfiguration">The notification configuration.</param>
        /// <param name="smtpConfiguration">The SMTP server configuration.</param>
        public static void SendNotification(this NotificationConfiguration notificationConfiguration, SMTPServerConfiguration smtpConfiguration)
        {
            Notification.Send(smtpConfiguration, notificationConfiguration);
        }
    }
}
