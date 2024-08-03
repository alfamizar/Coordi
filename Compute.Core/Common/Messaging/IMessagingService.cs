namespace Compute.Core.Common.Messaging
{
    public interface IMessagingService
    {
        void Send<TMessage>(TMessage message) where TMessage : class;

        void Subscribe<TRecipient, TMessage>(TRecipient recipient) where TMessage : class;
    }
}
