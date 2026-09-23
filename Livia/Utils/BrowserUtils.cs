using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Livia.Attributes;
using Microsoft.Data.Sqlite;

namespace Livia.Utils;

public static class BrowserUtils
{
    /// <summary>
    /// Extracts and decrypts a cookie value from any Chromium-based browser supporting DPAPI encryption.
    /// </summary>
    /// <remarks>
    /// This method requires elevated permissions to successfully
    /// create a Volume Shadow Copy (VSS), otherwise initialization will fail.
    /// </remarks>
    /// <param name="browser">
    /// The path relative to %USERPROFILE%\AppData\Local, excluding "User Data" 
    /// (e.g., "Vivaldi", "Google\Chrome", or "BraveSoftware\Brave-Browser").
    /// </param>
    /// <param name="hostName">The target domain/host for the cookie (e.g., ".example.com").</param>
    /// <param name="cookieName">The specific name of the cookie.</param>
    /// <returns>The decrypted cookie value text, or null if the cookie could not be found.</returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [RequiresLiviaOptIn(LiviaOptIn.BrowserCookieDecryption)]
    public static string? GetCookieValue(string browser, string hostName, string cookieName)
    {
        // Resolve dynamic base paths relative to AppData\Local using the provided browser profile directory
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string browserBasePath = Path.Combine(localAppData, browser, "User Data");

        string localStatePath = Path.Combine(browserBasePath, "Local State");
        string cookieDbPath = Path.Combine(browserBasePath, "Default", "Network", "Cookies");

        // 1. Get Decrypted Master AES Key via DPAPI from the browser's Local State configuration file
        byte[] masterKey = ExtractMasterKey(localStatePath);

        string tempDbPath = Path.GetTempFileName();
        string? shadowId = null;

        try
        {
            // 2. Create VSS Shadow Copy of the C: drive to securely bypass file locks if the browser is currently running
            var managementClass = new ManagementClass(@"\\.\root\cimv2", "Win32_ShadowCopy", null);
            var inParams = managementClass.GetMethodParameters("Create");
            inParams["Volume"] = "C:\\";
            inParams["Context"] = "ClientAccessible";

            var outParams = managementClass.InvokeMethod("Create", inParams, null);
            if (outParams == null || (uint)outParams["ReturnValue"] != 0)
            {
                throw new Exception($"VSS creation failed with error code: {outParams?["ReturnValue"]}");
            }

            shadowId = outParams["ShadowID"]?.ToString();
            string? shadowDeviceObject = null;

            // Retrieve the unique global device path associated with the created shadow snapshot
            using (var searcher = new ManagementObjectSearcher(
                $"SELECT DeviceObject FROM Win32_ShadowCopy WHERE ID='{shadowId}'"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    shadowDeviceObject = obj["DeviceObject"]?.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(shadowDeviceObject))
            {
                throw new Exception("Failed to resolve VSS shadow copy device path.");
            }

            // Map standard target file path over to the shadow device volume namespace
            string driveLetter = Path.GetPathRoot(cookieDbPath) ?? "C:\\";
            string relativePath = cookieDbPath.Substring(driveLetter.Length);
            string shadowFilePath = Path.Combine(shadowDeviceObject, relativePath);

            // Copy the active lock database file safely out of shadow storage
            File.Copy(shadowFilePath, tempDbPath, overwrite: true);

            // 3. Query the isolated database snapshot copy via SQLite
            using var connection = new SqliteConnection($"Data Source={tempDbPath};Mode=ReadOnly;");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT encrypted_value FROM cookies WHERE host_key = @host AND name = @name";
            command.Parameters.AddWithValue("@host", hostName);
            command.Parameters.AddWithValue("@name", cookieName);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    byte[] encryptedData = (byte[])reader["encrypted_value"];
                    return DecryptCookie(encryptedData, masterKey);
                }
            }
        }
        finally
        {
            // Clean up temporary database snapshot copy file
            if (File.Exists(tempDbPath))
            {
                try
                {
                    File.Delete(tempDbPath);
                }
                catch { }
            }

            // Clean up and release system VSS snapshot resources
            if (!string.IsNullOrEmpty(shadowId))
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_ShadowCopy WHERE ID='{shadowId}'");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        obj.InvokeMethod("Delete", null);
                    }
                }
                catch { }
            }
        }

        return null;
    }

    /// <summary>
    /// Reads and decrypts the browser's AES master key from the Local State JSON configuration file using Windows DPAPI.
    /// </summary>
    private static byte[] ExtractMasterKey(string localStatePath)
    {
        string jsonContent = File.ReadAllText(localStatePath);
        using var doc = JsonDocument.Parse(jsonContent);

        string? encryptedKeyBase64 = doc.RootElement
            .GetProperty("os_crypt")
            .GetProperty("encrypted_key")
            .GetString();

        if (string.IsNullOrEmpty(encryptedKeyBase64))
        {
            throw new InvalidOperationException("Encrypted key not found in browser Local State configuration.");
        }

        byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKeyBase64);

        // Strip the standard 5-byte 'DPAPI' prefix header string
        byte[] dpapiBytes = new byte[encryptedKeyBytes.Length - 5];
        Array.Copy(encryptedKeyBytes, 5, dpapiBytes, 0, dpapiBytes.Length);

        return ProtectedData.Unprotect(dpapiBytes, null, DataProtectionScope.CurrentUser);
    }

    /// <summary>
    /// Decrypts an individual v10/v11 encrypted cookie payload block using AES-GCM and the master key.
    /// </summary>
    private static string DecryptCookie(byte[] encryptedData, byte[] masterKey)
    {
        if (encryptedData.Length > 3 &&
            encryptedData[0] == 'v' && encryptedData[1] == '1' && (encryptedData[2] == '0' || encryptedData[2] == '1'))
        {
            byte[] nonce = new byte[12];
            Array.Copy(encryptedData, 3, nonce, 0, 12);

            int tagLength = 16;
            byte[] tag = new byte[tagLength];
            Array.Copy(encryptedData, encryptedData.Length - tagLength, tag, 0, tagLength);

            int cipherLength = encryptedData.Length - 3 - 12 - tagLength;
            byte[] ciphertext = new byte[cipherLength];
            Array.Copy(encryptedData, 15, ciphertext, 0, cipherLength);

            byte[] plaintextBytes = new byte[cipherLength];

            using var aesGcm = new AesGcm(masterKey, tag.Length);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            // Chromium cookie payloads may contain a 32-byte SHA-256 host-key hash
            // before the actual cookie value. Remove that prefix before UTF-8 decoding
            // or else we get garbage bytes before the actual value.
            const int hostKeyHashLength = 32;

            if (plaintextBytes.Length < hostKeyHashLength)
            {
                throw new CryptographicException(
                    "Decrypted cookie payload is too short.");
            }

            return Encoding.UTF8.GetString(
                plaintextBytes,
                hostKeyHashLength,
                plaintextBytes.Length - hostKeyHashLength);
        }

        throw new NotSupportedException("Unsupported cookie encryption scheme version.");
    }
}