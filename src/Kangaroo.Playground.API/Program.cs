// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Kangaroo.API.Extensions;
using Kangaroo.Playground.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Was Added
builder.Services.AddServiceCollection();

var app = builder.Build();

// Was Added
await app.ConfigureDatabase();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Was added
app.UseKangarooException();

app.UseHttpsRedirection();

app.UseAuthorization();

// Was added
app.UseAuthentication();

app.MapControllers();

app.Run();
