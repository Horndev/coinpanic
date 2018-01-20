using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace coinpanic_airdrop.Services
{
    public class MailingService
    {
        public static class MonitoringService
        {
            public static void SendMessage(string subject, string messagetx)
            {
                var emailhost = System.Configuration.ConfigurationManager.AppSettings["EmailSMTPHost"];
                var emailport = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["EmailSMTPPort"]);
                var emailuser = System.Configuration.ConfigurationManager.AppSettings["EmailUser"];
                var emailpass = System.Configuration.ConfigurationManager.AppSettings["EmailPass"];

                var message = new MailMessage();
                message.To.Add(new MailAddress("claims@coinpanic.com"));
                message.From = new MailAddress(emailuser);  // replace with valid value
                message.Subject = "Monitoring Message: " + subject;
                message.Body = messagetx;
                message.IsBodyHtml = false;

                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = emailuser,
                        Password = emailpass
                    };
                    smtp.Credentials = credential;
                    smtp.Host = emailhost;
                    smtp.Port = emailport;
                    smtp.EnableSsl = false;
                    smtp.Send(message);
                }
            }
        }
    }
}