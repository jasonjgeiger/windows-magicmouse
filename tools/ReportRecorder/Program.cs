using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MagicMouseWindows.Contracts.Protocol;
using MagicMouseWindows.Hid;

const int MaximumDurationSeconds = 600;
const int MaximumFrameCount = 10_000;
const string AcknowledgementOption = "--acknowledge-explicit-capture";

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

if (!options.Acknowledged)
{
    Console.Error.WriteLine(
        $"Raw HID input capture requires affirmative {AcknowledgementOption}.");
    return 2;
}

if (!TryValidateOptions(options, out string? validationError))
{
    Console.Error.WriteLine(validationError);
    return 2;
}

HidInspectionResult inspection = HidDeviceInspector.Inspect();
HidDeviceInfo[] selectedDevices = inspection.Devices
    .Where(device => string.Equals(
        device.DevicePath,
        options.DevicePath,
        StringComparison.OrdinalIgnoreCase))
    .ToArray();

if (selectedDevices.Length != 1)
{
    Console.Error.WriteLine(selectedDevices.Length == 0
        ? "The exact --device-path is not a currently present HID interface. No other device was selected."
        : "The exact --device-path matched multiple HID interfaces; capture was refused.");
    WriteInspectionErrors(inspection.Errors);
    return 3;
}

HidDeviceInfo selected = selectedDevices[0];
HidCaptureAssessment eligibility = HidCaptureEligibility.Assess(selected);
if (!eligibility.IsAllowed)
{
    Console.Error.WriteLine($"The selected HID interface is excluded: {eligibility.Reason}");
    return 3;
}

if (!selected.CanOpenForRead)
{
    Console.Error.WriteLine("The selected HID interface is present but cannot be opened for read access.");
    WriteInspectionErrors(selected.Errors);
    return 3;
}

if (selected.InputReportByteLength is not (> 0 and <= ProtocolLimits.MaxReportBytes))
{
    Console.Error.WriteLine(
        $"The selected interface input report length is unavailable or exceeds the {ProtocolLimits.MaxReportBytes}-byte protocol limit.");
    return 3;
}

if (selected.VendorId is not > 0 || selected.ProductId is not > 0)
{
    Console.Error.WriteLine(
        "The selected interface lacks the non-zero VID/PID required by the capture fixture contract.");
    return 3;
}

if (!TryResolveOutputPath(options.OutputPath!, out string outputPath, out string? pathError))
{
    Console.Error.WriteLine(pathError);
    return 2;
}

string? outputDirectory = Path.GetDirectoryName(outputPath);
if (string.IsNullOrWhiteSpace(outputDirectory))
{
    Console.Error.WriteLine("Unable to determine the output directory.");
    return 2;
}

string temporaryPath = Path.Combine(
    outputDirectory,
    $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");

using var captureCancellation = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKeyPress;

try
{
    Directory.CreateDirectory(outputDirectory);
    await using HidInputReportReader reader = HidInputReportReader.Open(selected);
    using var durationCancellation = new CancellationTokenSource(
        TimeSpan.FromSeconds(options.DurationSeconds));
    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
        captureCancellation.Token,
        durationCancellation.Token);

    Console.Error.WriteLine(
        $"Capturing exact device path for up to {options.DurationSeconds} second(s) or {options.MaxFrames} frame(s). Press Ctrl+C to stop.");

    var frames = new List<FixtureFrame>(Math.Min(options.MaxFrames, 4096));
    long started = Stopwatch.GetTimestamp();

    while (frames.Count < options.MaxFrames && !linkedCancellation.IsCancellationRequested)
    {
        byte[] report;
        try
        {
            report = await reader.ReadReportAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
        {
            break;
        }

        long timestampMicroseconds =
            Stopwatch.GetElapsedTime(started).Ticks / TimeSpan.TicksPerMicrosecond;
        frames.Add(new FixtureFrame(
            new RawTouchFrame(
                timestampMicroseconds,
                frames.Count,
                report[0],
                report),
            [],
            []));
    }

    if (frames.Count == 0)
    {
        throw new HidInputReadException(
            selected.DevicePath,
            "No input reports arrived before the capture bound expired.");
    }

    CaptureFixture fixture = CreateFixture(options.Scenario!, selected, frames);
    byte[] document = ProtocolDocumentSerializer.SerializeFixture(fixture);

    await WriteAtomicallyAsync(
        temporaryPath,
        outputPath,
        document,
        CancellationToken.None);

    Console.Error.WriteLine(
        $"Recorded {frames.Count} frame(s) to {outputPath}. No HID output reports were generated.");
    return 0;
}
catch (Exception exception) when (
    exception is HidInputOpenException or HidInputReadException or IOException
    or UnauthorizedAccessException or ProtocolContractException)
{
    Console.Error.WriteLine($"Capture failed: {exception.Message}");
    return 4;
}
finally
{
    Console.CancelKeyPress -= OnCancelKeyPress;
    try
    {
        File.Delete(temporaryPath);
    }
    catch (IOException)
    {
    }
    catch (UnauthorizedAccessException)
    {
    }
}

void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true;
    captureCancellation.Cancel();
}

static CaptureFixture CreateFixture(
    string scenario,
    HidDeviceInfo device,
    IReadOnlyList<FixtureFrame> frames)
{
    string hardwareId = FirstEvidence(device.HardwareIds);
    string compatibleId = FirstEvidence(device.CompatibleIds);
    string descriptorEvidence = BuildDescriptorSignatureEvidence(device);
    string descriptorSignature = Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(descriptorEvidence)));
    string profileId = string.Create(
        CultureInfo.InvariantCulture,
        $"research-hid-{device.VendorId!.Value:x4}-{device.ProductId!.Value:x4}-{descriptorSignature[..12]}");
    string deviceModel = BoundedText(
        device.Product ??
        device.Manufacturer ??
        $"HID {device.VendorId.Value:X4}:{device.ProductId.Value:X4}");
    string transport = device.HardwareIds
        .Concat(device.CompatibleIds)
        .Any(static value =>
            value.Contains("BTH", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
        ? "Bluetooth HID"
        : "HID";
    string toolVersion = Assembly.GetExecutingAssembly()
        .GetName()
        .Version?
        .ToString() ?? "1.0.0";

    var descriptor = new DescriptorIdentity(
        device.VendorId.Value,
        device.ProductId.Value,
        device.VersionNumber ?? 0,
        hardwareId,
        compatibleId,
        transport,
        descriptorSignature);
    var profile = new DeviceProfileIdentity(
        profileId,
        ProtocolLimits.CurrentProfileSchemaVersion,
        descriptorSignature);
    var metadata = new CaptureMetadata(
        BoundedText(scenario),
        deviceModel,
        descriptor,
        new CaptureEnvironment(
            BoundedText(Environment.OSVersion.VersionString),
            RuntimeInformation.ProcessArchitecture.ToString(),
            "Not collected",
            device.VersionNumber.HasValue
                ? $"HID version observation 0x{device.VersionNumber.Value:X4}"
                : string.Empty),
        new CaptureProvenance(
            "ReportRecorder",
            BoundedText(toolVersion),
            "Read-only asynchronous HID input report capture",
            "Explicit user-acknowledged research capture from one exact inspected HID path; no output reports were generated."));

    return new CaptureFixture(
        ProtocolLimits.CurrentFixtureSchemaVersion,
        profile,
        metadata,
        frames);
}

static string BuildDescriptorSignatureEvidence(HidDeviceInfo device)
{
    var builder = new StringBuilder();
    Append("vid", device.VendorId?.ToString("X4", CultureInfo.InvariantCulture));
    Append("pid", device.ProductId?.ToString("X4", CultureInfo.InvariantCulture));
    Append("version", device.VersionNumber?.ToString("X4", CultureInfo.InvariantCulture));
    Append("manufacturer", device.Manufacturer);
    Append("product", device.Product);
    Append("serial", device.SerialNumber);
    Append("usagePage", device.UsagePage?.ToString("X4", CultureInfo.InvariantCulture));
    Append("usage", device.Usage?.ToString("X4", CultureInfo.InvariantCulture));
    Append("inputLength", device.InputReportByteLength?.ToString(CultureInfo.InvariantCulture));
    Append("outputLength", device.OutputReportByteLength?.ToString(CultureInfo.InvariantCulture));
    Append("featureLength", device.FeatureReportByteLength?.ToString(CultureInfo.InvariantCulture));

    foreach (string value in device.HardwareIds.Order(StringComparer.OrdinalIgnoreCase))
    {
        Append("hardwareId", value);
    }

    foreach (string value in device.CompatibleIds.Order(StringComparer.OrdinalIgnoreCase))
    {
        Append("compatibleId", value);
    }

    return builder.ToString();

    void Append(string name, string? value)
    {
        builder.Append(name);
        builder.Append('=');
        builder.Append(value ?? string.Empty);
        builder.Append('\n');
    }
}

static string FirstEvidence(IReadOnlyList<string> values) =>
    BoundedText(values.Count > 0 ? values[0] : "Unavailable");

static string BoundedText(string value) =>
    value.Length <= ProtocolLimits.MaxTextLength
        ? value
        : value[..ProtocolLimits.MaxTextLength];

static bool TryResolveOutputPath(
    string path,
    out string outputPath,
    out string? error)
{
    try
    {
        outputPath = Path.GetFullPath(path);
        error = null;
        return true;
    }
    catch (Exception exception) when (
        exception is ArgumentException or NotSupportedException or PathTooLongException)
    {
        outputPath = string.Empty;
        error = $"Invalid --output path: {exception.Message}";
        return false;
    }
}

static async Task WriteAtomicallyAsync(
    string temporaryPath,
    string outputPath,
    byte[] document,
    CancellationToken cancellationToken)
{
    await using (var stream = new FileStream(
                     temporaryPath,
                     FileMode.CreateNew,
                     FileAccess.Write,
                     FileShare.None,
                     64 * 1024,
                     FileOptions.Asynchronous | FileOptions.WriteThrough))
    {
        await stream.WriteAsync(document, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    File.Move(temporaryPath, outputPath, overwrite: true);
}

static bool TryValidateOptions(CliOptions options, out string? error)
{
    if (string.IsNullOrWhiteSpace(options.DevicePath))
    {
        error = "--device-path is required.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(options.Scenario))
    {
        error = "--scenario is required.";
        return false;
    }

    if (options.Scenario.Length > ProtocolLimits.MaxTextLength)
    {
        error = $"--scenario must not exceed {ProtocolLimits.MaxTextLength} characters.";
        return false;
    }

    if (options.DurationSeconds is < 1 or > MaximumDurationSeconds)
    {
        error = $"--duration-seconds must be between 1 and {MaximumDurationSeconds}.";
        return false;
    }

    if (options.MaxFrames is < 1 or > MaximumFrameCount)
    {
        error = $"--max-frames must be between 1 and {MaximumFrameCount}.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(options.OutputPath))
    {
        error = "--output is required.";
        return false;
    }

    error = null;
    return true;
}

static bool TryParseArguments(
    string[] arguments,
    out CliOptions options,
    out string? error)
{
    string? devicePath = null;
    string? scenario = null;
    string? outputPath = null;
    int durationSeconds = 0;
    int maxFrames = 0;
    bool acknowledged = false;
    bool help = false;

    for (int index = 0; index < arguments.Length; index++)
    {
        string argument = arguments[index];
        switch (argument)
        {
            case "--device-path":
                if (!TryTakeValue(arguments, ref index, argument, out devicePath, out error))
                {
                    options = default;
                    return false;
                }

                break;
            case "--scenario":
                if (!TryTakeValue(arguments, ref index, argument, out scenario, out error))
                {
                    options = default;
                    return false;
                }

                break;
            case "--duration-seconds":
                if (!TryTakeInt(arguments, ref index, argument, out durationSeconds, out error))
                {
                    options = default;
                    return false;
                }

                break;
            case "--max-frames":
                if (!TryTakeInt(arguments, ref index, argument, out maxFrames, out error))
                {
                    options = default;
                    return false;
                }

                break;
            case "--output":
                if (!TryTakeValue(arguments, ref index, argument, out outputPath, out error))
                {
                    options = default;
                    return false;
                }

                break;
            case AcknowledgementOption:
                acknowledged = true;
                break;
            case "--help":
            case "-h":
                help = true;
                break;
            default:
                options = default;
                error = $"Unknown argument: {argument}";
                return false;
        }
    }

    options = new CliOptions(
        devicePath,
        scenario,
        durationSeconds,
        maxFrames,
        outputPath,
        acknowledged,
        help);
    error = null;
    return true;
}

static bool TryTakeValue(
    string[] arguments,
    ref int index,
    string option,
    out string? value,
    out string? error)
{
    if (++index >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index]))
    {
        value = null;
        error = $"{option} requires a value.";
        return false;
    }

    value = arguments[index];
    error = null;
    return true;
}

static bool TryTakeInt(
    string[] arguments,
    ref int index,
    string option,
    out int value,
    out string? error)
{
    if (!TryTakeValue(arguments, ref index, option, out string? text, out error))
    {
        value = 0;
        return false;
    }

    if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value))
    {
        error = $"{option} requires a base-10 integer.";
        return false;
    }

    return true;
}

static void WriteInspectionErrors(IReadOnlyList<HidInspectionError> errors)
{
    foreach (HidInspectionError error in errors)
    {
        Console.Error.WriteLine(
            error.NativeErrorCode.HasValue
                ? $"  {error.Operation}: {error.Message} (code {error.NativeErrorCode.Value})"
                : $"  {error.Operation}: {error.Message}");
    }
}

static void WriteUsage(TextWriter writer)
{
    writer.WriteLine("ReportRecorder - explicit, bounded raw HID input research capture");
    writer.WriteLine();
    writer.WriteLine(
        "Usage: ReportRecorder --device-path <path> --scenario <name> --duration-seconds <1-600>");
    writer.WriteLine(
        "       --max-frames <1-10000> --output <fixture.json> --acknowledge-explicit-capture");
    writer.WriteLine();
    writer.WriteLine("The exact path must be present, readable, and expose a bounded input report length.");
    writer.WriteLine("No HID output reports are generated.");
}

internal readonly record struct CliOptions(
    string? DevicePath,
    string? Scenario,
    int DurationSeconds,
    int MaxFrames,
    string? OutputPath,
    bool Acknowledged,
    bool Help);
