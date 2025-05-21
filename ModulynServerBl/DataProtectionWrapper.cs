using System.Security.Cryptography;
using System.Text;

namespace Modulyn.Server.Bl
{
    public static class DataProtectionWrapper
    {
        /// <summary>
        /// Encrypts data using DPAPI with the current user scope and returns the encrypted data as a base64 string.
        /// </summary>
        /// <param name="data">The string data to encrypt.</param>
        /// <returns>The encrypted data as a base64 string.</returns>
        public static string EncryptDataToBase64(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedBytes = ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypts base64 encoded data using DPAPI with the current user scope and returns the original string.
        /// </summary>
        /// <param name="base64EncryptedData">The base64 encoded encrypted data.</param>
        /// <returns>The decrypted string data.</returns>
        public static string DecryptDataFromBase64(string base64EncryptedData)
        {
            byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Encrypts data using DPAPI with the local machine scope and returns the encrypted data as a base64 string.
        /// </summary>
        /// <param name="data">The string data to encrypt.</param>
        /// <returns>The encrypted data as a base64 string.</returns>
        public static string EncryptDataMachineToBase64(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedBytes = ProtectedData.Protect(dataBytes, null, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypts base64 encoded data using DPAPI with the local machine scope and returns the original string.
        /// </summary>
        /// <param name="base64EncryptedData">The base64 encoded encrypted data.</param>
        /// <returns>The decrypted string data.</returns>
        public static string DecryptDataMachineFromBase64(string base64EncryptedData)
        {
            byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
