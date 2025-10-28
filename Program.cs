using System;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {

        string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "virtual_disk.bin");

        try
        {
            /*================================================TASK 1=====================================================*/
            Console.WriteLine("=============TASK 1=============");
            // Create the VirtualDisk object and initialize it
            VirtualDisk vd = new VirtualDisk();
            vd.Initialize(diskPath);

            // Prepare 1024 bytes of test data to write
            byte[] dataToWrite = Encoding.ASCII.GetBytes(new string('0', 1024));

            // Write data to cluster 0
            vd.WriteCluster(0, dataToWrite);

            // Read the same cluster back
            byte[] readData = vd.ReadCluster(0);
            string p = Encoding.ASCII.GetString(readData, 0, 32);
            Console.WriteLine($"First 32 bytes: {p.Substring(0, 32)}");


            /*================================================TASK 2=====================================================*/
            Console.WriteLine("=============TASK 2=============");
            // Create the superblock manager 
            SuperblockManager sb = new SuperblockManager(vd);

            // Read current superblock 
            // Output: zeros
            byte[] current = sb.ReadSuperBlock();
            string text = Encoding.ASCII.GetString(current, 0, 32);
            Console.WriteLine($"Superblock first 32 bytes: {text}");


            // The superblock will contain R in ASCII value instead of zeros
            byte[] data = new byte[1024];
            for (int i = 0; i < data.Length; i++)
            {
                char ch = 'R';
                data[i] = (byte)ch;

            }
            // Write the superblock to cluster 0 on the disk
            sb.WriteSuperBlock(data);

            // Read the superblock again
            // Output: R
            // Currently in bytes
            byte[] read = sb.ReadSuperBlock();

            // Convert bytes to R
            string result = Encoding.ASCII.GetString(read, 0, 32);
            Console.WriteLine($"Superblock first 32 bytes after editing: {result}");


            // Close the disk
            vd.CloseDisk();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
