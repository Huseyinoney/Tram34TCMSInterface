using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tram34TCMSInterface.Application.Abstractions.UDP;
using Tram34TCMSInterface.Infrastructure.Services.UDP;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddSingleton<IReadDataFromTCMSWithUDP, ReadDataFromTCMSWithUDP>();

using IHost host = builder.Build();
await host.RunAsync();
