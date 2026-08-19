using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using MailKit.Security;
using MimeKit;
using System.Text.RegularExpressions;

namespace EmailApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string myEmail;
            do
            {
                Console.Write("Enter your Gmail address: ");
                myEmail = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(myEmail) || !Regex.IsMatch(myEmail ?? "", @"^[A-Za-z0-9._%+-]+@gmail\.com$"))
                {
                    Console.WriteLine("Please enter a valid Gmail address.");
                    myEmail = null;
                }

            } while (myEmail == null);


            string clientId = Environment.GetEnvironmentVariable("EMAIL_CLIENT_ID", EnvironmentVariableTarget.User);
            string clientSecret = Environment.GetEnvironmentVariable("EMAIL_CLIENT_SECRET", EnvironmentVariableTarget.User);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                Console.Write("Enter Google Client ID: ");
                clientId = Console.ReadLine()?.Trim();

                while (string.IsNullOrWhiteSpace(clientId))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    Console.Write("Enter Google Client ID: ");
                    clientId = Console.ReadLine()?.Trim();

                }

                Environment.SetEnvironmentVariable("EMAIL_CLIENT_ID", clientId, EnvironmentVariableTarget.User);
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                Console.Write("Enter Google Client Secret: ");
                clientSecret = Console.ReadLine()?.Trim();

                while (string.IsNullOrWhiteSpace(clientSecret))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    Console.Write("Enter Google Client secret: ");
                    clientSecret = Console.ReadLine()?.Trim();

                }

                Environment.SetEnvironmentVariable("EMAIL_CLIENT_SECRET", clientSecret, EnvironmentVariableTarget.User);
            }

            var clientSecrets = new ClientSecrets
            {
                ClientId = Environment.GetEnvironmentVariable("EMAIL_CLIENT_ID", EnvironmentVariableTarget.User),

                ClientSecret = Environment.GetEnvironmentVariable("EMAIL_CLIENT_SECRET", EnvironmentVariableTarget.User)
            };




            var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {

                DataStore = new FileDataStore("CredentialCacheFolder", false),
                Scopes = new[] { "https://mail.google.com/" },
                ClientSecrets = clientSecrets,
                LoginHint = myEmail
            });

            var codeReceiver = new LocalServerCodeReceiver();
            var authCode = new AuthorizationCodeInstalledApp(codeFlow, codeReceiver);

            var credential = await authCode.AuthorizeAsync(myEmail, CancellationToken.None);

            if (credential.Token.IsStale)
                await credential.RefreshTokenAsync(CancellationToken.None);


            // get user input 
            Console.WriteLine("Welcome back!");
            Console.WriteLine("--------------------------------------------------------------");

            Console.Write("Enter the recipient's address: ");
            string recipient = Console.ReadLine();

            Console.Write("Enter the subject line: ");
            string subject = Console.ReadLine();

            Console.Write("Enter the email body: ");
            string body = Console.ReadLine();

            Console.WriteLine("-------------------------Email Draft--------------------------");

            Console.WriteLine("To:" + recipient + "\nSubject:" + subject + "\n\n" + body);

            Console.WriteLine("Send email? Y/N");
            string userInput = Console.ReadLine();

            while (userInput.ToUpper() != "Y" && userInput.ToUpper() != "N")
            {
                Console.WriteLine("Invalid input, please try again.");
                Console.WriteLine("Send email? Y/N");
                userInput = Console.ReadLine();

            }
            if (userInput.ToUpper() == "N")
            {
                return;
            }

            // create email using MimeKit 
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("User", myEmail));
            email.To.Add(new MailboxAddress("", recipient));
            email.Subject = subject;
            email.Body = new TextPart("plain")
            {
                Text = body
            };


            // authenticate to Google via OAuth2
            var oauth2 = new SaslMechanismOAuthBearer(credential.UserId, credential.Token.AccessToken);

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(oauth2);
                    await client.SendAsync(email);
                    await client.DisconnectAsync(true);

                    Console.WriteLine("--------------------------------------------------------------");
                    Console.WriteLine($"Email successfully sent to {recipient} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("--------------------------------------------------------------");
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }
            }
        }
    }
}
