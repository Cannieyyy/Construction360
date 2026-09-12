using Construction360.Repositories;
using Construction360.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICES =====

// MVC
builder.Services.AddControllersWithViews();

// Database & Repositories
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISupervisorRepository, SupervisorRepository>();

// Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
    });

builder.Services.AddAuthorization();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== SWAGGER =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Construct360 API",
        Version = "v1",
        Description = "Workforce Management System API for ROCLA",
        Contact = new OpenApiContact
        {
            Name = "ROCLA Support",
            Email = "support@rocla.com"
        }
    });

    // Add cookie authentication to Swagger UI
    options.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
    {
        Name = "CookieAuth",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Description = "Cookie-based authentication. Login via /Account/Login first."
    });


    // Include XML comments if you have them
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// ===== DATABASE INITIALIZATION =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
        // DatabaseService constructor auto-initializes the database
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialization failed");
    }
}

// ===== MIDDLEWARE PIPELINE =====

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // ===== SWAGGER (only in Development) =====
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Construct360 API v1");
        options.RoutePrefix = "swagger"; // Access at /swagger
        options.DocumentTitle = "Construct360 API Documentation";
        options.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();