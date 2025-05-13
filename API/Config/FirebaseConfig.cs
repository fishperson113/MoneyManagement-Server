using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Google.Cloud.Firestore.V1;
using API.Helpers;

namespace API.Config
{
    public static class FirebaseConfig
    {
        public static IServiceCollection ConfigureFirebase(this IServiceCollection services, IConfiguration configuration)
        {
            var path = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new InvalidOperationException("Missing or invalid FIREBASE_CREDENTIAL_PATH env var");

            var credential = GoogleCredential.FromFile(path);

            var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
                            ?? configuration["Firebase:ProjectId"];

            var clientBuilder = new FirestoreClientBuilder { Credential = credential };
            var client = clientBuilder.Build();
            var firestoreDb = FirestoreDb.Create(projectId, client);
            services.AddSingleton(firestoreDb);

            // Make the credential available for other services
            services.AddSingleton(credential);

            return services;
        }


    }
}
