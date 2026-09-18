using System.Globalization;

namespace MagicMouseWindows.Contracts.Protocol;

public static class ProtocolContractValidator
{
    public static void Validate(CaptureFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        Require(
            fixture.SchemaVersion == ProtocolLimits.CurrentFixtureSchemaVersion,
            "$.schemaVersion",
            $"unsupported fixture schema version {fixture.SchemaVersion}");

        ValidateProfileIdentity(fixture.Profile, "$.profile");
        ValidateMetadata(fixture.Metadata, "$.metadata");
        RequireCount(fixture.Frames, ProtocolLimits.MaxFrames, "$.frames");

        long previousTimestamp = -1;
        long previousSequence = -1;

        for (var index = 0; index < fixture.Frames.Count; index++)
        {
            var path = $"$.frames[{index}]";
            var frame = fixture.Frames[index]
                ?? throw new ProtocolContractException(path, "frame is required");

            ValidateRawFrame(frame.Raw, $"{path}.raw");
            Require(
                frame.Raw.TimestampMicroseconds >= previousTimestamp,
                $"{path}.raw.timestampMicroseconds",
                "timestamps must be monotonic");
            Require(
                frame.Raw.Sequence > previousSequence,
                $"{path}.raw.sequence",
                "sequences must be strictly increasing");

            previousTimestamp = frame.Raw.TimestampMicroseconds;
            previousSequence = frame.Raw.Sequence;

            RequireCount(
                frame.ExpectedContacts,
                ProtocolLimits.MaxContactsPerFrame,
                $"{path}.expectedContacts");
            RequireCount(
                frame.ExpectedOutput,
                ProtocolLimits.MaxOutputCommandsPerFrame,
                $"{path}.expectedOutput");

            for (var contactIndex = 0; contactIndex < frame.ExpectedContacts.Count; contactIndex++)
            {
                ValidateContact(
                    frame.ExpectedContacts[contactIndex],
                    $"{path}.expectedContacts[{contactIndex}]");
            }

            for (var outputIndex = 0; outputIndex < frame.ExpectedOutput.Count; outputIndex++)
            {
                ValidateOutput(
                    frame.ExpectedOutput[outputIndex],
                    $"{path}.expectedOutput[{outputIndex}]",
                    frame.Raw.TimestampMicroseconds);
            }
        }
    }

    public static void Validate(DeviceProfileDocument profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        Require(
            profile.SchemaVersion == ProtocolLimits.CurrentProfileSchemaVersion,
            "$.schemaVersion",
            $"unsupported profile schema version {profile.SchemaVersion}");
        RequireText(profile.ProfileId, "$.profileId");
        RequireText(profile.DisplayName, "$.displayName");
        RequireCount(
            profile.DeviceMatches,
            ProtocolLimits.MaxDeviceMatches,
            "$.deviceMatches",
            requireNonEmpty: true);
        RequireCount(
            profile.TouchReports,
            ProtocolLimits.MaxReportShapes,
            "$.touchReports",
            requireNonEmpty: true);
        Require(
            profile.MaxContacts is > 0 and <= ProtocolLimits.MaxContactsPerFrame,
            "$.maxContacts",
            $"must be between 1 and {ProtocolLimits.MaxContactsPerFrame}");
        ValidateCoordinateRange(profile.Coordinates, "$.coordinates");
        RequireText(profile.InitializationState, "$.initializationState");
        RequireCount(profile.Fields, ProtocolLimits.MaxDecodedFields, "$.fields");

        var descriptorSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < profile.DeviceMatches.Count; index++)
        {
            var path = $"$.deviceMatches[{index}]";
            var match = profile.DeviceMatches[index]
                ?? throw new ProtocolContractException(path, "device match is required");
            Require(match.VendorId != 0, $"{path}.vendorId", "must be non-zero");
            Require(match.ProductId != 0, $"{path}.productId", "must be non-zero");
            RequireText(match.HardwareId, $"{path}.hardwareId");
            RequireText(match.CompatibleId, $"{path}.compatibleId");
            RequireSha256(
                match.DescriptorSignatureSha256,
                $"{path}.descriptorSignatureSha256");
            Require(
                descriptorSignatures.Add(match.DescriptorSignatureSha256),
                $"{path}.descriptorSignatureSha256",
                "duplicate descriptor signature");
        }

        var reportIds = new HashSet<byte>();
        for (var index = 0; index < profile.TouchReports.Count; index++)
        {
            var path = $"$.touchReports[{index}]";
            var report = profile.TouchReports[index]
                ?? throw new ProtocolContractException(path, "report shape is required");
            Require(reportIds.Add(report.ReportId), $"{path}.reportId", "duplicate report ID");
            Require(
                report.ExactLength is > 0 and <= ProtocolLimits.MaxReportBytes,
                $"{path}.exactLength",
                $"must be between 1 and {ProtocolLimits.MaxReportBytes}");
        }

        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < profile.Fields.Count; index++)
        {
            var path = $"$.fields[{index}]";
            var field = profile.Fields[index]
                ?? throw new ProtocolContractException(path, "decoded field is required");
            RequireText(field.Name, $"{path}.name");
            RequireText(field.Notes, $"{path}.notes", allowEmpty: true);
            Require(fieldNames.Add(field.Name), $"{path}.name", "duplicate field name");
        }
    }

    private static void ValidateProfileIdentity(DeviceProfileIdentity identity, string path)
    {
        if (identity is null)
        {
            throw new ProtocolContractException(path, "profile identity is required");
        }

        RequireText(identity.ProfileId, $"{path}.profileId");
        Require(
            identity.SchemaVersion == ProtocolLimits.CurrentProfileSchemaVersion,
            $"{path}.schemaVersion",
            $"unsupported profile schema version {identity.SchemaVersion}");
        RequireSha256(
            identity.DescriptorSignatureSha256,
            $"{path}.descriptorSignatureSha256");
    }

    private static void ValidateMetadata(CaptureMetadata metadata, string path)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        RequireText(metadata.Scenario, $"{path}.scenario");
        RequireText(metadata.DeviceModel, $"{path}.deviceModel");
        ValidateDescriptor(metadata.Descriptor, $"{path}.descriptor");

        var environment = metadata.Environment
            ?? throw new ProtocolContractException($"{path}.environment", "environment is required");
        RequireText(environment.WindowsVersion, $"{path}.environment.windowsVersion");
        RequireText(environment.Architecture, $"{path}.environment.architecture");
        RequireText(environment.BluetoothAdapter, $"{path}.environment.bluetoothAdapter");
        RequireText(
            environment.FirmwareObservation,
            $"{path}.environment.firmwareObservation",
            allowEmpty: true);

        var provenance = metadata.Provenance
            ?? throw new ProtocolContractException($"{path}.provenance", "provenance is required");
        RequireText(provenance.ToolName, $"{path}.provenance.toolName");
        RequireText(provenance.ToolVersion, $"{path}.provenance.toolVersion");
        RequireText(provenance.Method, $"{path}.provenance.method");
        RequireText(provenance.Statement, $"{path}.provenance.statement");
    }

    private static void ValidateDescriptor(DescriptorIdentity descriptor, string path)
    {
        if (descriptor is null)
        {
            throw new ProtocolContractException(path, "descriptor identity is required");
        }

        Require(descriptor.VendorId != 0, $"{path}.vendorId", "must be non-zero");
        Require(descriptor.ProductId != 0, $"{path}.productId", "must be non-zero");
        RequireText(descriptor.HardwareId, $"{path}.hardwareId");
        RequireText(descriptor.CompatibleId, $"{path}.compatibleId");
        RequireText(descriptor.Transport, $"{path}.transport");
        RequireSha256(
            descriptor.DescriptorSignatureSha256,
            $"{path}.descriptorSignatureSha256");
    }

    private static void ValidateRawFrame(RawTouchFrame frame, string path)
    {
        if (frame is null)
        {
            throw new ProtocolContractException(path, "raw frame is required");
        }

        Require(frame.TimestampMicroseconds >= 0, $"{path}.timestampMicroseconds", "must be non-negative");
        Require(frame.Sequence >= 0, $"{path}.sequence", "must be non-negative");
        if (frame.Report is null)
        {
            throw new ProtocolContractException($"{path}.report", "is required");
        }

        Require(
            frame.Report.Length is > 0 and <= ProtocolLimits.MaxReportBytes,
            $"{path}.report",
            $"must contain between 1 and {ProtocolLimits.MaxReportBytes} bytes");
        Require(
            frame.Report[0] == frame.ReportId,
            $"{path}.reportId",
            "must match the first report byte");
    }

    private static void ValidateContact(NormalizedContact contact, string path)
    {
        if (contact is null)
        {
            throw new ProtocolContractException(path, "contact is required");
        }

        Require(contact.ContactId >= 0, $"{path}.contactId", "must be non-negative");
        RequireNormalized(contact.X, $"{path}.x");
        RequireNormalized(contact.Y, $"{path}.y");
        if (contact.Pressure is not null)
        {
            RequireNormalized(contact.Pressure.Value, $"{path}.pressure");
        }
    }

    private static void ValidateOutput(
        ExpectedScrollCommand output,
        string path,
        long frameTimestamp)
    {
        if (output is null)
        {
            throw new ProtocolContractException(path, "output command is required");
        }

        Require(
            output.TimestampMicroseconds >= frameTimestamp,
            $"{path}.timestampMicroseconds",
            "must not precede its source frame");
        Require(
            output.VerticalDelta is >= short.MinValue and <= short.MaxValue,
            $"{path}.verticalDelta",
            "must fit a signed 16-bit HID value");
        Require(
            output.HorizontalDelta is >= short.MinValue and <= short.MaxValue,
            $"{path}.horizontalDelta",
            "must fit a signed 16-bit HID value");
    }

    private static void ValidateCoordinateRange(CoordinateRange range, string path)
    {
        if (range is null)
        {
            throw new ProtocolContractException(path, "coordinate range is required");
        }

        Require(range.MaximumX > range.MinimumX, $"{path}.maximumX", "must exceed minimumX");
        Require(range.MaximumY > range.MinimumY, $"{path}.maximumY", "must exceed minimumY");
        RequireText(range.Origin, $"{path}.origin");
        RequireText(range.Orientation, $"{path}.orientation");
    }

    private static void RequireNormalized(double value, string path)
    {
        Require(
            double.IsFinite(value) && value is >= 0 and <= 1,
            path,
            "must be a finite value from 0 through 1");
    }

    private static void RequireSha256(string value, string path)
    {
        RequireText(value, path);
        Require(value.Length == 64, path, "must contain 64 hexadecimal characters");

        foreach (var character in value)
        {
            Require(
                Uri.IsHexDigit(character),
                path,
                "must contain only hexadecimal characters");
        }
    }

    private static void RequireText(string value, string path, bool allowEmpty = false)
    {
        if (value is null)
        {
            throw new ProtocolContractException(path, "is required");
        }

        Require(
            value.Length <= ProtocolLimits.MaxTextLength,
            path,
            $"must not exceed {ProtocolLimits.MaxTextLength.ToString(CultureInfo.InvariantCulture)} characters");
        Require(allowEmpty || !string.IsNullOrWhiteSpace(value), path, "must not be empty");
    }

    private static void RequireCount<T>(
        IReadOnlyList<T> values,
        int maximum,
        string path,
        bool requireNonEmpty = false)
    {
        if (values is null)
        {
            throw new ProtocolContractException(path, "is required");
        }

        Require(values.Count <= maximum, path, $"must not contain more than {maximum} items");
        Require(!requireNonEmpty || values.Count > 0, path, "must not be empty");
    }

    private static void Require(bool condition, string path, string message)
    {
        if (!condition)
        {
            throw new ProtocolContractException(path, message);
        }
    }
}
