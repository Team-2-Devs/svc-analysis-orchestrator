using System.Text.Json;
using Messaging.MessageContracts;
using Messaging.RabbitMQ;

namespace AnalysisOrchestrator.Workers;

public sealed class ImageUploadedWorker : BackgroundService
{
    private readonly IEventPublisher _publisher;
    private readonly IEventConsumer _consumer;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ImageUploadedWorker(IEventPublisher publisher, IEventConsumer consumer)
    {
        _publisher = publisher;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            // Declare queue
            const string queue = "orchestrator.image-uploaded";

            // Subscribe to ImageUploaded fanout exchange
            await _consumer.SubscribeAsync(queue, Exchanges.ImageUploaded, ct);
            
            // Start consume loop
            await _consumer.RunAsync(HandleImageUploadedAsync, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Error: {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }
    
    private async Task<bool> HandleImageUploadedAsync(ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        ImageUploaded? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ImageUploaded>(body.Span, JsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Failed to deserialize ImageUploaded: {ex.Message}");
            return false; // Nack: payload is broken, don't requeue
        }
        if (payload == null)
        {
            Console.WriteLine("[Orchestrator] ImageUploaded payload was null");
            return false;
        }
        
        Console.WriteLine($"[Orchestrator] Received ImageUploaded for {payload.ObjectKey}");

        // Publish AnalysisStarted
        var started = new AnalysisStarted(payload.ObjectKey);
        await _publisher.PublishAsync(Exchanges.AnalysisStarted, "", started, ExchangeKind.Fanout, ct);
        
        Console.WriteLine($"[Orchestrator] Published AnalysisStarted for {payload.ObjectKey}");

        return (true); // Ack
    }
}