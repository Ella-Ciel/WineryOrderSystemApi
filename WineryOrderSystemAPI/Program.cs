using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WineryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PrimaryConnection")));

builder.Services.AddDbContext<WineryReadDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SecondaryConnection")));

// Register HttpClient and Chatbot Service
builder.Services.AddHttpClient<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

builder.Services.AddAutoMapper(typeof(WineryOrderSystemAPI.Mappings.MappingProfile));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();