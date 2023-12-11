namespace Dodkin.Service;

static class EventIds
{
    public static readonly EventId Servicing = new(1, "Worker");
    public static readonly EventId Receiving = new(10, "Receiving");
    public static readonly EventId Sending = new(20, "Sending");
}