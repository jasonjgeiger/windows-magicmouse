using MagicMouseWindows.Hid;

namespace MagicMouseWindows.Hid.Tests;

[TestClass]
public sealed class HidCaptureEligibilityTests
{
    [TestMethod]
    public void AllowsAppleMouseCollection()
    {
        HidCaptureAssessment result = HidCaptureEligibility.Assess(
            CreateDevice(usagePage: 0x0001, usage: 0x0002, isApple: true));

        Assert.IsTrue(result.IsAllowed);
    }

    [TestMethod]
    public void AllowsAppleVendorDefinedCollection()
    {
        HidCaptureAssessment result = HidCaptureEligibility.Assess(
            CreateDevice(usagePage: 0xFF00, usage: 0x0001, isApple: true));

        Assert.IsTrue(result.IsAllowed);
    }

    [TestMethod]
    [DataRow(0x0001, 0x0006)]
    [DataRow(0x0001, 0x0007)]
    [DataRow(0x000C, 0x0001)]
    public void RejectsKeyboardKeypadAndConsumerCollections(int usagePage, int usage)
    {
        HidCaptureAssessment result = HidCaptureEligibility.Assess(
            CreateDevice((ushort)usagePage, (ushort)usage, isApple: true));

        Assert.IsFalse(result.IsAllowed);
    }

    [TestMethod]
    public void RejectsNonAppleMouseCollection()
    {
        HidCaptureAssessment result = HidCaptureEligibility.Assess(
            CreateDevice(usagePage: 0x0001, usage: 0x0002, isApple: false));

        Assert.IsFalse(result.IsAllowed);
    }

    [TestMethod]
    public void RejectsMissingUsageMetadata()
    {
        HidCaptureAssessment result = HidCaptureEligibility.Assess(
            CreateDevice(usagePage: null, usage: null, isApple: true));

        Assert.IsFalse(result.IsAllowed);
    }

    private static HidDeviceInfo CreateDevice(
        ushort? usagePage,
        ushort? usage,
        bool isApple)
    {
        return new HidDeviceInfo(
            DevicePath: @"\\?\hid#test",
            VendorId: isApple ? (ushort)0x05AC : (ushort)0x1234,
            ProductId: 1,
            VersionNumber: 1,
            Manufacturer: isApple ? "Apple Inc." : "Test",
            Product: "Test HID",
            SerialNumber: null,
            HardwareIds: [],
            CompatibleIds: [],
            UsagePage: usagePage,
            Usage: usage,
            InputReportByteLength: 64,
            OutputReportByteLength: 0,
            FeatureReportByteLength: 0,
            CanOpenForRead: true,
            Candidate: new HidCandidateAssessment(
                IsAppleDevice: isApple,
                IsCandidate: true,
                Classification: "Test",
                Evidence: []),
            Errors: []);
    }
}
