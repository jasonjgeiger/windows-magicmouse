namespace MagicMouseWindows.Contracts.Protocol;

public sealed class ProtocolContractException : Exception
{
    public ProtocolContractException(string path, string message)
        : base($"{path}: {message}")
    {
        Path = path;
    }

    public ProtocolContractException(string path, string message, Exception innerException)
        : base($"{path}: {message}", innerException)
    {
        Path = path;
    }

    public string Path { get; }
}
