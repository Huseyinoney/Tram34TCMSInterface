using MediatR;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Tram34TCMSInterface.Application.Features.ReadDataFromTCMS;

namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class ReadDataFromTCMSWithUDPBackgroundService : BackgroundService
    {
        private IMediator mediator;
        public ReadDataFromTCMSWithUDPBackgroundService(IMediator mediator)
        {
            this.mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            Console.WriteLine("UDP Background Service Başladı...");
            var command = new ReadDataFromTCMSCommand(); // Döngü dışına alındı

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await mediator.Send(command, stoppingToken);
                    if (result is not null)
                    {
                        string jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine($"Alınan veri: {jsonResult}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata oluştu: {ex.Message}");
                }

            }
        }
    }
}