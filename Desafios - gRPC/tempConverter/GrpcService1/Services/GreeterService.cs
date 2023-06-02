//Vejam a documentação da Microsoft (https://learn.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-7.0) e este tutorial (https://www.youtube.com/watch?v=QyxCX2GYHxk)
//*********************
//GreeterService file
//*********************
using System;
using Grpc.Core;
using GrpcService1;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GrpcService1
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

    }
}