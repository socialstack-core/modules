﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Client.IO
{
    /// <summary>
    /// </summary>
    public class LocalStorage
    {
        /// <summary>
        /// 
        /// </summary>
        public static string FOLDER = "";
        private const string ACCOUNT_FILE = "Account.{0}.txt";
        private const string ACCOUNT_PRIVATE_KEY_FILE = "Account.{0}.Private.pem";
        private const string ACCOUNT_PUBLIC_KEY_FILE = "Account.{0}.Public.pem";

        /// <summary>
        /// </summary>
        public LocalStorage()
        {
        }

        // Public Specific Methods

        /// <summary>
        /// </summary>
        public async Task PersistAccount(string accountContactEmail, string accountId)
        {
            var file = string.Format(ACCOUNT_FILE, accountContactEmail.Trim().ToLower());
            var path = GetOutputPath(file);

            await WriteAsync(path, accountId);
        }

        /// <summary>
        /// </summary>
        public async Task<string> LoadAccount(string accountContactEmail)
        {
            var file = string.Format(ACCOUNT_FILE, accountContactEmail.Trim().ToLower());
            var path = GetOutputPath(file);

            return await ReadAsync(path);
        }

        /// <summary>
        /// </summary>
        public async Task PersistPrivateKey(string accountContactEmail, string privateKeyPem)
        {
            var file = string.Format(ACCOUNT_PRIVATE_KEY_FILE, accountContactEmail.Trim().ToLower());
            var path = GetOutputPath(file);

            await WriteAsync(path, privateKeyPem);
        }

        /// <summary>
        /// </summary>
        public async Task<string> LoadPrivateKey(string accountContactEmail)
        {
            var file = string.Format(ACCOUNT_PRIVATE_KEY_FILE, accountContactEmail.Trim().ToLower());
            var path = GetOutputPath(file);

            return await ReadAsync(path);
        }

        /// <summary>
        /// </summary>
        public async Task PersistPublicKey(string accountContactEmail, string publicKeyPem)
        {
            var file = string.Format(ACCOUNT_PUBLIC_KEY_FILE, accountContactEmail.Trim().ToLower());
            var path = GetOutputPath(file);

            await WriteAsync(path, publicKeyPem);
        }

        /// <summary>
        /// </summary>
        public async Task<string> LoadPublicKey(string accountContactEmail)
        {
            var file = string.Format(ACCOUNT_PUBLIC_KEY_FILE, accountContactEmail.Trim().ToLower());
            var path = GetOutputPath(file);

            return await ReadAsync(path);
        }

        // Public General Methods

        /// <summary>
        /// </summary>
        public async Task<string> ReadAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"File on path '{path}' does not exists!");
            }

            using (var stream = File.OpenRead(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        /// <summary>
        /// </summary>
        public async Task WriteAsync(string path, string text)
        {
            await WriteAsync(path, Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// </summary>
        public async Task WriteAsync(string path, byte[] data)
        {
            var fullPath = Path.GetFullPath(path);
            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var stream = File.Create(fullPath))
            {
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        // Private Helper Methods

        private string GetOutputPath(string fileName)
        {
            var directoryPath = GetOutputDirectoryPath();
            return directoryPath + fileName;
        }

        private string GetOutputDirectoryPath()
        {
            return FOLDER;
        }
    }
}