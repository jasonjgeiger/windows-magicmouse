namespace MagicMouseWindows.Hid;

public sealed record HidCaptureAssessment(
    bool IsAllowed,
    string Reason);

public static class HidCaptureEligibility
{
    private const ushort GenericDesktopUsagePage = 0x0001;
    private const ushort PointerUsage = 0x0001;
    private const ushort MouseUsage = 0x0002;
    private const ushort KeyboardUsage = 0x0006;
    private const ushort KeypadUsage = 0x0007;
    private const ushort ConsumerUsagePage = 0x000C;
    private const ushort FirstVendorDefinedUsagePage = 0xFF00;

    public static HidCaptureAssessment Assess(HidDeviceInfo device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!device.Candidate.IsAppleDevice)
        {
            return new HidCaptureAssessment(false, "The interface is not identified as Apple hardware.");
        }

        if (!device.UsagePage.HasValue || !device.Usage.HasValue)
        {
            return new HidCaptureAssessment(false, "The interface has no usable HID usage metadata.");
        }

        if (device.UsagePage == ConsumerUsagePage)
        {
            return new HidCaptureAssessment(false, "Consumer-control interfaces are excluded from capture.");
        }

        if (device.UsagePage == GenericDesktopUsagePage &&
            device.Usage is KeyboardUsage or KeypadUsage)
        {
            return new HidCaptureAssessment(false, "Keyboard and keypad interfaces are excluded from capture.");
        }

        bool isPointingCollection =
            device.UsagePage == GenericDesktopUsagePage &&
            device.Usage is PointerUsage or MouseUsage;
        bool isVendorDefinedCollection =
            device.UsagePage >= FirstVendorDefinedUsagePage;

        return isPointingCollection || isVendorDefinedCollection
            ? new HidCaptureAssessment(true, "Apple pointing or vendor-defined HID collection.")
            : new HidCaptureAssessment(false, "The HID usage is not a pointing or vendor-defined collection.");
    }
}
