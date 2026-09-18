namespace MagicMouseWindows.Contracts.Protocol;

public static class ProtocolLimits
{
    public const int CurrentFixtureSchemaVersion = 1;
    public const int CurrentProfileSchemaVersion = 1;
    public const int MaxDocumentBytes = 16 * 1024 * 1024;
    public const int MaxFrames = 250_000;
    public const int MaxReportBytes = 4_096;
    public const int MaxContactsPerFrame = 32;
    public const int MaxOutputCommandsPerFrame = 16;
    public const int MaxDeviceMatches = 32;
    public const int MaxReportShapes = 32;
    public const int MaxDecodedFields = 128;
    public const int MaxTextLength = 512;
}
