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
    }
}
