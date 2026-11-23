using System.Security.Cryptography;

namespace MiniHttpServer.Framework.Authentication
{
    public class PasswordHasher
    {
        //private const int numeres = 100000;

        //public static string Hash(string password)
        //{
        //    byte[] salt = RandomNumberGenerator.GetBytes(16);
        //    byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, numeres, HashAlgorithmName.SHA256, 32);

        //    return "pbkdf2$" + numeres + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(key);
        //}

        public static bool Verify(string password,string DBPassword)
        {
            if (string.IsNullOrEmpty(DBPassword)) { return false; }

            string[] parts = DBPassword.Split('$');
            if (parts.Length!=4 || parts[0] != "pbkdf2")
            {
                return false;
            }

            int numeres = int.Parse(parts[1]);
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, numeres, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
