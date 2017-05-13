using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Threading.Tasks;
using Lightwork.Core;

namespace Lightwork.Data
{
    public class EmailWorkflow : Workflow
    {
        public EmailWorkflow()
        {
        }

        public EmailWorkflow(string smtpClientUrl, string from, string to, string subject, string body)
        {
            SmtpClientUrl = new Argument<string>("SmtpClientUrl", smtpClientUrl);
            FromAddress = new Argument<string>("FromAddress", from);
            To = new Argument<ICollection<string>>("To", new Collection<string> { to });
            Subject = new Argument<string>("Subject", subject);
            Body = new Argument<string>("Body", body);
        }

        public Argument<string> SmtpClientUrl { get; set; }

        public Argument<string> FromAddress { get; set; }

        public Argument<string> FromName { get; set; }

        public Argument<ICollection<string>> To { get; set; }

        public Argument<ICollection<string>> Cc { get; set; }

        public Argument<ICollection<string>> Bcc { get; set; }

        public Argument<string> Subject { get; set; }

        public Argument<string> Body { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            var message = new MailMessage
            {
                From = new MailAddress(FromAddress.Value, FromName.Value),
                Subject = Subject.Value,
                Body = Body.Value
            };

            foreach (var to in To.Value)
            {
                message.To.Add(to);
            }

            foreach (var cc in Cc.Value)
            {
                message.CC.Add(cc);
            }

            foreach (var bcc in Bcc.Value)
            {
                message.Bcc.Add(bcc);
            }

            using (var client = new SmtpClient(SmtpClientUrl.Value))
            {
                await client.SendMailAsync(message);
            }
        }
    }
}
