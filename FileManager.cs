// TASK 5
using System;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;

public class FileManager
{
    private VirtualDisk disk;
    private FatTableManager fat;
    private DirectoryManager dir;

    public FileManager(VirtualDisk disk, FatTableManager fat, DirectoryManager dir)
    {
        this.disk = disk;
        this.fat = fat;
        this.dir = dir;
    }

    // ================= CREATE + WRITE FILE =================
    public void CreateFile(int dirCluster, string name, byte[] data)
    {
        // Format the name to 8.3 format
        string formattedName = dir.Format8Dot3(name);
        
        //Calculate how many clusters needed
        int clustersNeeded = (int)Math.Ceiling(data.Length / 1024.0);
        
        // Handle empty files
        if (clustersNeeded == 0)
            clustersNeeded = 1;

        int firstCluster = fat.AllocateChain(clustersNeeded);
        if (firstCluster == -1)
            throw new Exception("Not enough space");

        // Write data to clusters
        List<int> chain = fat.FollowChain(firstCluster);
        int dataIndex = 0;

        foreach (int cluster in chain)
        {
            byte[] buffer = new byte[1024];
            int length = Math.Min(1024, data.Length - dataIndex);
            Array.Copy(data, dataIndex, buffer, 0, length);
            disk.WriteCluster(cluster, buffer);
            dataIndex += length;
        }

        DirectoryEntry entry = new DirectoryEntry(formattedName, 0, firstCluster, data.Length);
        dir.AddEntry(dirCluster, entry);
    }

    // ================= READ FILE =================
    public byte[] ReadFile(DirectoryEntry entry)
    {
        List<int> chain = fat.FollowChain(entry.FirstCluster);
        byte[] data = new byte[entry.FileSize];
        int index = 0;

        foreach (int cluster in chain)
        {
            byte[] buffer = disk.ReadCluster(cluster);
            int length = Math.Min(1024, data.Length - index);
            Array.Copy(buffer, 0, data, index, length);
            index += length;
        }

        return data;
    }

    // ================= DELETE FILE =================
    public void DeleteFile(int dirCluster, string name)
    {
        DirectoryEntry? entry = dir.TryFindEntry(dirCluster, name);
        if (entry == null)
            return;

        fat.FreeChain(entry.FirstCluster);
        dir.RemoveEntry(dirCluster, name);
    }

    // ================= WRITE TEXT =================
    public void WriteText(int dirCluster, string name, string text, bool append)
    {
        DirectoryEntry? e = dir.TryFindEntry(dirCluster, name);

        if (e == null)
            throw new Exception("File does not exist");
        
        byte[] old = append ? ReadFile(e) : new byte[0];
        byte[] newData = Encoding.ASCII.GetBytes(text);
        byte[] combined = new byte[old.Length + newData.Length];

        Array.Copy(old, combined, old.Length);
        Array.Copy(newData, 0, combined, old.Length, newData.Length);

        DeleteFile(dirCluster, name);
        CreateFile(dirCluster, name, combined);
    }

    //Create a duplicate of a file with a new name
    public void CopyFile(int dirCluster, string src, string dst)
    {
        // Find source file
        DirectoryEntry? e = dir.TryFindEntry(dirCluster, src);
        if (e == null) return;

        byte[] data = ReadFile(e);
        CreateFile(dirCluster, dst, data);
    }

}