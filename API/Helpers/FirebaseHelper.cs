using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace API.Helpers
{
    public class FirebaseHelper
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public FirebaseHelper(GoogleCredential credential, string bucketName)
        {
            _bucketName = bucketName;
            _storageClient = StorageClient.Create(credential);
        }

        public async Task<string> UploadUserAvatarAsync(string userId, IFormFile file)
        {

            string filePath = $"user_avatars/{userId}/avatar.jpg";
            string encodedPath = Uri.EscapeDataString(filePath);
            string firebaseUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{encodedPath}?alt=media";

            using var stream = file.OpenReadStream();
            await _storageClient.UploadObjectAsync(_bucketName, filePath, file.ContentType, stream);

            // Create public URL
            return firebaseUrl;
        }
        public async Task<string> UploadGroupAvatarAsync(Guid groupId, IFormFile file)
        {
            // Similar to UploadUserAvatarAsync but for groups
            string folder = $"groups/{groupId}/images";
            return await UploadFileAsync(folder, file);
        }
        public async Task<string> UploadFileAsync(string folder, IFormFile file, bool generateUniqueName = true)
        {
            if (file is null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            // Generate filename
            string filename = file.FileName;
            if (generateUniqueName)
            {
                string extension = Path.GetExtension(file.FileName);
                filename = $"{Guid.NewGuid()}{extension}";
            }

            // Ensure folder path is formatted correctly
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            string filePath = $"{folder}{filename}";
            string encodedPath = Uri.EscapeDataString(filePath);
            string firebaseUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{encodedPath}?alt=media";

            using var stream = file.OpenReadStream();
            await _storageClient.UploadObjectAsync(_bucketName, filePath, file.ContentType, stream);

            return firebaseUrl;
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                // Extract the file path from the Firebase Storage URL
                if (string.IsNullOrEmpty(fileUrl))
                {
                    return false;
                }

                Uri uri = new Uri(fileUrl);
                string path = uri.AbsolutePath;

                // Firebase URLs typically have the format /v0/b/BUCKET_NAME/o/FILE_PATH
                if (path.StartsWith("/v0/b/"))
                {
                    int startIndex = path.IndexOf("/o/") + 3;
                    if (startIndex > 3)
                    {
                        path = path.Substring(startIndex);
                        // URL decode the path
                        path = Uri.UnescapeDataString(path);

                        // Remove any query parameters
                        int queryIndex = path.IndexOf('?');
                        if (queryIndex >= 0)
                        {
                            path = path.Substring(0, queryIndex);
                        }

                        await _storageClient.DeleteObjectAsync(_bucketName, path);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
