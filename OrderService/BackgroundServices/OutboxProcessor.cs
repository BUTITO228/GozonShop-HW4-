using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderService.Data;
using RabbitMQ.Client;

namespace OrderService.BackgroundServices
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private IConnection _connection;
        private RabbitMQ.Client.IModel _channel;


        public OutboxProcessor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
                UserName = "guest",
                Password = "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: RabbitMQSettings.PaymentRequestQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                        var unsent = await context.OutboxMessages
                            .Where(m => !m.Sent)
                            .OrderBy(m => m.CreatedAt)
                            .Take(10)
                            .ToListAsync(stoppingToken);

                        foreach (var message in unsent)
                        {
                            var body = Encoding.UTF8.GetBytes(message.Payload);

                            _channel.BasicPublish(
                                exchange: "",
                                routingKey: RabbitMQSettings.PaymentRequestQueue,
                                basicProperties: null,
                                body: body
                            );

                            message.Sent = true;
                            message.SentAt = DateTime.UtcNow;

                            Console.WriteLine($"Sent payment request from outbox: {message.Id}");
                        }

                        if (unsent.Any())
                        {
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }

                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in outbox processor: {ex.Message}");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
