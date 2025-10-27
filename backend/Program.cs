using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.Services;
using SantiyeTalepApi.Mappings;
using System.Text;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // JSON property naming policy'yi camelCase olarak ayarla
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.WriteIndented = true;
});

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient with retry policy
builder.Services.AddHttpClient("ProductApi", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "SantiyeTalepApi/1.0");
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Regular HttpClient for RequestController
builder.Services.AddHttpClient();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Şantiye Talep Yönetim API", 
        Version = "v1",
        Description = "Şantiye çalışanlarının talep yönetimi için API"
    });

    // JWT için Swagger yapılandırması
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Şantiye Talep API V1");
        c.RoutePrefix = string.Empty; // Swagger UI'ı root'ta göster
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Request logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context.Request.Method} {context.Request.Path}");
    
    if (context.Request.ContentLength > 0)
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        Console.WriteLine($"Request Body: {body}");
    }
    
    await next();
    
    Console.WriteLine($"Response Status: {context.Response.StatusCode}");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
