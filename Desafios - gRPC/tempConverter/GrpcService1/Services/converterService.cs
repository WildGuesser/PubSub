using System;
using Grpc.Core;
using GrpcService1;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GrpcService1
{
    public class converterService : TemperatureConverter.TemperatureConverterBase
    {
        private readonly ILogger<converterService> _logger;
        public converterService(ILogger<converterService> logger)
        {
            _logger = logger;
        }

        public override Task<TemperatureResponse> ConvertToFahrenheit(TemperatureRequest request, ServerCallContext context)
        {
            float celsius = request.Celsius;
            float fahrenheit = celsius * 9 / 5 + 32;
            return Task.FromResult(new TemperatureResponse { Fahrenheit = fahrenheit });
        }

    }
}