using System.Text;
using ASP.MongoDb.API.Middleware;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Services;
using ASP.MongoDb.API.Settings;
using ASP.MongoDb.API.SignalIR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

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

// Register Repositories and Other Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
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

// Add Controllers
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000") // Allow local frontend origins
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

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





//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.Events = new JwtBearerEvents
//        {
//            OnMessageReceived = context =>
//            {
//                // Check for the token in the cookies
//                var token = context.HttpContext.Request.Cookies["auth-token"];

//                if (!string.IsNullOrEmpty(token))
//                {
//                    context.Token = token;  // Attach the token to the request
//                }

//                return Task.CompletedTask;
//            }
//        };
//        options.Events = new JwtBearerEvents
//        {
//            OnMessageReceived = context =>
//            {
//                // Check for the token in the query string for SignalR
//                var accessToken = context.Request.Query["access_token"];

//                // Check for the token in cookies as a fallback
//                if (string.IsNullOrEmpty(accessToken))
//                {
//                    accessToken = context.HttpContext.Request.Cookies["auth-token"];
//                }

//                if (!string.IsNullOrEmpty(accessToken))
//                {
//                    context.Token = accessToken;  // Attach the token to the request
//                }

//                return Task.CompletedTask;
//            }
//        };

//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
//            ValidAudience = builder.Configuration["JwtSettings:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
//        };
//    });