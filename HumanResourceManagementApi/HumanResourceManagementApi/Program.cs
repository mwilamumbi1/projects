using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using Core.HumanResourceManagementApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Required for JWT Bearer
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // Required for TokenValidationParameters, SymmetricSecurityKey
using System.Text; // Required for Encoding.UTF8.GetBytes
using System.Text.Json.Serialization; // Already there, but good to ensure

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration; // Keep for easier access

// Add services to the container.
builder.Services.AddScoped<IEmailService, SendGridEmailService>();

// Configure JSON serialization options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure CORS for your React application
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:5000") // Corrected to only the React app origin
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()); // Important for authentication cookies/headers if used
});

builder.Services.AddTransient<IUserService, UserService>();

// Configure JWT Bearer Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // Validate the server that created the token
        ValidateAudience = true, // Validate the recipient of the token
        ValidateLifetime = true, // Validate the token's expiration date
        ValidateIssuerSigningKey = true, // Validate the signing key
        ValidIssuer = configuration["JwtSettings:Issuer"], // Get issuer from appsettings.json
        ValidAudience = configuration["JwtSettings:Audience"], // Get audience from appsettings.json
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"])), // Get secret key
        ClockSkew = TimeSpan.Zero // No delay for token expiration validation
    };
});

// Register DbContext with SQL Server
builder.Services.AddDbContext<HRDataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add controllers (already present, but ensures order)
builder.Services.AddControllers();

// Swagger setup for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.UseStaticFiles();
// Order of middleware is CRITICAL for authentication and authorization!
app.UseHttpsRedirection(); // Redirects HTTP to HTTPS

app.UseRouting(); // Identifies what endpoint is being hit

app.UseCors("AllowReactApp"); // Must be after UseRouting and before UseAuthentication/UseAuthorization

app.UseAuthentication(); // THIS IS ESSENTIAL: Checks for the token and authenticates the user
app.UseAuthorization(); // THIS IS ESSENTIAL: Checks if the authenticated user has permissions

app.MapControllers(); // Maps incoming requests to controller actions

app.Run();