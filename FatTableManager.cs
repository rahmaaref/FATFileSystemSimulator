using System;

public class FatTableManager
{
    private const int FAT_ENTRIES = 1024;     // 1024 ints
    private const int FAT_START_CLUSTER = 1;  // FAT uses clusters 1–4
    private const int FAT_End_CLUSTER = 5;  // 4 clusters reserved for FAT
    private const int ENTRY_SIZE = 4;         // 4 bytes per int

    private VirtualDisk disk;                 // Reference to the virtual disk
    private int[] fat = new int[FAT_ENTRIES]; // The in-memory FAT array

    // ================================================================
    // Constructor
    // ================================================================
    public FatTableManager(VirtualDisk vd)
    {
        disk = vd;
    }

    // ================================================================
    // Load FAT from clusters 1–4 into memory
    // ================================================================
    public void LoadFatFromDisk()
    {
        int entryIndex = 0;

        for (int cluster = FAT_START_CLUSTER; cluster < FAT_End_CLUSTER; cluster++)
        {
            byte[] data = disk.ReadCluster(cluster);

            for (int i = 0; i < data.Length; i += ENTRY_SIZE)
            {
                if (entryIndex >= FAT_ENTRIES)
                    return;

                fat[entryIndex] = BitConverter.ToInt32(data, i);
                entryIndex++;
            }
        }
    }

    // ================================================================
    // Write the in-memory FAT array into clusters 1–4
    // ================================================================
    public void FlushFatToDisk()
    {
        int entryIndex = 0;

        for (int cluster = FAT_START_CLUSTER; cluster < FAT_End_CLUSTER; cluster++)
        {
            byte[] data = new byte[1024];
            int pos = 0;

            while (pos < 1024 && entryIndex < FAT_ENTRIES)
            {
                byte[] bytes = BitConverter.GetBytes(fat[entryIndex]);
                Array.Copy(bytes, 0, data, pos, 4);
                entryIndex++;
                pos += 4;
            }

            disk.WriteCluster(cluster, data);
        }
    }

    // ================================================================
    // Read a specific FAT entry
    // ================================================================
    public int GetFatEntry(int index)
    {
        return fat[index];
    }

    // ================================================================
    // Write a specific FAT entry
    // ================================================================
    public void SetFatEntry(int index, int value)
    {
        fat[index] = value;
    }

    // ================================================================
    // Return the whole FAT
    // ================================================================
    public int[] ReadAllFat()
    {
        return fat;
    }

    public void WriteAllFat(int[] entries)
    {
        for (int i = 0; i < FAT_ENTRIES; i++)
            fat[i] = entries[i];
    }

    // ================================================================
    // Follow a chain until -1 (end of chain)
    // ================================================================
    public List<int> FollowChain(int start)
    {
        List<int> chain = new List<int>();
        int current = start;

        while (current != -1)
        {
            chain.Add(current);
            current = fat[current];
        }

        return chain;
    }

    // ================================================================
    // Allocate n clusters and link them with FAT
    // ================================================================
    public int AllocateChain(int count)
    {
        List<int> free = new List<int>();

        for (int i = 5; i < FAT_ENTRIES; i++) // clusters 0–4 are reserved
        {
            if (fat[i] == 0)
                free.Add(i);

            if (free.Count == count)
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

    // ================================================================
    // Free all clusters in the chain
    // ================================================================
    public void FreeChain(int start)
    {
        int current = start;

        while (current != -1)
        {
            int next = fat[current];
            fat[current] = 0; // mark free
            current = next;
        }
    }
}
