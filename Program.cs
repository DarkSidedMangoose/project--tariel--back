using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ASP.MongoDb.API.Repository;
using ASP.MongoDb.API.Settings;
using ASP.MongoDb.API.SignalIR;

var builder = WebApplication.CreateBuilder(args);
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

// Add authentication with JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Check for the token in the cookies
                var token = context.HttpContext.Request.Cookies["auth-token"];

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;  // Attach the token to the request
                }

                return Task.CompletedTask;
            }
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Check for the token in the query string for SignalR
                var accessToken = context.Request.Query["access_token"];

                // Check for the token in cookies as a fallback
                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = context.HttpContext.Request.Cookies["auth-token"];
                }

                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;  // Attach the token to the request
                }

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
        };
    });


// Add authorization and configure the custom AdminPolicy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminPolicy", policy =>
        policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "superAdmin"));
    options.AddPolicy("FuckerPolicy", policy =>
    policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "fucker"));

});

// Bind MongoDB settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(nameof(MongoDbSettings)));

// Add services to the container
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITasksRepository, TasksRepository>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Logging.AddConsole();
var app = builder.Build();
app.UseCors("AllowFrontend");
// Configure middleware
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
