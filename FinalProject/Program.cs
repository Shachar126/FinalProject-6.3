using FinalProject.Components;
using MyClasses.Services;

namespace FinalProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // 🔥 Keep LoginSession as Singleton (as per your requirement)
            builder.Services.AddSingleton<LoginSession>();

            // Register HttpClient with a dynamic "User-Role" header
            builder.Services.AddScoped(sp =>
            {
                var loginSession = sp.GetRequiredService<LoginSession>();
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5101/")
                };

                // 🔥 Ensure this updates dynamically after login
                if (!string.IsNullOrEmpty(loginSession.role))
                {
                    httpClient.DefaultRequestHeaders.Remove("User-Role");
                    httpClient.DefaultRequestHeaders.Add("User-Role", loginSession.role);
                }

                return httpClient;
            });

            // 🔥 Register Controllers if needed (matches teacher's version)
            builder.Services.AddControllers();

            // Enable Authentication and Authorization Middleware (if needed)
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(); // Uncomment if you have authentication

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            // 🔥 Ensure routing, authentication, and authorization are set up
            app.UseRouting();
            app.UseAuthentication(); // Uncomment if you have authentication
            app.UseAuthorization();

            app.UseAntiforgery();

            // 🔥 Map Controllers
            app.MapControllers();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}