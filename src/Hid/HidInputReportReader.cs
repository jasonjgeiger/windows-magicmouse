using System.ComponentModel;
using MagicMouseWindows.Contracts.Protocol;
using Microsoft.Win32.SafeHandles;

namespace MagicMouseWindows.Hid;

public sealed class HidInputReportReader : IAsyncDisposable
{
    private readonly FileStream stream;
    private bool disposed;

    private HidInputReportReader(
        HidDeviceInfo device,
        int reportLength,
        FileStream stream)
    {
        Device = device;
        ReportLength = reportLength;
        this.stream = stream;
    }

    public HidDeviceInfo Device { get; }

    public int ReportLength { get; }

    public static HidInputReportReader Open(string devicePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(devicePath);

        HidInspectionResult inspection = HidDeviceInspector.Inspect();
        HidDeviceInfo[] matches = inspection.Devices
            .Where(device => string.Equals(
                device.DevicePath,
                devicePath,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            string detail = inspection.Errors.Count == 0
                ? "The exact device path is not present."
                : $"The exact device path is not present. Inspection error: {inspection.Errors[0].Message}";
            throw new HidInputOpenException(devicePath, detail);
        }

        if (matches.Length > 1)
        {
            throw new HidInputOpenException(
                devicePath,
                "The exact device path matched more than one present HID interface.");
        }

        return Open(matches[0]);
    }

    public static HidInputReportReader Open(HidDeviceInfo device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!device.InputReportByteLength.HasValue)
        {
            throw new HidInputOpenException(
                device.DevicePath,
                "The inspected HID interface did not expose an input report length.");
        }

        int reportLength = device.InputReportByteLength.Value;
        if (reportLength is <= 0 or > ProtocolLimits.MaxReportBytes)
        {
            throw new HidInputOpenException(
                device.DevicePath,
                $"The inspected report length must be between 1 and {ProtocolLimits.MaxReportBytes} bytes.");
        }

        if (!device.CanOpenForRead)
        {
            HidInspectionError? openError = device.Errors
                .FirstOrDefault(error => error.Operation == "OpenForRead");
            string detail = openError is null
                ? "The inspected HID interface cannot be opened for read access."
                : $"The inspected HID interface cannot be opened for read access: {openError.Message}";
            throw new HidInputOpenException(device.DevicePath, detail, openError?.NativeErrorCode);
        }

        SafeFileHandle handle = NativeMethods.CreateFile(
            device.DevicePath,
            NativeMethods.GenericRead,
            NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
            IntPtr.Zero,
            NativeMethods.OpenExisting,
            NativeMethods.FileFlagOverlapped,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new HidInputOpenException(
                device.DevicePath,
                new Win32Exception(error).Message,
                error);
        }

        try
        {
            var stream = new FileStream(handle, FileAccess.Read, bufferSize: 0, isAsync: true);
            return new HidInputReportReader(device, reportLength, stream);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public async ValueTask<byte[]> ReadReportAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var report = new byte[ReportLength];

        try
        {
            int bytesRead = await stream
                .ReadAsync(report.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new HidInputReadException(
                    Device.DevicePath,
                    "The device returned end-of-stream, which commonly indicates removal.");
            }

            return report.AsSpan(0, bytesRead).ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HidInputReadException)
        {
            throw;
        }
        catch (IOException exception)
        {
            throw new HidInputReadException(
                Device.DevicePath,
                $"Reading the HID input report failed, possibly because the device was removed: {exception.Message}",
                exception);
        }

    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        await stream.DisposeAsync().ConfigureAwait(false);
    }
}

public sealed class HidInputOpenException : IOException
{
    public HidInputOpenException(
        string devicePath,
        string message,
        int? nativeErrorCode = null)
        : base(FormatMessage(devicePath, message, nativeErrorCode))
    {
        DevicePath = devicePath;
        NativeErrorCode = nativeErrorCode;
    }

    public string DevicePath { get; }

    public int? NativeErrorCode { get; }

    private static string FormatMessage(
        string devicePath,
        string message,
        int? nativeErrorCode) =>
        nativeErrorCode.HasValue
            ? $"{message} (Win32 error {nativeErrorCode.Value}) Device path: {devicePath}"
            : $"{message} Device path: {devicePath}";
}

public sealed class HidInputReadException : IOException
{
    public HidInputReadException(
        string devicePath,
        string message,
        Exception? innerException = null)
        : base($"{message} Device path: {devicePath}", innerException)
    {
        DevicePath = devicePath;
    }

    public string DevicePath { get; }
}
