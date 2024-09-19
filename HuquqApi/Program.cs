using HuquqApi;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Register(builder.Configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware yapýlandýrmasý
app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();
