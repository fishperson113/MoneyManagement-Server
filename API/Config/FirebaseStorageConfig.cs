using API.Helpers;
using Google.Apis.Auth.OAuth2;

namespace API.Config
{
    public static class FirebaseStorageConfig
    {
        public static IServiceCollection ConfigureFirebaseStorage(this IServiceCollection services, IConfiguration configuration, GoogleCredential credential = null)
        {
            // Use the provided credential or get a new one
            if (credential == null)
            {
                var path = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    throw new InvalidOperationException("Missing or invalid FIREBASE_CREDENTIAL_PATH env var");

                credential = GoogleCredential.FromFile(path);
            }

            var bucketName = Environment.GetEnvironmentVariable("FIREBASE_STORAGE_BUCKET")
                          ?? configuration["Firebase:StorageBucket"];

            if (string.IsNullOrEmpty(bucketName))
                throw new InvalidOperationException("Missing or invalid FIREBASE_STORAGE_BUCKET env var");

            services.AddSingleton(new FirebaseHelper(credential, bucketName));

            return services;
        }
    }
}
