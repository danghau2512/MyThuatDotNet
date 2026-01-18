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

// CORS cho MVC gọi API
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// ✅ PHẢI CÓ để serve ảnh /uploads/...
app.UseStaticFiles();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();
