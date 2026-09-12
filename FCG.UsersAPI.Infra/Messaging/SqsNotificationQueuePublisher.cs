using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FCG.UsersAPI.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FCG.UsersAPI.Infra.Messaging;

public class SqsNotificationQueuePublisher : INotificationQueuePublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SqsNotificationQueuePublisher> _logger;
    private readonly string? _userCreatedQueueUrl;

    public SqsNotificationQueuePublisher(
        IAmazonSQS sqsClient,
        IConfiguration configuration,
        ILogger<SqsNotificationQueuePublisher> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _userCreatedQueueUrl = configuration["Aws:Sqs:UserCreatedQueueUrl"];
    }

    public async Task PublishUserCreatedAsync(Guid userId, string name, string email, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(_userCreatedQueueUrl))
        {
            _logger.LogWarning(
                "Aws:Sqs:UserCreatedQueueUrl não configurado — pulando dual-publish para a fila da Lambda de notificações.");
            return;
        }

        var payload = JsonSerializer.Serialize(new { userId, name, email, createdAt });

        try
        {
            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _userCreatedQueueUrl,
                MessageBody = payload
            });
        }
        catch (Exception ex)
        {
            // Best-effort: falha aqui não deve derrubar o registro do usuário, já concluído.
            _logger.LogError(ex, "Falha ao publicar UserCreated na fila SQS para o usuário {UserId}", userId);
        }
    }
}
