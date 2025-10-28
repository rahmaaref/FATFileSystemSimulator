// FSConstants.cs
public static class FSConstants
{
    // Size of a cluster in bytes
    public const int CLUSTER_SIZE = 1024;

    // Total number of clusters
    public const int CLUSTER_COUNT = 1024;

    // Cluster for the superblock 
    public const int SUPERBLOCK_CLUSTER = 0;

    // FAT entries as 4 clusters
    public const int FAT_START_CLUSTER = 1;
    public const int FAT_END_CLUSTER = 4;

    // The begining of the content
    public const int CONTENT_START_CLUSTER = 5;
    public const int ROOT_DIR_FIRST_CLUSTER = 5;

}
