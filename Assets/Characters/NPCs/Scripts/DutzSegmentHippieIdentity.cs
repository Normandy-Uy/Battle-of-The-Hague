/// <summary>Names for the 7 pooled small hippies that teleport between highway segments.</summary>
public static class DutzSegmentHippieIdentity
{
    public const string PoolRootName = "DutzSegmentHippiePool";
    public const string ManagerObjectName = "DutzSegmentHippieManager";
    public const string HippiePrefix = "DutzSegmentHippie_";
    public const int PoolCount = 7;

    public static bool IsPoolHippie(string objectName) =>
        !string.IsNullOrEmpty(objectName) && objectName.StartsWith(HippiePrefix);
}
