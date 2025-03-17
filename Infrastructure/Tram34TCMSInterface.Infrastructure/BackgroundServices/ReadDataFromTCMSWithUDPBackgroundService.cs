using MediatR;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Tram34TCMSInterface.Application.Features.ReadDataFromTCMS;
using Tram34TCMSInterface.Application.Features.SendDataToLogicManagerFromTCMS;

namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class ReadDataFromTCMSWithUDPBackgroundService : BackgroundService
    {
        private IMediator mediator;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        ReadDataFromTCMSCommand readDataFromTCMSCommand = new ReadDataFromTCMSCommand();
        SendDataToLogicManagerFromTCMSCommand sendDataToLogicManagerFromTCMSCommand = new SendDataToLogicManagerFromTCMSCommand();
        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
        public ReadDataFromTCMSWithUDPBackgroundService(IMediator mediator)

        {
            this.mediator = mediator;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("UDP Background Service Başladı...");
            while (!stoppingToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(stoppingToken);
                try
                {
                    var result = await mediator.Send(readDataFromTCMSCommand, stoppingToken);
                    if (result is not null)
                    {
                        sendDataToLogicManagerFromTCMSCommand.trainData = result;
                        await mediator.Send(sendDataToLogicManagerFromTCMSCommand, stoppingToken);
                        string jsonResult = JsonSerializer.Serialize(result, jsonSerializerOptions);
                        Console.WriteLine($"Alınan veri: {jsonResult}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata oluştu: {ex.Message}");
                }
                finally { _semaphore.Release(); }
            }
        }
    }
}