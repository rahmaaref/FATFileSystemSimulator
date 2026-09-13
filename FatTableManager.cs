using System;
using System.Collections.Generic;

public class FatTableManager
{
    private VirtualDisk disk;                 // Reference to the virtual disk

    // Constructor
    public FatTableManager(VirtualDisk vd)
    {
        disk = vd;
    }

    // Reads FAT from disk clusters 1-4 into the in-memory array
    private int[] fat = new int[FSConstants.FAT_ENTRIES]; // The FAT array
    public void LoadFatFromDisk()
    {
        int entryIndex = 0;
        for (int cluster = FSConstants.FAT_START_CLUSTER; cluster <= FSConstants.FAT_End_CLUSTER; cluster++)
        {
            byte[] data = disk.ReadCluster(cluster);

            for (int i = 0; i < data.Length; i += FSConstants.ENTRY_SIZE)
            {
                if (entryIndex >= FSConstants.FAT_ENTRIES)
                    return;

                fat[entryIndex] = BitConverter.ToInt32(data, i);
                entryIndex++;
            }
        }
    }

    // Write the in-memory FAT array into clusters 1â€“4
    public void FlushFatToDisk()
    {
        int entryIndex = 0;//from 0 to 1023

        for (int cluster = FSConstants.FAT_START_CLUSTER; cluster <= FSConstants.FAT_End_CLUSTER; cluster++)
        {
            byte[] data = new byte[1024];
            int pos = 0; //from 0 to 1023

            while (pos < 1024 && entryIndex < FSConstants.FAT_ENTRIES)
            {
                byte[] bytes = BitConverter.GetBytes(fat[entryIndex]); // Convert one integer (FAT entry) to 4 bytes   pos:starting index in data
                Array.Copy(bytes, 0, data, pos, 4);
                entryIndex++;
                pos += 4;
            }

            disk.WriteCluster(cluster, data);
        }
    }

    // Read a specific FAT entry
    public int GetFatEntry(int index)
    {
        return fat[index];
    }

    // Write a specific FAT entry
    public void SetFatEntry(int index, int value)
    {
        fat[index] = value;
    }

    // Return the whole FAT
    public int[] ReadAllFat()
    {
        return fat;
    }

    public void WriteAllFat(int[] entries)
    {
        for (int i = 0; i < FSConstants.FAT_ENTRIES; i++)
            fat[i] = entries[i];
    }

    // Follow a chain until -1 (end of chain)
    // Used when reading a new file.
    /* example 
    Start at cluster 10
    Look up where cluster 10 points â†’ cluster 15
    Look up where cluster 15 points â†’ cluster 22
    Look up where cluster 22 points â†’ -1 (end!)
    Return the path: [10, 15, 22]
*/
    public List<int> FollowChain(int start)
    {
        List<int> chain = new List<int>();

        // Validate starting cluster
        if (start < 0 || start >= FSConstants.FAT_ENTRIES)
        {
            throw new ArgumentException($"Invalid start cluster: {start}");
        }

        int current = start;
        int maxIterations = FSConstants.FAT_ENTRIES; // Prevent infinite loops

        while (current != -1 && chain.Count < maxIterations)
        {
            // Validate cluster number
            if (current < 0 || current >= FSConstants.FAT_ENTRIES)
                break;

            // Prevent cycles
            if (chain.Contains(current))
                break;

            chain.Add(current);
            current = fat[current];
        }

        return chain;
    }

    // Allocate n clusters and link them with FAT
    // Finds free clusters, links them together into a chain, and returns the starting cluster
    // Used when creating a new file.
    public int AllocateChain(int count) //n clusters
    {
        List<int> free = new List<int>();

        for (int i = 5; i < FSConstants.FAT_ENTRIES; i++) // clusters 0â€“4 are reserved
        {
            if (fat[i] == 0)
                free.Add(i);

            if (free.Count == count) //first fit
                break;
        }

        if (free.Count < count)
            return -1; // not enough space

        // Link clusters
        for (int i = 0; i < count - 1; i++)
            fat[free[i]] = free[i + 1];

        fat[free[count - 1]] = -1; // last cluster

        return free[0]; // return start of chain
    }

    // Walks through a cluster chain and marks all clusters as free (FAT value = 0)
    // Used when deleting a file.
    public void FreeChain(int start)
    {
        int current = start;
        int maxIterations = FSConstants.FAT_ENTRIES;
        int iterations = 0;

        while (current != -1 && iterations < maxIterations)
        {
            // Validate cluster number
            if (current < 0 || current >= FSConstants.FAT_ENTRIES)
                break;

            int next = fat[current];
            fat[current] = 0; // mark free
            current = next;
            iterations++;
        }
    }
}