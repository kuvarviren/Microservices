using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
//add authentication service
builder.Services.AddAuthentication("Bearer")
               .AddJwtBearer("Bearer", options =>
               {

                   options.Authority = "https://localhost:7122/";
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateAudience = false
                   };
               });
builder.Services.AddOcelot();
var app = builder.Build();

app.UseOcelot();

app.Run();
