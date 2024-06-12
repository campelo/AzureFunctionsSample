namespace AzFunctionsSample.Options;

public class ServiceBusOptions
{
    public static string OptionsName => "ServiceBus";
    public string? ConnectionString { get; set; }
    public QueueOption? Queue { get; set; }

    public class QueueOption
    {
        public string? Normal { get; set; }
        public string? Rejected { get; set; }

    }
}
