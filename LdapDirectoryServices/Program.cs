using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;

namespace LdapDirectoryServices
{
    public class Program
    {
        public static string ldapServer { get; set; }
        public static string adminUser { get; set; }
        public static string adminPassword { get; set; }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("net6 v1 - System.DirectoryServices.Protocols");

                Console.WriteLine("Domain:");
                ldapServer = Console.ReadLine();

                Console.WriteLine("Admin login:");
                adminUser = Console.ReadLine();

                Console.WriteLine("Admin password:");
                adminPassword = ReadPassword();

                Actions();
            }
            catch (LdapException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.ErrorCode);
                Console.WriteLine(ex.ServerErrorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}.");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"StackTrace: {ex.StackTrace}.");

            }
            Console.ReadKey();
        }

        private static void Actions()
        {
            Console.WriteLine();
            Console.WriteLine("Select action:");
            Console.WriteLine("1 - users count");
            Console.WriteLine("2 - show users");
            Console.WriteLine("3 - user password reset");

            var action = Console.ReadKey();
            Console.WriteLine();

            var ldapConnection = new LdapConnection(new LdapDirectoryIdentifier(ldapServer, 636));
            ldapConnection.Credential = new NetworkCredential(adminUser, adminPassword);
            ldapConnection.SessionOptions.SecureSocketLayer = true;
            ldapConnection.SessionOptions.VerifyServerCertificate = (con, cer) => true;
            ldapConnection.Bind();

            switch (action.Key)
            {
                case ConsoleKey.D1:
                    GetUsersCount(ldapConnection);
                    break;
                case ConsoleKey.D2:
                    GetUsers(ldapConnection);
                    break;
                case ConsoleKey.D3:
                    ChangeUserPassword(ldapConnection);
                    break;
                default:
                    Console.WriteLine("unhandled action");
                    break;
            }

            Actions();
        }

        private static void GetUsersCount(LdapConnection ldapConnection)
        {
            var dn = GetDnFromHost(ldapServer);
            var searchRequest = new SearchRequest(dn, "(objectClass=user)", SearchScope.Subtree, null);
            var searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);

            Console.WriteLine("Users count: " + searchResponse.Entries.Count);
        }

        private static void GetUsers(LdapConnection ldapConnection)
        {
            var dn = GetDnFromHost(ldapServer);
            var searchRequest = new SearchRequest(dn, "(objectClass=user)", SearchScope.Subtree, null);
            var searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                Console.WriteLine(entry.DistinguishedName);
            }
        }

        private static void ChangeUserPassword(LdapConnection ldapConnection)
        {

            var newPassword = Guid.NewGuid() + "@$$";

            Console.WriteLine("Enter user DN (example: CN=Test,CN=Users,DC=int,DC=hideez,DC=com)");
            var userDn = Console.ReadLine();
            var modifyRequest = new ModifyRequest(userDn, DirectoryAttributeOperation.Replace, "unicodePwd", Encoding.Unicode.GetBytes($"\"{newPassword}\""));

            try
            {
                var r = ldapConnection.SendRequest(modifyRequest);
                Console.WriteLine("Password changed successfully.");
            }
            catch (LdapException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.ErrorCode);
                Console.WriteLine(ex.ServerErrorMessage);
            }
        }

        private static string GetDnFromHost(string hostname)
        {
            char separator = '.';
            var parts = hostname.Split(separator);
            var dnParts = parts.Select(_ => $"dc={_}");
            return string.Join(",", dnParts);
        }

        private static string ReadPassword()
        {
            string password = string.Empty;
            ConsoleKeyInfo keyInfo;

            do
            {
                keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    Console.Write("\b \b");
                }
                else if (keyInfo.Key != ConsoleKey.Enter && keyInfo.Key != ConsoleKey.Backspace)
                {
                    password += keyInfo.KeyChar;
                    Console.Write("*");
                }

            } while (keyInfo.Key != ConsoleKey.Enter);

            return password;
        }
    }
}