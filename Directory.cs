using System;
using System.Collections.Generic;
using System.Text;

public class DirectoryManager
{
    private VirtualDisk disk;
    private FatTableManager fat;
    
    //Constructor
     public DirectoryManager(VirtualDisk vd, FatTableManager ft)
    {
        disk = vd;
        fat = ft;
    }

    // 1) Read all directory entries from folder cluster
    public List<DirectoryEntry> ReadDirectory(int startCluster)
    {
        List<DirectoryEntry> entries = new List<DirectoryEntry>();

        // Follow cluster chain
        List<int> chain = fat.FollowChain(startCluster);

        foreach (int clus in chain)
        {
            byte[] data = disk.ReadCluster(clus);

            // Each entry = 32 bytes
            for (int i = 0; i < 1024; i += 32)
            {
                byte[] entryBytes = new byte[32];
                Array.Copy(data, i, entryBytes, 0, 32);

                DirectoryEntry entry = DirectoryEntry.FromBytes(entryBytes);

                if (entry != null)
                    entries.Add(entry);
            }
        }

        return entries;
    }

    // 2) Find entry by name (case-insensitive)
    public DirectoryEntry FindEntry(int startCluster, string name)
    {
        name = Format8Dot3(name);

        List<DirectoryEntry> entries = ReadDirectory(startCluster);

        foreach (DirectoryEntry e in entries)
        {
            if (e.Name.ToUpper() == name.ToUpper())
                return e;
        }

        return null;
    }

    // 3) Add new entry
    public void AddEntry(int startCluster, DirectoryEntry newEntry)
    {
        List<int> chain = fat.FollowChain(startCluster);

        foreach (int clus in chain)
        {
            byte[] data = disk.ReadCluster(clus);

            // look for empty slot
            for (int i = 0; i < 1024; i += 32)
            {
                if (data[i] == 0) // empty entry
                {
                    // Write here
                    byte[] entryBytes = newEntry.ToBytes();
                    Array.Copy(entryBytes, 0, data, i, 32);
                    disk.WriteCluster(clus, data);
                    return;
                }
            }
        }

        // No empty slot → allocate new cluster
        int newClus = fat.AllocateChain(1);

        // Update directory chain
        // find last cluster of directory
        int last = chain[chain.Count - 1];
        fat.SetFatEntry(last, newClus);
        fat.SetFatEntry(newClus, -1);

        // write entry at new cluster
        byte[] empty = new byte[1024];
        byte[] entryData = newEntry.ToBytes();
        Array.Copy(entryData, 0, empty, 0, 32);

        disk.WriteCluster(newClus, empty);
    }

    // 4) Remove entry
    public void RemoveEntry(int startCluster, string name)
    {
        name = Format8Dot3(name);

        List<int> chain = fat.FollowChain(startCluster);

        foreach (int clus in chain)
        {
            byte[] data = disk.ReadCluster(clus);

            for (int i = 0; i < 1024; i += 32)
            {
                // Match name
                string n = Encoding.ASCII.GetString(data, i, 11).Trim();

                if (n.ToUpper() == name.ToUpper())
                {
                    // Read entry
                    byte[] entryBytes = new byte[32];
                    Array.Copy(data, i, entryBytes, 0, 32);

                    DirectoryEntry entry = DirectoryEntry.FromBytes(entryBytes);

                    // free its clusters
                    fat.FreeChain(entry.FirstCluster);

                    // Clear entry (set first byte = 0)
                    data[i] = 0;

                    disk.WriteCluster(clus, data);
                    return;
                }
            }
        }
    }

    // Helper: format to upper-case 8.3
    public string Format8Dot3(string name)
    {
        name = name.ToUpper();

        string[] parts = name.Split('.');

        string fname = parts[0];
        string ext = parts.Length > 1 ? parts[1] : "";

        fname = fname.PadRight(8).Substring(0, 8);
        ext = ext.PadRight(3).Substring(0, 3);

        return fname + ext; // 11 chars
    }
}
