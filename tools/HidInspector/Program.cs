using System.Text.Json;
using System.Globalization;
using MagicMouseWindows.Hid;

const string CandidateNotice = "Candidate labels are inspection hints only; protocol support is not verified.";

if (!TryParseArguments(args, out CliOptions options, out string? argumentError))
{
    Console.Error.WriteLine(argumentError);
    WriteUsage(Console.Error);
    return 2;
}

if (options.Help)
{
    WriteUsage(Console.Out);
    return 0;
}

HidInspectionResult result = HidDeviceInspector.Inspect();
HidDeviceInfo[] devices = result.Devices
    .Where(device => !options.AppleOnly || device.Candidate.IsAppleDevice)
    .ToArray();

var output = new InspectorOutput(
    DateTimeOffset.UtcNow,
    CandidateNotice,
    devices,
    result.Errors);

if (options.Json || options.OutputPath is not null)
{
    string json = JsonSerializer.Serialize(
        output,
        new JsonSerializerOptions { WriteIndented = true });

    if (options.OutputPath is not null)
    {
        try
        {
            string fullPath = Path.GetFullPath(options.OutputPath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, json);
            Console.Error.WriteLine($"Exported {devices.Length} HID interface(s) to {fullPath}");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException)
        {
            Console.Error.WriteLine($"Unable to export JSON: {exception.Message}");
            return 3;
        }
    }

    if (options.Json)
    {
        Console.WriteLine(json);
    }
}
else
{
    WriteText(devices, result.Errors);
}

return result.Errors.Count == 0 ? 0 : 1;

static bool TryParseArguments(
    string[] arguments,
    out CliOptions options,
    out string? error)
{
    bool appleOnly = false;
    bool json = false;
    bool help = false;
    string? outputPath = null;

    for (int index = 0; index < arguments.Length; index++)
    {
        switch (arguments[index])
        {
            case "--apple-only":
                appleOnly = true;
                break;
            case "--json":
                json = true;
                break;
            case "--output":
                if (++index >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index]))
                {
                    options = default;
                    error = "--output requires a file path.";
                    return false;
                }

                outputPath = arguments[index];
                break;
            case "--help":
            case "-h":
                help = true;
                break;
            default:
                options = default;
                error = $"Unknown argument: {arguments[index]}";
                return false;
        }
    }

    options = new CliOptions(appleOnly, json, outputPath, help);
    error = null;
    return true;
}

static void WriteUsage(TextWriter writer)
{
    writer.WriteLine("HidInspector - safely inspect present Windows HID interfaces (no input capture)");
    writer.WriteLine();
    writer.WriteLine("Usage: HidInspector [--apple-only] [--json] [--output <path>]");
    writer.WriteLine();
    writer.WriteLine("  --apple-only  Include only interfaces identified as Apple devices.");
    writer.WriteLine("  --json        Write JSON to standard output.");
    writer.WriteLine("  --output      Export JSON to a file (may be combined with --json).");
    writer.WriteLine("  --help, -h    Show this help.");
}

static void WriteText(
    IReadOnlyList<HidDeviceInfo> devices,
    IReadOnlyList<HidInspectionError> enumerationErrors)
{
    Console.WriteLine($"Present HID interfaces: {devices.Count}");
    Console.WriteLine(CandidateNotice);

    for (int index = 0; index < devices.Count; index++)
    {
        HidDeviceInfo device = devices[index];
        Console.WriteLine();
        Console.WriteLine($"[{index + 1}] {device.Product ?? "(product unavailable)"}");
        Console.WriteLine($"  Path: {device.DevicePath}");
        Console.WriteLine(
            $"  VID:PID:VER: {FormatHex(device.VendorId)}:{FormatHex(device.ProductId)}:{FormatHex(device.VersionNumber)}");
        Console.WriteLine($"  Manufacturer: {device.Manufacturer ?? "(unavailable)"}");
        Console.WriteLine($"  Serial: {device.SerialNumber ?? "(unavailable)"}");
        Console.WriteLine(
            $"  Usage page/usage: {FormatHex(device.UsagePage)} / {FormatHex(device.Usage)}");
        Console.WriteLine(
            $"  Report bytes (input/output/feature): {FormatNumber(device.InputReportByteLength)} / {FormatNumber(device.OutputReportByteLength)} / {FormatNumber(device.FeatureReportByteLength)}");
        Console.WriteLine($"  Read access: {(device.CanOpenForRead ? "yes" : "no")}");
        Console.WriteLine(
            $"  Candidate: {(device.Candidate.IsCandidate ? "yes" : "no")} ({device.Candidate.Classification})");

        if (device.Candidate.Evidence.Count > 0)
        {
            Console.WriteLine($"  Candidate evidence: {string.Join("; ", device.Candidate.Evidence)}");
        }

        WriteValues("Hardware IDs", device.HardwareIds);
        WriteValues("Compatible IDs", device.CompatibleIds);

        foreach (HidInspectionError error in device.Errors)
        {
            Console.WriteLine($"  Error [{error.Operation}]: {FormatError(error)}");
        }
    }

    foreach (HidInspectionError error in enumerationErrors)
    {
        Console.Error.WriteLine($"Enumeration error [{error.Operation}]: {FormatError(error)}");
    }
}

static void WriteValues(string label, IReadOnlyList<string> values)
{
    Console.WriteLine(
        values.Count == 0
            ? $"  {label}: (unavailable)"
            : $"  {label}: {string.Join(" | ", values)}");
}

static string FormatHex(ushort? value) =>
    value.HasValue ? value.Value.ToString("X4", CultureInfo.InvariantCulture) : "????";

static string FormatNumber(ushort? value) =>
    value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "?";

static string FormatError(HidInspectionError error) =>
    error.NativeErrorCode.HasValue
        ? $"{error.Message} (code {error.NativeErrorCode.Value})"
        : error.Message;

internal readonly record struct CliOptions(
    bool AppleOnly,
    bool Json,
    string? OutputPath,
    bool Help);

internal sealed record InspectorOutput(
    DateTimeOffset InspectedAtUtc,
    string CandidateNotice,
    IReadOnlyList<HidDeviceInfo> Devices,
    IReadOnlyList<HidInspectionError> Errors);
