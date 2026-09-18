using System.Text.Json;
using System.Text.Json.Serialization;

namespace MagicMouseWindows.Contracts.Protocol;

public static class ProtocolDocumentSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = 32,
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    public static byte[] SerializeFixture(CaptureFixture fixture)
    {
        ProtocolContractValidator.Validate(fixture);
        return SerializeBounded(fixture, "fixture");
    }

    public static CaptureFixture DeserializeFixture(ReadOnlySpan<byte> document)
    {
        var fixture = DeserializeBounded<CaptureFixture>(document, "fixture");
        ProtocolContractValidator.Validate(fixture);
        return fixture;
    }

    public static byte[] SerializeProfile(DeviceProfileDocument profile)
    {
        ProtocolContractValidator.Validate(profile);
        return SerializeBounded(profile, "profile");
    }

    public static DeviceProfileDocument DeserializeProfile(ReadOnlySpan<byte> document)
    {
        var profile = DeserializeBounded<DeviceProfileDocument>(document, "profile");
        ProtocolContractValidator.Validate(profile);
        return profile;
    }

    private static byte[] SerializeBounded<T>(T value, string documentName)
    {
        var document = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        if (document.Length > ProtocolLimits.MaxDocumentBytes)
        {
            throw new ProtocolContractException(
                "$",
                $"{documentName} exceeds the {ProtocolLimits.MaxDocumentBytes}-byte limit");
        }

        return document;
    }

    private static T DeserializeBounded<T>(ReadOnlySpan<byte> document, string documentName)
    {
        if (document.Length is 0 or > ProtocolLimits.MaxDocumentBytes)
        {
            throw new ProtocolContractException(
                "$",
                $"{documentName} must contain between 1 and {ProtocolLimits.MaxDocumentBytes} bytes");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(document, SerializerOptions)
                ?? throw new ProtocolContractException("$", $"{documentName} is null");
        }
        catch (JsonException exception)
        {
            throw new ProtocolContractException(
                exception.Path ?? "$",
                $"invalid {documentName} JSON: {exception.Message}",
                exception);
        }
    }
}
