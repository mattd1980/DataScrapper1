public class StatusHistory
{
    public string Timestamp { get; set; }
    public string StatusCode { get; set; }

    public StatusHistory(string timestamp, string statusCode)
    {
        Timestamp = timestamp;
        StatusCode = statusCode;
    }
}
