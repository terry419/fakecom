using System;

public class MapLoadException : Exception
{
    public string MapID { get; }

    public MapLoadException(string message, string mapID = "Unknown", Exception innerException = null)
        : base(message, innerException)
    {
        MapID = mapID;
    }
}