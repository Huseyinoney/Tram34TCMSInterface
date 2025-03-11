using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Tram34TCMSInterface.Application.Abstractions.UDP;
using Tram34TCMSInterface.Application.Features.ReadDataFromTCMS;
using Tram34TCMSInterface.Infrastructure.BackgroundServices;
using Tram34TCMSInterface.Infrastructure.Services.UDP;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

//Mediator
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ReadDataFromTCMSHandler>());

builder.Services.AddSingleton<IReadDataFromTCMSWithUDP, ReadDataFromTCMSWithUDP>();

//Background Service
builder.Services.AddHostedService<ReadDataFromTCMSWithUDPBackgroundService>();
builder.Services.AddHostedService<UdpSenderBackgroundService>();

using IHost host = builder.Build();
await host.RunAsync();