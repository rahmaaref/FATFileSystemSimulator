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

    // Convert entry → 32 bytes
    public byte[] ToBytes()
    {
        byte[] data = new byte[32];

        // Name (11 bytes)
        byte[] nameBytes = Encoding.ASCII.GetBytes(Name);
        Array.Copy(nameBytes, 0, data, 0, 11);

        // Attribute
        data[11] = Attribute;

        // First cluster (4 bytes)
        Array.Copy(BitConverter.GetBytes(FirstCluster), 0, data, 12, 4);

        // File size (4 bytes)
        Array.Copy(BitConverter.GetBytes(FileSize), 0, data, 16, 4);

        // Remaining 12 bytes = zeros (already zero)

        return data;
    }

    // Convert bytes → DirectoryEntry
    public static DirectoryEntry FromBytes(byte[] data)
    {
        // If first byte = 0 → empty entry
        if (data[0] == 0)
            return null;

        string name = Encoding.ASCII.GetString(data, 0, 11).Trim();
        byte attr = data[11];
        int firstClus = BitConverter.ToInt32(data, 12);
        int size = BitConverter.ToInt32(data, 16);

        return new DirectoryEntry(name, attr, firstClus, size);
    }
}
