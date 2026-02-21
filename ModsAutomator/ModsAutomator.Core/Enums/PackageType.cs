namespace ModsWatcher.Core.Enums
{
    /// <summary>
    /// This Enum represents the type of package used for a Mod
    /// </summary>
    public enum PackageType : byte
    {
        Unknown = 0,
        Zip = 1,
        Rar = 2,
        Scs = 3, //ATS and ETS2 native package format
        SevenZip = 4,
    }
}
