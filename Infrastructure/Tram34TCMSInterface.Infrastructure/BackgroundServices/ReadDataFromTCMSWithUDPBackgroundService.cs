using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Tram34TCMSInterface.Application.Features.ReadDataFromTCMS;
using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;


namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class ReadDataFromTCMSWithUDPBackgroundService : BackgroundService
    {
        private readonly IMediator mediator;
        private readonly IConfiguration configuration;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        ReadDataFromTCMSCommand readDataFromTCMSCommand = new ReadDataFromTCMSCommand();
        SendCoupledDataToCoupleExchangeFromTCMSCommand sendCoupledDataToCoupleExchangeFromTCMSCommand = new SendCoupledDataToCoupleExchangeFromTCMSCommand();
        SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand = new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand();
        public ReadDataFromTCMSWithUDPBackgroundService(IMediator mediator, IConfiguration configuration)
        {
            this.mediator = mediator;
            this.configuration = configuration;
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
                        sendCoupledDataToCoupleExchangeFromTCMSCommand.trainData = result;
                        await mediator.Send(sendCoupledDataToCoupleExchangeFromTCMSCommand, stoppingToken);

                        SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand.trainData = result;
                        await mediator.Send(SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata oluştu: {ex.Message}");
                }
                finally { _semaphore.Release(); }
                await Task.Delay(int.Parse(configuration["TrainSettings:SendDelayRabbitmq"]), stoppingToken);
            }
        }
    }
}