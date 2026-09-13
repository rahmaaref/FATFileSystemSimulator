using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {

        string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "virtual_disk.bin");

        try
        {
            /*================================================TASK 6=====================================================*/
            
            VirtualDisk vd = new VirtualDisk();
            vd.Initialize(diskPath);
            SuperblockManager sb = new SuperblockManager(vd);
            FatTableManager fat = new FatTableManager(vd);
            DirectoryManager dir = new DirectoryManager(vd, fat);
            FileManager fm = new FileManager(vd, fat,dir);
            Console.WriteLine("\n=============TASK 6=============");
            Console.WriteLine("Starting FAT File System Shell...\n");

            Shell shell = new Shell(vd, fat, dir, fm);
            // Run the interactive shell
            shell.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

    }
}