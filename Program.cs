using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartWasteManagement.Data;
using SmartWasteManagement.Hubs;
using SmartWasteManagement.Middleware;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
EnvLoader.LoadDotEnv(builder.Environment.ContentRootPath);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var mongoSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>() ?? new MongoDbSettings();
// Prefer environment variable if provided (MONGODBCONN in .env or host env)
var envConn = Environment.GetEnvironmentVariable("MONGODBCONN");
if (!string.IsNullOrWhiteSpace(envConn))
{
    mongoSettings.ConnectionString = envConn.Trim();
}
Console.WriteLine($"[MongoDB] Using connection string: {(mongoSettings.ConnectionString?.Contains("mongodb+srv") == true ? "Atlas (mongodb+srv)" : mongoSettings.ConnectionString)}");
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoSettings.DatabaseName));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ITruckService, TruckService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddScoped<ISmartBinService, SmartBinService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<IPickupRequestService, PickupRequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IDataSeedService, DataSeedService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, SmartWasteManagement.Hubs.JwtUserIdProvider>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(2),
            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs") || path.Value?.Contains("/hubs", StringComparison.OrdinalIgnoreCase) == true))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Waste Management API",
        Version = "v1",
        Description = "Enterprise waste management platform with JWT, MongoDB, and SignalR."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Waste v1"));
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<LiveTrackingHub>("/hubs/tracking");

using (var scope = app.Services.CreateScope())
{
    // Verify MongoDB connectivity at startup
    try
    {
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        var db = client.GetDatabase(mongoSettings.DatabaseName);
        var ping = await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        Console.WriteLine($"[MongoDB] Ping OK: {ping}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MongoDB] Ping failed: {ex.Message}");
    }

    var seed = scope.ServiceProvider.GetRequiredService<IDataSeedService>();
    await seed.SeedAsync();
}

app.Run();
