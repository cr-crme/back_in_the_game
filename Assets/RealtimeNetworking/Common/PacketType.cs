namespace DevelopersHub.RealtimeNetworking.Common
{
    public static class Protocol
    {
        public const string version = "1.0.0";

    }

    public enum PacketType {
        Version = 0,
        CsvWriterDataEntry = 2,
        Int = 3,
        ChangeScene = 4,
        ShowYFrame = 5,
    }
}