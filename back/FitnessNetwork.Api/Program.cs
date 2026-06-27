using System.Text;
using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.AccessControl;
using Npgsql;
using FitnessNetwork.Api.Modules.Clubs;
using FitnessNetwork.Api.Modules.Identity;
using FitnessNetwork.Api.Modules.Reporting;
using FitnessNetwork.Api.Modules.Scheduling;
using FitnessNetwork.Api.Modules.Staff;
using FitnessNetwork.Api.Modules.Subscriptions;
using FitnessNetwork.Common.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Default"));
dataSourceBuilder.MapEnum<StaffRoleType>("staff_role");
dataSourceBuilder.MapEnum<SubscriptionStatus>("subscription_status");
dataSourceBuilder.MapEnum<EntryMethod>("entry_method");
dataSourceBuilder.MapEnum<ClassStatus>("class_status");
dataSourceBuilder.MapEnum<BookingStatus>("booking_status");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(dataSource, o => o
    .MapEnum<StaffRoleType>("staff_role")
    .MapEnum<SubscriptionStatus>("subscription_status")
    .MapEnum<EntryMethod>("entry_method")
    .MapEnum<ClassStatus>("class_status")
    .MapEnum<BookingStatus>("booking_status")));

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.AddSingleton<JwtService>();

var jwtSettings = jwtSection.Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddStackExchangeRedisCache(opts =>
{
    opts.Configuration = builder.Configuration.GetConnectionString("Redis");
    opts.InstanceName = "FitnessNetwork:";
});

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(opts =>
{
    opts.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
    });
    opts.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClubService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<SubscriptionTypeService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<VisitService>();
builder.Services.AddScoped<SchedulingService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<TurnstileService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
