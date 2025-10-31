using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Messaging.MessageContracts;
using Messaging.RabbitMQ;

namespace AnalysisOrchestrator.Workers;

public sealed class RabbitCommandConsumer : BackgroundService
{
    private readonly IEventPublisher _events;
    private readonly string _host, _user, _pass;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public RabbitCommandConsumer(IEventPublisher events, string host, string user, string pass)
    {
        _events = events;
        (_host, _user, _pass) = (host, user, pass);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _host,
            UserName = _user,
            Password = _pass
        };

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var connection = await factory.CreateConnectionAsync(ct);
                await using var channel = await connection.CreateChannelAsync();

                // Declare the direct exchange for commands
                await channel.ExchangeDeclareAsync(Exchanges.AnalysisCommands, ExchangeType.Direct, durable: true);

                // Declare the fanout exchanges for events
                await channel.ExchangeDeclareAsync(Exchanges.AnalysisStarted, ExchangeType.Fanout, durable: true);
                await channel.ExchangeDeclareAsync(Exchanges.AnalysisCompleted, ExchangeType.Fanout, durable: true);

                // Queue for this orchestrator service
                const string queue = "orchestrator.analysis.commands";
                await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
                await channel.QueueBindAsync(queue, Exchanges.AnalysisCommands, Routes.RequestAnalysis);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    var command = JsonSerializer.Deserialize<RequestAnalysis>(ea.Body.Span, JsonOpts);
                    if (command is null) return;

                    // Handle command
                    Console.WriteLine($"[Orchestrator] Received RequestAnalysis for {command.ObjectKey}");

                    // Publish started event
                    var started = new AnalysisStarted(command.CorrelationId, command.ObjectKey);
                    await _events.PublishAsync(Exchanges.AnalysisStarted, "", started, ExchangeKind.Fanout, ct);

                    // Simulate work
                    await Task.Delay(2000, ct);

                    // Publish completed event
                    var completed = new AnalysisCompleted(command.CorrelationId, command.ObjectKey, Success: true);
                    await _events.PublishAsync(Exchanges.AnalysisCompleted, "", completed, ExchangeKind.Fanout, ct);
                };

                await channel.BasicConsumeAsync(queue, autoAck: true, consumer, ct);
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Orchestrator] Error: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }
}