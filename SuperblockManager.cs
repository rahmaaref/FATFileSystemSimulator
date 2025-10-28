using System;

public class SuperblockManager
{
    private VirtualDisk disk;

    // Constructor that takes the VirtualDisk object
    public SuperblockManager(VirtualDisk vd)
    {
        disk = vd;
    }

    // Reads the Superblock from the virtual disk
    public byte[] ReadSuperBlock()
    {
        return disk.ReadCluster(0);
    }

    // Writes data to the Superblock
    public void WriteSuperBlock(byte[] data)
    {
        if (data.Length != 1024)
            throw new ArgumentException("Data Size Must Be 1024 bytes");

        disk.WriteCluster(0, data);
    }
}
