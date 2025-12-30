using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MyThuatDotNetContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Dbtest");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// CORS để MVC gọi API 
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.MapControllers();
app.Run();
