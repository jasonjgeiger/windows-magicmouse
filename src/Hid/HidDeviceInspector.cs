using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MagicMouseWindows.Hid;

public static class HidDeviceInspector
{
    private const int MaximumDevices = 4096;
    private const int MaximumDetailBytes = 64 * 1024;
    private const int MaximumPropertyBytes = 64 * 1024;
    private const int MaximumPropertyStringCharacters = 4096;
    private const int MaximumHidStringCharacters = 256;
    private const int MaximumMultiStringValues = 64;
    private const int MaximumEvidenceValues = 8;

    public static HidInspectionResult Inspect()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new HidInspectionResult(
                [],
                [new HidInspectionError("Platform", null, "HID inspection is supported only on Windows.")]);
        }

        var devices = new List<HidDeviceInfo>();
        var errors = new List<HidInspectionError>();

        NativeMethods.HidD_GetHidGuid(out Guid hidGuid);
        using SafeDeviceInfoSet deviceInfoSet = NativeMethods.SetupDiGetClassDevs(
            ref hidGuid,
            null,
            IntPtr.Zero,
            NativeMethods.DigcfPresent | NativeMethods.DigcfDeviceInterface);

        if (deviceInfoSet.IsInvalid)
        {
            AddWin32Error(errors, "SetupDiGetClassDevs", Marshal.GetLastWin32Error());
            return new HidInspectionResult(devices, errors);
        }

        for (uint index = 0; index < MaximumDevices; index++)
        {
            var interfaceData = NativeMethods.SpDeviceInterfaceData.Create();
            if (!NativeMethods.SetupDiEnumDeviceInterfaces(
                    deviceInfoSet,
                    IntPtr.Zero,
                    ref hidGuid,
                    index,
                    ref interfaceData))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.ErrorNoMoreItems)
                {
                    AddWin32Error(errors, "SetupDiEnumDeviceInterfaces", error);
                }

                break;
            }

            HidDeviceInfo? device = InspectDevice(deviceInfoSet, ref interfaceData, index, errors);
            if (device is not null)
            {
                devices.Add(device);
            }
        }

        if (devices.Count == MaximumDevices)
        {
            errors.Add(new HidInspectionError(
                "EnumerationLimit",
                null,
                $"Enumeration stopped at the safety limit of {MaximumDevices} HID interfaces."));
        }

        return new HidInspectionResult(devices, errors);
    }

    private static HidDeviceInfo? InspectDevice(
        SafeDeviceInfoSet deviceInfoSet,
        ref NativeMethods.SpDeviceInterfaceData interfaceData,
        uint index,
        List<HidInspectionError> enumerationErrors)
    {
        var errors = new List<HidInspectionError>();
        var deviceInfoData = NativeMethods.SpDevinfoData.Create();

        _ = NativeMethods.SetupDiGetDeviceInterfaceDetail(
            deviceInfoSet,
            ref interfaceData,
            IntPtr.Zero,
            0,
            out uint requiredSize,
            ref deviceInfoData);

        int sizeError = Marshal.GetLastWin32Error();
        if (requiredSize == 0 ||
            requiredSize > MaximumDetailBytes ||
            sizeError != NativeMethods.ErrorInsufficientBuffer)
        {
            string message = requiredSize > MaximumDetailBytes
                ? $"Device interface detail requires {requiredSize} bytes, exceeding the {MaximumDetailBytes}-byte limit."
                : new Win32Exception(sizeError).Message;
            enumerationErrors.Add(new HidInspectionError(
                $"SetupDiGetDeviceInterfaceDetail[{index}]",
                sizeError == 0 ? null : sizeError,
                message));
            return null;
        }

        IntPtr detailBuffer = Marshal.AllocHGlobal(checked((int)requiredSize));
        try
        {
            Marshal.WriteInt32(detailBuffer, IntPtr.Size == 8 ? 8 : 6);
            if (!NativeMethods.SetupDiGetDeviceInterfaceDetail(
                    deviceInfoSet,
                    ref interfaceData,
                    detailBuffer,
                    requiredSize,
                    out _,
                    ref deviceInfoData))
            {
                AddWin32Error(
                    enumerationErrors,
                    $"SetupDiGetDeviceInterfaceDetail[{index}]",
                    Marshal.GetLastWin32Error());
                return null;
            }

            string? devicePath = Marshal.PtrToStringUni(IntPtr.Add(detailBuffer, sizeof(int)));
            if (string.IsNullOrWhiteSpace(devicePath))
            {
                enumerationErrors.Add(new HidInspectionError(
                    $"DevicePath[{index}]",
                    null,
                    "SetupAPI returned an empty HID device path."));
                return null;
            }

            IReadOnlyList<string> hardwareIds = ReadMultiStringProperty(
                deviceInfoSet,
                ref deviceInfoData,
                NativeMethods.SpdrpHardwareId,
                "HardwareIds",
                errors);
            IReadOnlyList<string> compatibleIds = ReadMultiStringProperty(
                deviceInfoSet,
                ref deviceInfoData,
                NativeMethods.SpdrpCompatibleIds,
                "CompatibleIds",
                errors);

            return ReadHidMetadata(devicePath, hardwareIds, compatibleIds, errors);
        }
        finally
        {
            Marshal.FreeHGlobal(detailBuffer);
        }
    }

    private static HidDeviceInfo ReadHidMetadata(
        string devicePath,
        IReadOnlyList<string> hardwareIds,
        IReadOnlyList<string> compatibleIds,
        List<HidInspectionError> errors)
    {
        ushort? vendorId = null;
        ushort? productId = null;
        ushort? versionNumber = null;
        string? manufacturer = null;
        string? product = null;
        string? serialNumber = null;
        ushort? usagePage = null;
        ushort? usage = null;
        ushort? inputReportLength = null;
        ushort? outputReportLength = null;
        ushort? featureReportLength = null;

        using SafeFileHandle metadataHandle = NativeMethods.CreateFile(
            devicePath,
            0,
            NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
            IntPtr.Zero,
            NativeMethods.OpenExisting,
            0,
            IntPtr.Zero);

        if (metadataHandle.IsInvalid)
        {
            AddWin32Error(errors, "OpenForMetadata", Marshal.GetLastWin32Error());
        }
        else
        {
            var attributes = NativeMethods.HiddAttributes.Create();
            if (NativeMethods.HidD_GetAttributes(metadataHandle, ref attributes))
            {
                vendorId = attributes.VendorId;
                productId = attributes.ProductId;
                versionNumber = attributes.VersionNumber;
            }
            else
            {
                AddWin32Error(errors, "HidD_GetAttributes", Marshal.GetLastWin32Error());
            }

            manufacturer = ReadHidString(metadataHandle, NativeMethods.HidD_GetManufacturerString);
            product = ReadHidString(metadataHandle, NativeMethods.HidD_GetProductString);
            serialNumber = ReadHidString(metadataHandle, NativeMethods.HidD_GetSerialNumberString);

            if (NativeMethods.HidD_GetPreparsedData(metadataHandle, out IntPtr preparsedData))
            {
                try
                {
                    int status = NativeMethods.HidP_GetCaps(preparsedData, out NativeMethods.HidpCaps caps);
                    if (status == NativeMethods.HidpStatusSuccess)
                    {
                        usagePage = caps.UsagePage;
                        usage = caps.Usage;
                        inputReportLength = caps.InputReportByteLength;
                        outputReportLength = caps.OutputReportByteLength;
                        featureReportLength = caps.FeatureReportByteLength;
                    }
                    else
                    {
                        errors.Add(new HidInspectionError(
                            "HidP_GetCaps",
                            status,
                            $"HidP_GetCaps failed with NTSTATUS 0x{status:X8}."));
                    }
                }
                finally
                {
                    if (!NativeMethods.HidD_FreePreparsedData(preparsedData))
                    {
                        AddWin32Error(errors, "HidD_FreePreparsedData", Marshal.GetLastWin32Error());
                    }
                }
            }
            else
            {
                AddWin32Error(errors, "HidD_GetPreparsedData", Marshal.GetLastWin32Error());
            }
        }

        bool canOpenForRead;
        using (SafeFileHandle readHandle = NativeMethods.CreateFile(
                   devicePath,
                   NativeMethods.GenericRead,
                   NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
                   IntPtr.Zero,
                   NativeMethods.OpenExisting,
                   0,
                   IntPtr.Zero))
        {
            canOpenForRead = !readHandle.IsInvalid;
            if (!canOpenForRead)
            {
                AddWin32Error(errors, "OpenForRead", Marshal.GetLastWin32Error());
            }
        }

        HidCandidateAssessment candidate = AssessCandidate(
            vendorId,
            manufacturer,
            product,
            hardwareIds,
            compatibleIds,
            usagePage,
            usage);

        return new HidDeviceInfo(
            devicePath,
            vendorId,
            productId,
            versionNumber,
            manufacturer,
            product,
            serialNumber,
            hardwareIds,
            compatibleIds,
            usagePage,
            usage,
            inputReportLength,
            outputReportLength,
            featureReportLength,
            canOpenForRead,
            candidate,
            errors);
    }

    private static string[] ReadMultiStringProperty(
        SafeDeviceInfoSet deviceInfoSet,
        ref NativeMethods.SpDevinfoData deviceInfoData,
        uint property,
        string operation,
        List<HidInspectionError> errors)
    {
        var buffer = new byte[MaximumPropertyBytes];
        if (!NativeMethods.SetupDiGetDeviceRegistryProperty(
                deviceInfoSet,
                ref deviceInfoData,
                property,
                out uint registryDataType,
                buffer,
                (uint)buffer.Length,
                out uint requiredSize))
        {
            int error = Marshal.GetLastWin32Error();
            if (error != NativeMethods.ErrorInvalidData)
            {
                string message = error == NativeMethods.ErrorInsufficientBuffer
                    ? $"Property requires {requiredSize} bytes, exceeding the {MaximumPropertyBytes}-byte limit."
                    : new Win32Exception(error).Message;
                errors.Add(new HidInspectionError(operation, error, message));
            }

            return [];
        }

        if (registryDataType is not (NativeMethods.RegMultiSz or NativeMethods.RegSz))
        {
            errors.Add(new HidInspectionError(
                operation,
                null,
                $"SetupAPI returned unsupported registry data type {registryDataType}."));
            return [];
        }

        int byteCount = Math.Min(checked((int)requiredSize), buffer.Length);
        string value = System.Text.Encoding.Unicode.GetString(buffer, 0, byteCount);
        string[] parsed = value
            .Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static item => item.Length > 0)
            .Take(MaximumMultiStringValues)
            .ToArray();

        if (parsed.Any(static item => item.Length > MaximumPropertyStringCharacters))
        {
            errors.Add(new HidInspectionError(
                operation,
                null,
                $"One or more values were truncated to {MaximumPropertyStringCharacters} characters."));
        }

        return parsed
            .Select(static item => item.Length <= MaximumPropertyStringCharacters
                ? item
                : item[..MaximumPropertyStringCharacters])
            .ToArray();
    }

    private static string? ReadHidString(
        SafeFileHandle handle,
        NativeMethods.HidStringReader reader)
    {
        IntPtr buffer = Marshal.AllocHGlobal(MaximumHidStringCharacters * sizeof(char));
        try
        {
            int bufferLength = MaximumHidStringCharacters * sizeof(char);
            Marshal.Copy(new byte[bufferLength], 0, buffer, bufferLength);
            if (!reader(handle, buffer, bufferLength))
            {
                return null;
            }

            return Marshal.PtrToStringUni(buffer)?.TrimEnd('\0');
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static HidCandidateAssessment AssessCandidate(
        ushort? vendorId,
        string? manufacturer,
        string? product,
        IReadOnlyList<string> hardwareIds,
        IReadOnlyList<string> compatibleIds,
        ushort? usagePage,
        ushort? usage)
    {
        var evidence = new List<string>(MaximumEvidenceValues);
        bool idSaysApple = hardwareIds
            .Concat(compatibleIds)
            .Any(static id => id.Contains("VID_05AC", StringComparison.OrdinalIgnoreCase));
        bool isApple = vendorId == 0x05AC ||
            idSaysApple ||
            manufacturer?.Contains("Apple", StringComparison.OrdinalIgnoreCase) == true;

        if (vendorId == 0x05AC)
        {
            evidence.Add("Apple USB vendor ID 05AC");
        }
        else if (idSaysApple)
        {
            evidence.Add("SetupAPI ID contains VID_05AC");
        }
        else if (manufacturer?.Contains("Apple", StringComparison.OrdinalIgnoreCase) == true)
        {
            evidence.Add("Manufacturer string contains Apple");
        }

        bool namedMagicMouse =
            product?.Contains("Magic Mouse", StringComparison.OrdinalIgnoreCase) == true;
        bool pointingUsage = usagePage == 0x0001 && usage is 0x0001 or 0x0002;
        bool vendorDefinedUsage = usagePage >= 0xFF00;

        if (namedMagicMouse)
        {
            evidence.Add("Product string contains Magic Mouse");
        }

        if (pointingUsage)
        {
            evidence.Add($"Generic Desktop pointing usage 0x{usage:X4}");
        }

        if (vendorDefinedUsage)
        {
            evidence.Add($"Vendor-defined usage page 0x{usagePage:X4}");
        }

        bool isCandidate = isApple && (namedMagicMouse || pointingUsage || vendorDefinedUsage);
        string classification = !isApple
            ? "NotApple"
            : namedMagicMouse
                ? "NamedMagicMouseCandidate"
                : pointingUsage
                    ? "ApplePointingDeviceCandidate"
                    : vendorDefinedUsage
                        ? "AppleVendorDefinedCandidate"
                        : "AppleNonCandidateHid";

        return new HidCandidateAssessment(
            isApple,
            isCandidate,
            classification,
            evidence.Take(MaximumEvidenceValues).ToArray());
    }

    private static void AddWin32Error(
        List<HidInspectionError> errors,
        string operation,
        int error)
    {
        errors.Add(new HidInspectionError(operation, error, new Win32Exception(error).Message));
    }
}
