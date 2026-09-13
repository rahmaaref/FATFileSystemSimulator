//TASK 4
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
    // Cluster 5:  [Entry1][Entry2][Entry3]...[Entry32]
    // Cluster 10: [Entry33][Entry34]...[Entry64]
    public List<DirectoryEntry> ReadDirectory(int startCluster)
    {
        List<DirectoryEntry> entries = new List<DirectoryEntry>();

        // Follow cluster chain A directory might be larger than 1024 bytes (1 cluster)
        List<int> chain = fat.FollowChain(startCluster);

        foreach (int clus in chain) // Reads each cluster in the chain.
        {
            byte[] data = disk.ReadCluster(clus);

            // Extract entries from cluster 
            // Each entry = 32 bytes
            for (int i = 0; i < 1024; i += 32)
            {
                // A zero first byte marks an unused slot (same convention
                // AddEntry looks for and RemoveEntry sets) - skip it so
                // empty/deleted slots don't show up as blank entries.
                if (data[i] == 0)
                    continue;

                byte[] entryBytes = new byte[32];
                Array.Copy(data, i, entryBytes, 0, 32);

                entries.Add(DirectoryEntry.FromBytes(entryBytes));
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
            // Compare trimmed forms: stored names come back trimmed from
            // FromBytes, but Format8Dot3 always returns the full padded
            // 11-char form - trimming both sides keeps the comparison
            // consistent regardless of whether there's an extension.
            if (e.Name.Trim().ToUpper() == name.Trim().ToUpper())
                return e;
        }
        throw new FileNotFoundException($"Entry '{name.Trim()}' not found");

    }

    // Same lookup as FindEntry, but returns null instead of throwing.
    // Use this when the caller wants to check existence with an
    // `if (entry == null)` instead of a try/catch.
    public DirectoryEntry? TryFindEntry(int startCluster, string name)
    {
        try
        {
            return FindEntry(startCluster, name);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
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

        // No empty slot â†’ allocate new cluster
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
                // Match name (trim the padded search key too - see FindEntry)
                string n = Encoding.ASCII.GetString(data, i, 11).Trim();

                if (n.ToUpper() == name.Trim().ToUpper())
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