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

//franky edited
builder.Services.AddEndpointsApiExplorer(); // <--- wichtig
builder.Services.AddSwaggerGen();           // <--- wichtig
//end franky edited
var app = builder.Build();

//franky edited
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                        // <--- wichtig
    app.UseSwaggerUI();                     // <--- wichtig
}
//end franky edited
// Use CORS
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
