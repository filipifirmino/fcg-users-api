namespace FCG.UsersAPI.Application.Interfaces;

/// <summary>Dual-publish: além do RabbitMQ, envia o mesmo evento para SQS, que aciona a Lambda de notificações.</summary>
public interface INotificationQueuePublisher
{
    Task PublishUserCreatedAsync(Guid userId, string name, string email, DateTime createdAt);
}
