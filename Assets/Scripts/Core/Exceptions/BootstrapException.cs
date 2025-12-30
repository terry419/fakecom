using System;

public class BootstrapException : Exception
{
    public BootstrapException(string message) : base(message) { }

    public BootstrapException(string message, Exception innerException)
        : base(message, innerException) { }
}