using test;
using test.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// Seeding method
async Task SeedAdminUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Create Admin role if it doesn't exist
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Create default admin user
    var adminUser = await userManager.FindByEmailAsync("admin@test.com");
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = "admin@test.com",
            Email = "admin@test.com",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, "P4ssw0rd123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// Seed default admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();  // Create DB if not exists
    await SeedAdminUserAsync(services);
}

// Configure the HTTP request pipeline
app.ConfigureMiddleware();

app.Run();

// Extension methods for bootstrap organization
namespace test
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register application-specific services (equivalent to Spring's @Bean methods)
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add database context with Identity
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Add Identity
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Add custom services
            // services.AddScoped<IUserService, UserService>();
            // services.AddScoped<IOrderService, OrderService>();

            // Add logging
            // services.AddLogging(config =>
            // {
            //     config.AddConsole();
            //     config.AddDebug();
            // });

            return services;
        }
    }

    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Configure the HTTP request middleware pipeline (equivalent to Spring's filter chain)
        /// </summary>
        public static WebApplication ConfigureMiddleware(this WebApplication app)
        {
            // Exception handling (must be early in pipeline)
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // HTTPS redirection
            app.UseHttpsRedirection();

            // Static files
            app.UseStaticFiles();

            // Routing
            app.UseRouting();

            // Localization
            var supportedCultures = new[] { "en", "fr" };
            app.UseRequestLocalization(new RequestLocalizationOptions()
                .SetDefaultCulture("en")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures));

            // Authentication & Authorization (must be after routing, before endpoints)
            app.UseAuthentication();
            app.UseAuthorization();

            // Configure endpoints
            app.MapRoutes();

            return app;
        }

        /// <summary>
        /// Configure route mappings
        /// </summary>
        private static void MapRoutes(this WebApplication app)
        {
            // Area routes (checked first)
            app.MapControllerRoute(
                name: "area",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            // Default routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}
