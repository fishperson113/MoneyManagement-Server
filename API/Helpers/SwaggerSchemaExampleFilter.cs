using API.Models.DTOs;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace API.Helpers
{
    public class SwaggerSchemaExampleFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(SignInDTO))
            {
                if (schema.Properties.ContainsKey("email"))
                {
                    schema.Properties["email"].Example = new OpenApiString("test@example.com");
                }

                if (schema.Properties.ContainsKey("password"))
                {
                    schema.Properties["password"].Example = new OpenApiString("Test@123");
                }
            }

            if (context.Type == typeof(SignUpDTO))
            {
                if (schema.Properties.ContainsKey("firstName"))
                {
                    schema.Properties["firstName"].Example = new OpenApiString("John");
                }

                if (schema.Properties.ContainsKey("lastName"))
                {
                    schema.Properties["lastName"].Example = new OpenApiString("Doe");
                }

                if (schema.Properties.ContainsKey("email"))
                {
                    schema.Properties["email"].Example = new OpenApiString("john.doe@example.com");
                }

                if (schema.Properties.ContainsKey("password"))
                {
                    schema.Properties["password"].Example = new OpenApiString("StrongP@ssw0rd");
                }

                if (schema.Properties.ContainsKey("confirmPassword"))
                {
                    schema.Properties["confirmPassword"].Example = new OpenApiString("StrongP@ssw0rd");
                }
            }

            if (context.Type == typeof(CreateGroupDTO))
            {
                if (schema.Properties.ContainsKey("name"))
                {
                    schema.Properties["name"].Example = new OpenApiString("Finance Team");
                }

                if (schema.Properties.ContainsKey("description"))
                {
                    schema.Properties["description"].Example = new OpenApiString("For tracking shared expenses and budgeting");
                }

            }

            // Add more types as needed
        }
    }
}