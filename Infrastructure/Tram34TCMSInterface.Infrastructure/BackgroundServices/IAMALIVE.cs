using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class IAMALIVE : BackgroundService
    {
        private readonly ILogFactory logFactory;
        private readonly ILogService logService;
        private readonly IConfiguration configuration;

        public IAMALIVE(ILogFactory logFactory, ILogService logService, IConfiguration configuration)
        {
            this.logFactory = logFactory;
            this.logService = logService;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (Convert.ToBoolean(configuration["LogStatus:IMALIVE"]))
                    {

                        this.logService.SendLogAsync<InformationLog>(logFactory.CreateInformationLog("IMALIVE", "TCMSController"));
                    }
                }
                catch (Exception ex)
                {
                    // Hata yakalama istersen burada yap
                    // logService.LogError(ex.Message);
                }

                // 10 saniye bekle
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
