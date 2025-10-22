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
            // Create the VirtualDisk object and initialize it
            VirtualDisk vd = new VirtualDisk();
            vd.Initialize(diskPath);

            // Prepare 1024 bytes of test data to write
            byte[] dataToWrite = Encoding.ASCII.GetBytes(new string('0', 1024));

            // Write data to cluster 0
            vd.WriteCluster(0, dataToWrite);

            // Read the same cluster back
            byte[] readData = vd.ReadCluster(0);
            string preview = Encoding.ASCII.GetString(readData, 0, 32); 
            Console.WriteLine($"First 32 bytes: {preview.Substring(0, 32)}");


            // Close the disk
            vd.CloseDisk();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
