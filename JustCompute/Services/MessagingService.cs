using CommunityToolkit.Mvvm.Messaging;
using Compute.Core.Common.Messaging;

namespace JustCompute.Presentation.Dialogs
{
    public class MessagingService : IMessagingService
    {
        public void Send<TMessage>(TMessage message) where TMessage : class
        {
            WeakReferenceMessenger.Default.Send(message);
        }

        public void Subscribe<TRecipient, TMessage>(TRecipient recipient) where TMessage : class
        {
            if (recipient is IRecipient<TMessage> typedRecipient)
            {
                WeakReferenceMessenger.Default.Register(typedRecipient);
            }
            else
            {
                throw new ArgumentException($"{typeof(TRecipient).Name} does not implement IRecipient<{typeof(TMessage).Name}>", nameof(recipient));
            }
        }
    }
}
