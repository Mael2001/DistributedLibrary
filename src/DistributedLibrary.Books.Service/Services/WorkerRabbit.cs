using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DistributedLibrary.Books.Service.Services
{
    public class WorkerRabbit : BackgroundService
    {
        private readonly ILogger _logger;
        private IConnection _connection;
        private IModel _channel;

        public WorkerRabbit(ILoggerFactory loggerFactory)
        {
            this._logger = loggerFactory.CreateLogger<WorkerRabbit>();
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            // create connection  
            _connection = factory.CreateConnection();

            // create channel  
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("rabbit", ExchangeType.Topic);
            _channel.QueueDeclare("rabbit", false, false, false, null);
            _channel.QueueBind("rabbit", "rabbit", "rabbit", null);
            _channel.BasicQos(0, 1, false);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                // received message  
                var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

                // handle the received message  
                HandleMessage(content);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume("rabbit", false, consumer);
            return Task.CompletedTask;
        }

        private async Task HandleMessage(string content)
        {
            // we just print this message   
            string[] subs = content.Split('%');
            foreach (var sub in subs)
            {
                _logger.LogInformation($"consumer received {sub}");
            }
            foreach (var entity in GetEntities())
            {
                if (entity["isbn"] == (subs[1]))
                {
                    _logger.LogInformation($"consumer found matching book {entity}");
                    using (var httpClient = new HttpClient())
                    {
                        var URL_AUTHOR = $"http://localhost:52611/{entity["authorId"]}/"; 
                        var URL_OTHER_AUTHOR = $"http://localhost:54376/{entity["authorId"]}/";
                        if (entity["id"]>5)
                        {
                            var response_AUTHOR = await httpClient.GetStringAsync($"{URL_AUTHOR}");
                            _logger.LogInformation($"consumer found matching author {response_AUTHOR}");
                            File.WriteAllTextAsync(subs[0], entity.ToString() + "\n" + response_AUTHOR);
                        }
                        else
                        {
                            var response_OTHER_AUTHOR = await httpClient.GetStringAsync($"{URL_OTHER_AUTHOR}");
                            _logger.LogInformation($"consumer found matching author {response_OTHER_AUTHOR}");
                            File.WriteAllTextAsync(subs[0], entity.ToString() + "\n"+ JsonConvert.DeserializeObject(response_OTHER_AUTHOR));
                        }
                    }
                }
            }
        }

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }
        public IEnumerable<dynamic> GetEntities()
        {
            return JsonConvert.DeserializeObject<IEnumerable<dynamic>>(File.ReadAllText(@"books.json"));
        }
        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}