namespace MagicMouseWindows.Hid;

public sealed record HidInspectionError(
    string Operation,
    int? NativeErrorCode,
    string Message);

public sealed record HidCandidateAssessment(
    bool IsAppleDevice,
    bool IsCandidate,
    string Classification,
    IReadOnlyList<string> Evidence);

public sealed record HidDeviceInfo(
    string DevicePath,
    ushort? VendorId,
    ushort? ProductId,
    ushort? VersionNumber,
    string? Manufacturer,
    string? Product,
    string? SerialNumber,
    IReadOnlyList<string> HardwareIds,
    IReadOnlyList<string> CompatibleIds,
    ushort? UsagePage,
    ushort? Usage,
    ushort? InputReportByteLength,
    ushort? OutputReportByteLength,
    ushort? FeatureReportByteLength,
    bool CanOpenForRead,
    HidCandidateAssessment Candidate,
    IReadOnlyList<HidInspectionError> Errors);

public sealed record HidInspectionResult(
    IReadOnlyList<HidDeviceInfo> Devices,
    IReadOnlyList<HidInspectionError> Errors);
