using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SantiyeTalepWebUI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson()
    .AddRazorRuntimeCompilation(); // Enable runtime compilation for better hot reload

// HTTP Context Accessor for sessions
builder.Services.AddHttpContextAccessor();

// HTTP Client for API calls
builder.Services.AddHttpClient("SantiyeTalepAPI", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://elementelkapi.com/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
});

// Custom services
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? ""))
        };
    });

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// Add middleware to initialize auth from cookies
app.Use(async (context, next) =>
{
    // Only initialize for non-API requests to avoid interference
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        var authService = context.RequestServices.GetService<IAuthService>();
        if (authService != null)
        {
            try
            {
                authService.InitializeFromCookies();
            }
            catch (Exception ex)
            {
                // Log error but don't break the request
                var logger = context.RequestServices.GetService<ILogger<Program>>();
                logger?.LogError(ex, "Error initializing auth from cookies");
            }
        }
    }
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Element}/{action=Index}/{id?}");

app.Run();
