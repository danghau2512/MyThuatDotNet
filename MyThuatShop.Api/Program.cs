using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MyThuatDotNetContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Dbtest");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// DI email sender
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// CORS: cho MVC (7288) gọi API (7090)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvc", p =>
        p.WithOrigins("https://localhost:7288")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// ✅ phải có để truy cập /uploads/...
app.UseStaticFiles();

app.UseCors("AllowMvc");

app.MapControllers();

app.Run();
