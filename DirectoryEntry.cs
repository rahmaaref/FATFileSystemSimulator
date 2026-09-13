// Task 4
using System;
using System.Text;

public class DirectoryEntry
{
    public string Name;
    public byte Attribute;
    public int FirstCluster;
    public int FileSize;

    // Constructor
    public DirectoryEntry(string name, byte attr, int firstCluster, int fileSize)
    {
        Name = name;
        Attribute = attr;
        FirstCluster = firstCluster;
        FileSize = fileSize;
    }

    // Convert entry â†’ 32 bytes Converting Entry to Disk Format
    public byte[] ToBytes()
    {
        byte[] data = new byte[32];

        // store Name (11 bytes) - ensure name is exactly 11 characters
        string paddedName = Name.Length >= 11 ? Name.Substring(0, 11) : Name.PadRight(11); // truncate to 11/spaces to make it 11
        byte[] nameBytes = Encoding.ASCII.GetBytes(paddedName); //Convert to ASCII bytes
        Array.Copy(nameBytes, 0, data, 0, Math.Min(11, nameBytes.Length));

        // Stores the attribute byte at position 11
        data[11] = Attribute;

        // Reserved space (8 bytes at offset 12-19)
        // Leave as zeros

        // First cluster (4 bytes at offset 20)
        byte[] clusterBytes = BitConverter.GetBytes(FirstCluster); // Converts the integer cluster number into 4 bytes
        Array.Copy(clusterBytes, 0, data, 20, 4);

        // File size (4 bytes at offset 24)
        byte[] sizeBytes = BitConverter.GetBytes(FileSize); // Converts the file size integer into 4 bytes
        Array.Copy(sizeBytes, 0, data, 24, 4);

        // Remaining 4 bytes (28-31) = zeros 

        return data;
    }

    // Convert bytes â†’ DirectoryEntry
    public static DirectoryEntry FromBytes(byte[] data)
    {


        string name = Encoding.ASCII.GetString(data, 0, 11).Trim(); // Extract Name (Bytes 0-10) .Trim() removes trailing spaces
        byte attr = data[11]; // Extract Attribute
        int firstClus = BitConverter.ToInt32(data, 20); // Extract First Cluster
        int size = BitConverter.ToInt32(data, 24); // Extract File Size

        return new DirectoryEntry(name, attr, firstClus, size);
    }
}