
using EmailTracker.Domain.Services;
using EmailTracker.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

namespace EmailTracker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
       
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddMemoryCache();

            // Add JWT Authentication
            builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
            {
                var jwt = builder.Configuration.GetSection("Jwt");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]))
                };


                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        // Stop the default 401 Unauthorized response
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        // Create your custom error response
                        var errorResponse = new
                        {
                            Message = "You are not authorized to access this resource.",
                            ErrorCode = "UnauthorizedAccess",
                            Details = context.ErrorDescription // Optional: include more details from the challenge
                        };

                        await context.Response.WriteAsJsonAsync(errorResponse);
                    }
                };
            });


            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            // Add this line to register BounceMonitorService as a hosted service in the DI container
            builder.Services.AddHostedService<BounceMonitorService>();

            var app = builder.Build();

            //app.UseMiddleware<IpWhitelistMiddleware>();  // For internal endpoints
            app.UseAuthentication();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // ?? OpenAPI (JSON)
                app.MapOpenApi()
                   .AllowAnonymous(); // IMPORTANT to avoid 403

                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "v1");
                    options.RoutePrefix = "swagger";

                });

                app.UseReDoc(options =>
                {
                    options.SpecUrl = "/openapi/v1.json";
                    options.RoutePrefix = "redoc";

                });

                app.MapScalarApiReference(options =>
                {
                    options.Title = "My Scalar UI. Asri";
                    options.Theme = ScalarTheme.Alternate;
                    options.DarkMode = true;
                    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
                    options.CustomCss = "";
                    options.ShowSidebar = true;
                    options.AddPreferredSecuritySchemes("BearerAuth")
                    .AddHttpAuthentication("BearerAuth", auth =>
                    {
                        auth.Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
                    });
                });
            }
            app.MapPost("/api/email/webhook", async (HttpContext context) =>
            {
                // Parse webhook payload (e.g., JSON with event: "bounce", tracking ID, etc.)
                // Update tracking store accordingly
                // Example: If event is "bounce" and type is "hard", set status to "failed" with "hard bounce"
                return Results.Ok();
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
