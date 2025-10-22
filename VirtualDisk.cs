using System;
using System.IO;

public class VirtualDisk
{

    private const int CLUSTER_SIZE = 1024;
    private const int TOTAL_CLUSTERS = 1024;
    private const int DISK_SIZE = CLUSTER_SIZE * TOTAL_CLUSTERS;

    private FileStream? diskFile;

    // Initialize() 
    public void Initialize(string path)
    {
        if (!File.Exists(path))
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] zeroCluster = new byte[CLUSTER_SIZE];
                for (int i = 0; i < TOTAL_CLUSTERS; i++)
                {
                    fs.Write(zeroCluster, 0, CLUSTER_SIZE);
                }
            }
        }
        diskFile = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
    }

    // ReadCluster()
    public byte[] ReadCluster(int clusterNumber)
    {
        if (diskFile == null)
            throw new InvalidOperationException("Disk is not initialized.");

        if (clusterNumber < 0 || clusterNumber >= TOTAL_CLUSTERS)
            throw new ArgumentOutOfRangeException(nameof(clusterNumber), "Invalid cluster number");

        byte[] buffer = new byte[CLUSTER_SIZE];
        long offset = (long)clusterNumber * CLUSTER_SIZE;
        diskFile.Seek(offset, SeekOrigin.Begin);

        int bytesRead = diskFile.Read(buffer, 0, CLUSTER_SIZE);
        if (bytesRead < CLUSTER_SIZE)
            throw new IOException("Could not read full cluster.");

        return buffer;
    }

    // WriteCluster()
    public void WriteCluster(int clusterNumber, byte[] data)
    {
        if (diskFile == null)
            throw new InvalidOperationException("Disk is not initialized.");

        if (clusterNumber < 0 || clusterNumber >= TOTAL_CLUSTERS)
            throw new ArgumentOutOfRangeException(nameof(clusterNumber), "Invalid cluster number");


        long offset = (long)clusterNumber * CLUSTER_SIZE;
        diskFile.Seek(offset, SeekOrigin.Begin);
        diskFile.Write(data, 0, CLUSTER_SIZE);
        diskFile.Flush();
    }

    // GetDiskSize()
    public int GetDiskSize()
    {
        return DISK_SIZE;
    }

    // CloseDisk()
    public void CloseDisk()
    {
        if (diskFile == null)
            throw new InvalidOperationException("Disk is not initialized.");

        diskFile.Flush();
        diskFile.Close();
        diskFile = null;
    }
}
