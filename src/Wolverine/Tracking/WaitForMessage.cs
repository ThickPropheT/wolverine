using JasperFx.Core.Reflection;

namespace Wolverine.Tracking;

internal class WaitForMessage<T> : ITrackedCondition
{
    private bool _isCompleted;

    public int UniqueNodeId { get; set; }

    public void Record(EnvelopeRecord record)
    {
        if (record.MessageEventType != MessageEventType.MessageSucceeded && record.MessageEventType != MessageEventType.MessageFailed)
        {
            return;
        }

        if (record.Envelope.Message is T)
        {
            if (UniqueNodeId != 0 && UniqueNodeId != record.UniqueNodeId)
            {
                return;
            }

            _isCompleted = true;
        }
    }

    public bool IsCompleted()
    {
        return _isCompleted;
    }

    public override string ToString()
    {
        var description = $"Wait for message of type {typeof(T).GetFullName()} to be received";
        if (UniqueNodeId != 0)
        {
            description += " at node " + UniqueNodeId;
        }

        return description;
    }
}