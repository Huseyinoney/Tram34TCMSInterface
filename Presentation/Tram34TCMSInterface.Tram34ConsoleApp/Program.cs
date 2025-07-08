using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tram34TCMSInterface.Application.Abstractions.CacheMemory;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Application.Abstractions.MongoDB;
using Tram34TCMSInterface.Application.Abstractions.TCP;
using Tram34TCMSInterface.Application.Abstractions.UDP;
using Tram34TCMSInterface.Application.Features.ReadDataFromTCMS;
using Tram34TCMSInterface.Infrastructure.BackgroundServices;
using Tram34TCMSInterface.Infrastructure.Log;
using Tram34TCMSInterface.Infrastructure.Repositories.Cache;
using Tram34TCMSInterface.Infrastructure.Services;
using Tram34TCMSInterface.Infrastructure.Services.TCP;
using Tram34TCMSInterface.Infrastructure.Services.UDP;

using Tram34TCMSInterface.Persistence.MongoDBContext;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


builder.Services.AddScoped<DBContextLog>(serviceProvider =>
{
    var connectionString = builder.Configuration["MongoDb:ConnectionString"];
    var databaseName = builder.Configuration["MongoDB:DatabaseName"];
    return new DBContextLog(connectionString, databaseName);
});
//Mediator
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ReadDataFromTCMSHandler>());


builder.Services.AddSingleton<IReadDataFromTCMSWithTCP, ReadDataFromTCMSWithTCP>();
//builder.Services.AddSingleton<IReadDataFromTCMSWithUDP, ReadDataFromTCMSWithUDP>();
builder.Services.AddSingleton<IMongoDBRepository, MongoDBRepository>();
builder.Services.AddSingleton<IMongoDBTrainConfigurationCacheService, MongoDbTrainConfigurationCacheService>();
builder.Services.AddSingleton<ILogFactory, LogFactory>();
builder.Services.AddSingleton<ILogService, LogService>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<HttpClient>();

//Background Service
//builder.Services.AddHostedService<ReadDataFromTCMSWithUDPBackgroundService>();
//builder.Services.AddHostedService<UdpSenderBackgroundService>();

//TCP ile veri gönderme simule edilmeli !!!
//builder.Services.AddHostedService<TCPSenderBackgroundService>();

builder.Services.AddHostedService<ReadDataFromTCMSWithTCPBackgroundService>();


using IHost host = builder.Build();
await host.RunAsync();