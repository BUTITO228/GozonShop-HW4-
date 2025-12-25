using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common.Models;
using Common.RabbitMQ;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderService.Models;
using OrderService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.BackgroundServices
{
    public class PaymentResultConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private IConnection _connection;
        private RabbitMQ.Client.IModel _channel;

        public PaymentResultConsumer(IServiceProvider serviceProvider)
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
                queue: RabbitMQSettings.PaymentResultQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _channel.BasicQos(0, 1, false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<PaymentResultMessage>(messageJson);

                    Console.WriteLine($"Received payment result for order: {message.OrderId}, Success: {message.Success}");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var orderService = scope.ServiceProvider
                            .GetRequiredService<IOrderManagementService>();

                        var newStatus = message.Success ? OrderStatus.FINISHED : OrderStatus.CANCELLED;
                        await orderService.UpdateOrderStatusAsync(message.OrderId, newStatus);
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing payment result: {ex.Message}");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: RabbitMQSettings.PaymentResultQueue,
                autoAck: false,
                consumer: consumer
            );

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
