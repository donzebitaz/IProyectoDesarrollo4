using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// @author: Sebastián Alvarado García C5C341
// @author: Justin Andrés Badilla Ramírez C4C928
// @author: Abigail Crystal García Bonilla C5F263
// @author: Frank de Jesús Villalobos Elizondo C5K944

var builder = WebApplication.CreateBuilder(args);

// Los enums se leen y escriben como texto en el JSON (ej. "EnProgreso") en vez de numeros
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// Notificaciones de vencidas: el notificador es una interfaz, asi su implementacion puede cambiar despues
builder.Services.AddScoped<INotifier, LogNotifier>();
builder.Services.AddScoped<IOverdueReviewService, OverdueReviewService>();

// Revision automatica de vencidas que corre en segundo plano, sin ninguna solicitud HTTP
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