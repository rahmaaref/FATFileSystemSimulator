/* ========================================= TASK 1 ========================================== */
using System;
using System.IO;

public class VirtualDisk
{

    private const int CLUSTER_SIZE = 1024;
    private const int TOTAL_CLUSTERS = 1024;
    private const int DISK_SIZE = CLUSTER_SIZE * TOTAL_CLUSTERS;

    private FileStream? diskFile;

    // Initialize() 
    /*
    Takes a path
    Checks if file exists in the path file: virtual disk
    If doesn't exist: Creates a new 1 MB file filled with zeros
    If exists: Opens it for reading and writing
    */
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
        // Open the file for reading and writing
        diskFile = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
    }

    // ReadCluster(): reads a specific cluster from the disk
    public byte[] ReadCluster(int clusterNumber)
    {
        if (diskFile == null)
            throw new InvalidOperationException("Disk is not initialized.");

        if (clusterNumber < 0 || clusterNumber >= TOTAL_CLUSTERS)
            throw new ArgumentOutOfRangeException(nameof(clusterNumber), "Invalid cluster number");

        long offset = (long)clusterNumber * CLUSTER_SIZE;
        diskFile.Seek(offset, SeekOrigin.Begin); //let the pointer at the beginning of the cluster in the diskFile

        byte[] buffer = new byte[CLUSTER_SIZE]; // Prepare empty array
        int bytesRead = diskFile.Read(buffer, 0, CLUSTER_SIZE); // store 1024 bytes in buffer starting from index 0

        //Should read exactly 1024 bytes If less â†’ file might be corrupted or truncated
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
        diskFile.Flush(); //ensures data is actually saved by writing to file immediately
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

        diskFile.Flush(); //Ensures any pending writes are saved (extra safety)
        diskFile.Close();
        diskFile = null; //Marks disk as uninitialized preventing use-after-close 
    }
}