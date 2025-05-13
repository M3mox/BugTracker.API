using BugTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddDbContext<BugContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddEndpointsApiExplorer(); // <--- wichtig
builder.Services.AddSwaggerGen();           // <--- wichtig

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                        // <--- wichtig
    app.UseSwaggerUI();                     // <--- wichtig
}

// Use CORS
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BugContext>();
    db.Database.EnsureCreated(); // Legt DB an, wenn nicht vorhanden
}
 

app.Run();
