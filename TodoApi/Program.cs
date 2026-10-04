using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Enums are read and written as text in JSON (for example "InProgress") instead of numbers
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// Overdue notifications: the notifier is an interface, so its implementation can change later
builder.Services.AddScoped<INotifier, LogNotifier>();
builder.Services.AddScoped<IOverdueReviewService, OverdueReviewService>();

// Automatic overdue review that runs in the background, without any HTTP request
builder.Services.AddHostedService<OverdueReviewBackgroundService>();

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HostingConnection")));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<TodoDbContext>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();