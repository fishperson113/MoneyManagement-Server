using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Google.Cloud.Firestore.V1;

namespace API.Config
{
    public static class FirebaseConfig
    {
        public static IServiceCollection ConfigureFirebase(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Lấy JSON credential từ biến môi trường
            var jsonCred = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_JSON");
            if (string.IsNullOrEmpty(jsonCred))
                throw new InvalidOperationException("Missing FIREBASE_CREDENTIAL_JSON env var");

            var credential = GoogleCredential.FromJson(jsonCred);

            // 2. Khởi tạo Firestore client
            var clientBuilder = new FirestoreClientBuilder { Credential = credential };
            var client = clientBuilder.Build();
            var firestoreDb = FirestoreDb.Create(configuration["Firebase:ProjectId"], client);

            services.AddSingleton(firestoreDb);

            return services;
        }
    }
}
