using ASP.MongoDb.API.Middleware;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using ASP.MongoDb.API.Settings;
using ASP.MongoDb.API.SignalIR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure Redis as Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection"); // Fetch Redis connection
});

// Configure MongoDB settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// Configure Session Services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Prevent JavaScript access to cookies
    options.Cookie.IsEssential = true; // Mark cookies as essential for session tracking
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Enforce secure cookies
});

// Allow large file uploads (vidaos up to 1 GB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824; // 1 GB
});

// Register Repositories and Other Services
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IGenerateFilesRepository, GenerateFilesRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITasksRepository, TasksRepository>();
builder.Services.AddScoped<IStructureOfSystemRepository, StructureOfSystemRepository>();
builder.Services.AddScoped<IDataOfStructureRepository, DataOfStuctureRepository>();


// Add RedisExample Service
builder.Services.AddSingleton<RedisExample>(provider =>
{
    var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
    var cache = provider.GetRequiredService<IDistributedCache>();
    return new RedisExample(cache); // Use IDistributedCache in RedisExample
});


// Register MongoClient as Singleton
builder.Services.AddSingleton<MongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});


// Add Controllers
builder.Services.AddControllers();


// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://localhost") // Allow local frontend origins
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

Console.OutputEncoding = System.Text.Encoding.UTF8;
// Enable Swagger/OpenAPI
builder.Services.AddOpenApi();

// Add SignalR
builder.Services.AddSignalR();

// Add Logging
builder.Logging.AddConsole();

// Build the app
var app = builder.Build();

// Configure Middleware
app.UseCors("AllowFrontend");
app.UseSession(); // Add session middleware
app.UseHttpsRedirection();
app.UseMiddleware<SessionAuthorizationMiddleware>(); // Custom session authorization middleware

// Map Controllers and Hubs
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Run the app
app.Run();
