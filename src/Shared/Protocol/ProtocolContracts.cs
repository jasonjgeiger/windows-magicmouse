namespace MagicMouseWindows.Contracts.Protocol;

public sealed record DescriptorIdentity(
    ushort VendorId,
    ushort ProductId,
    ushort VersionNumber,
    string HardwareId,
    string CompatibleId,
    string Transport,
    string DescriptorSignatureSha256);

public sealed record DeviceProfileIdentity(
    string ProfileId,
    int SchemaVersion,
    string DescriptorSignatureSha256);

public sealed record CaptureEnvironment(
    string WindowsVersion,
    string Architecture,
    string BluetoothAdapter,
    string FirmwareObservation);

public sealed record CaptureProvenance(
    string ToolName,
    string ToolVersion,
    string Method,
    string Statement);

public sealed record CaptureMetadata(
    string Scenario,
    string DeviceModel,
    DescriptorIdentity Descriptor,
    CaptureEnvironment Environment,
    CaptureProvenance Provenance);

public sealed record RawTouchFrame(
    long TimestampMicroseconds,
    long Sequence,
    byte ReportId,
    byte[] Report);

public enum ContactPhase
{
    Down,
    Move,
    Up,
}

public sealed record NormalizedContact(
    int ContactId,
    ContactPhase Phase,
    double X,
    double Y,
    double? Pressure);

public sealed record ExpectedScrollCommand(
    long TimestampMicroseconds,
    int VerticalDelta,
    int HorizontalDelta);

public sealed record FixtureFrame(
    RawTouchFrame Raw,
    IReadOnlyList<NormalizedContact> ExpectedContacts,
    IReadOnlyList<ExpectedScrollCommand> ExpectedOutput);

public sealed record CaptureFixture(
    int SchemaVersion,
    DeviceProfileIdentity Profile,
    CaptureMetadata Metadata,
    IReadOnlyList<FixtureFrame> Frames);

public enum CompatibilityStatus
{
    Unknown,
    Captured,
    Decoded,
    ParserTested,
    HardwareTested,
    Experimental,
    Supported,
    Blocked,
}

public enum FieldConfidence
{
    Unknown,
    Hypothesis,
    Confirmed,
}

public sealed record DeviceMatch(
    ushort VendorId,
    ushort ProductId,
    string HardwareId,
    string CompatibleId,
    string DescriptorSignatureSha256);

public sealed record ReportShape(
    byte ReportId,
    int ExactLength);

public sealed record CoordinateRange(
    int MinimumX,
    int MaximumX,
    int MinimumY,
    int MaximumY,
    string Origin,
    string Orientation);

public sealed record DecodedField(
    string Name,
    FieldConfidence Confidence,
    string Notes);

public sealed record DeviceProfileDocument(
    int SchemaVersion,
    string ProfileId,
    string DisplayName,
    CompatibilityStatus Status,
    IReadOnlyList<DeviceMatch> DeviceMatches,
    IReadOnlyList<ReportShape> TouchReports,
    int MaxContacts,
    CoordinateRange Coordinates,
    string InitializationState,
    IReadOnlyList<DecodedField> Fields);
