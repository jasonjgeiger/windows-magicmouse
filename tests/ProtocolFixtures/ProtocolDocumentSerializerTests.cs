using System.Text;
using MagicMouseWindows.Contracts.Protocol;

namespace MagicMouseWindows.ProtocolFixtures;

[TestClass]
public sealed class ProtocolDocumentSerializerTests
{
    private const string DescriptorHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [TestMethod]
    [TestProperty("Requirements", "SR-004, SR-008")]
    public void FixtureRoundTripPreservesContract()
    {
        var fixture = CreateFixture();

        var serialized = ProtocolDocumentSerializer.SerializeFixture(fixture);
        var deserialized = ProtocolDocumentSerializer.DeserializeFixture(serialized);

        Assert.AreEqual(fixture.SchemaVersion, deserialized.SchemaVersion);
        Assert.AreEqual(fixture.Profile, deserialized.Profile);
        Assert.AreEqual(fixture.Metadata, deserialized.Metadata);
        Assert.HasCount(2, deserialized.Frames);
        CollectionAssert.AreEqual(
            fixture.Frames[0].Raw.Report,
            deserialized.Frames[0].Raw.Report);
    }

    [TestMethod]
    [TestProperty("Requirements", "SR-004")]
    public void FixtureRejectsUnknownFields()
    {
        var json = Encoding.UTF8.GetString(
            ProtocolDocumentSerializer.SerializeFixture(CreateFixture()));
        json = json.Replace(
            "\"schemaVersion\": 1,",
            "\"schemaVersion\": 1, \"unexpected\": true,",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.DeserializeFixture(Encoding.UTF8.GetBytes(json)));

        StringAssert.Contains(exception.Message, "unexpected");
    }

    [TestMethod]
    [TestProperty("Requirements", "SR-004")]
    public void FixtureRejectsUnsupportedSchemaVersion()
    {
        var fixture = CreateFixture() with { SchemaVersion = 2 };

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.SerializeFixture(fixture));

        StringAssert.Contains(exception.Message, "unsupported fixture schema version");
    }

    [TestMethod]
    [TestProperty("Requirements", "SR-002, SR-008")]
    public void FixtureRejectsOversizedReports()
    {
        var fixture = CreateFixture();
        var oversizedFrame = fixture.Frames[0] with
        {
            Raw = fixture.Frames[0].Raw with
            {
                Report = new byte[ProtocolLimits.MaxReportBytes + 1],
            },
        };
        var invalid = fixture with { Frames = [oversizedFrame] };

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.SerializeFixture(invalid));

        StringAssert.Contains(exception.Message, "$.frames[0].raw.report");
    }

    [TestMethod]
    [TestProperty("Requirements", "GR-009, SR-008")]
    public void FixtureRejectsNonMonotonicFrames()
    {
        var fixture = CreateFixture();
        var invalid = fixture with
        {
            Frames =
            [
                fixture.Frames[0],
                fixture.Frames[1] with
                {
                    Raw = fixture.Frames[1].Raw with { TimestampMicroseconds = 50 },
                },
            ],
        };

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.SerializeFixture(invalid));

        StringAssert.Contains(exception.Message, "timestamps must be monotonic");
    }

    [TestMethod]
    [TestProperty("Requirements", "SR-004, SR-008")]
    public void FixtureRejectsDocumentsAboveSizeLimit()
    {
        var oversized = new byte[ProtocolLimits.MaxDocumentBytes + 1];

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.DeserializeFixture(oversized));

        StringAssert.Contains(exception.Message, "must contain between 1");
    }

    [TestMethod]
    [TestProperty("Requirements", "SR-002")]
    public void FixtureRejectsNullReportData()
    {
        var json = Encoding.UTF8.GetString(
            ProtocolDocumentSerializer.SerializeFixture(CreateFixture()));
        json = json.Replace(
            "\"report\": \"AgoU\"",
            "\"report\": null",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.DeserializeFixture(Encoding.UTF8.GetBytes(json)));

        StringAssert.Contains(exception.Message, "$.frames[0].raw.report");
    }

    [TestMethod]
    [TestProperty("Requirements", "FR-011, SR-001, SR-004")]
    public void ProfileRoundTripPreservesContract()
    {
        var profile = CreateProfile();

        var serialized = ProtocolDocumentSerializer.SerializeProfile(profile);
        var deserialized = ProtocolDocumentSerializer.DeserializeProfile(serialized);

        Assert.AreEqual(profile.ProfileId, deserialized.ProfileId);
        Assert.AreEqual(profile.Status, deserialized.Status);
        Assert.HasCount(1, deserialized.DeviceMatches);
        Assert.HasCount(1, deserialized.TouchReports);
        Assert.HasCount(2, deserialized.Fields);
    }

    [TestMethod]
    [TestProperty("Requirements", "SR-001, SR-002")]
    public void ProfileRejectsInvalidDescriptorHash()
    {
        var profile = CreateProfile();
        var invalid = profile with
        {
            DeviceMatches =
            [
                profile.DeviceMatches[0] with
                {
                    DescriptorSignatureSha256 = "not-a-hash",
                },
            ],
        };

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.SerializeProfile(invalid));

        StringAssert.Contains(exception.Message, "64 hexadecimal characters");
    }

    [TestMethod]
    [TestProperty("Requirements", "SR-002")]
    public void ProfileRejectsDuplicateReportIds()
    {
        var profile = CreateProfile();
        var invalid = profile with
        {
            TouchReports =
            [
                new ReportShape(2, 64),
                new ReportShape(2, 128),
            ],
        };

        var exception = Assert.Throws<ProtocolContractException>(
            () => ProtocolDocumentSerializer.SerializeProfile(invalid));

        StringAssert.Contains(exception.Message, "duplicate report ID");
    }

    private static CaptureFixture CreateFixture()
    {
        var descriptor = new DescriptorIdentity(
            VendorId: 0x05AC,
            ProductId: 0x0001,
            VersionNumber: 1,
            HardwareId: "HID\\VID_05AC&PID_0001",
            CompatibleId: "HID_DEVICE_SYSTEM_MOUSE",
            Transport: "Bluetooth",
            DescriptorSignatureSha256: DescriptorHash);

        return new CaptureFixture(
            SchemaVersion: ProtocolLimits.CurrentFixtureSchemaVersion,
            Profile: new DeviceProfileIdentity(
                ProfileId: "magic-mouse-2-lightning-research",
                SchemaVersion: ProtocolLimits.CurrentProfileSchemaVersion,
                DescriptorSignatureSha256: DescriptorHash),
            Metadata: new CaptureMetadata(
                Scenario: "slow-vertical-positive",
                DeviceModel: "Magic Mouse 2 with Lightning",
                Descriptor: descriptor,
                Environment: new CaptureEnvironment(
                    WindowsVersion: "Windows 11",
                    Architecture: "x64",
                    BluetoothAdapter: "Test adapter",
                    FirmwareObservation: string.Empty),
                Provenance: new CaptureProvenance(
                    ToolName: "ReportRecorder",
                    ToolVersion: "0.1.0",
                    Method: "Explicit bounded hardware capture",
                    Statement: "Captured from the selected device for interoperability research.")),
            Frames:
            [
                new FixtureFrame(
                    Raw: new RawTouchFrame(
                        TimestampMicroseconds: 100,
                        Sequence: 0,
                        ReportId: 2,
                        Report: [2, 10, 20]),
                    ExpectedContacts:
                    [
                        new NormalizedContact(0, ContactPhase.Down, 0.25, 0.5, null),
                    ],
                    ExpectedOutput: []),
                new FixtureFrame(
                    Raw: new RawTouchFrame(
                        TimestampMicroseconds: 200,
                        Sequence: 1,
                        ReportId: 2,
                        Report: [2, 10, 25]),
                    ExpectedContacts:
                    [
                        new NormalizedContact(0, ContactPhase.Move, 0.25, 0.6, null),
                    ],
                    ExpectedOutput:
                    [
                        new ExpectedScrollCommand(200, 1, 0),
                    ]),
            ]);
    }

    private static DeviceProfileDocument CreateProfile()
    {
        return new DeviceProfileDocument(
            SchemaVersion: ProtocolLimits.CurrentProfileSchemaVersion,
            ProfileId: "magic-mouse-2-lightning-research",
            DisplayName: "Magic Mouse 2 with Lightning research profile",
            Status: CompatibilityStatus.Captured,
            DeviceMatches:
            [
                new DeviceMatch(
                    VendorId: 0x05AC,
                    ProductId: 0x0001,
                    HardwareId: "HID\\VID_05AC&PID_0001",
                    CompatibleId: "HID_DEVICE_SYSTEM_MOUSE",
                    DescriptorSignatureSha256: DescriptorHash),
            ],
            TouchReports:
            [
                new ReportShape(ReportId: 2, ExactLength: 64),
            ],
            MaxContacts: 16,
            Coordinates: new CoordinateRange(
                MinimumX: 0,
                MaximumX: 4095,
                MinimumY: 0,
                MaximumY: 4095,
                Origin: "Unknown",
                Orientation: "Unknown"),
            InitializationState: "Unknown",
            Fields:
            [
                new DecodedField("contactState", FieldConfidence.Hypothesis, "Requires captures."),
                new DecodedField("coordinates", FieldConfidence.Unknown, "Not decoded."),
            ]);
    }
}
