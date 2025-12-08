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

            /*================================================TASK 3=====================================================*/
            Console.WriteLine("=============TASK 3=============");
            // Create FAT manager
            FatTableManager fat = new FatTableManager(vd);

            // Load FAT from disk
            fat.LoadFatFromDisk();
            Console.WriteLine("FAT loaded from disk.");

            // Allocate a chain of 3 clusters
            int startCluster = fat.AllocateChain(3);

            if (startCluster == -1)
            {
                Console.WriteLine("Not enough free clusters to allocate 3 clusters.");
            }
            else
            {
                Console.WriteLine($"Allocated chain starting at cluster: {startCluster}");

                // Follow the chain and print it
                var chain = fat.FollowChain(startCluster);
                Console.Write("Cluster chain: ");
                foreach (int c in chain)
                {
                    Console.Write(c + " ");
                }
                Console.WriteLine();
            }

            // Free the allocated chain
            fat.FreeChain(startCluster);
            Console.WriteLine($"Freed the chain starting at cluster: {startCluster}");

            // Save FAT back to disk
            fat.FlushFatToDisk();
            Console.WriteLine("FAT table saved back to disk.");

            Console.WriteLine("=============TASK 4=============");
            // Use root directory starting cluster defined in constants
            int rootCluster = FSConstants.ROOT_DIR_FIRST_CLUSTER;
            DirectoryManager dir = new DirectoryManager(vd, fat);

            Console.WriteLine("Listing directory entries before adding:");
            List<DirectoryEntry> beforeList = dir.ReadDirectory(rootCluster);
            foreach (DirectoryEntry e in beforeList)
            {
                Console.WriteLine($"  Entry: {e.Name}, Attr: {e.Attribute}, FirstClus: {e.FirstCluster}, Size: {e.FileSize}");
            }

            // Add a new file entry
            string newName = "TESTTXT  TXT";   // name must be 11 chars (8.3, padded or uppercase)
            DirectoryEntry newEntry = new DirectoryEntry(newName, 0, -1, 0);
            dir.AddEntry(rootCluster, newEntry);
            Console.WriteLine($"Added entry with name: {newName}");

            Console.WriteLine("Listing directory entries after adding:");
            List<DirectoryEntry> afterList = dir.ReadDirectory(rootCluster);
            foreach (DirectoryEntry e in afterList)
            {
                Console.WriteLine($"  Entry: {e.Name}, Attr: {e.Attribute}, FirstClus: {e.FirstCluster}, Size: {e.FileSize}");
            }

            // Remove the entry
            dir.RemoveEntry(rootCluster, newName);
            Console.WriteLine($"Removed entry with name: {newName}");

            Console.WriteLine("Listing directory entries after removal:");
            List<DirectoryEntry> finalList = dir.ReadDirectory(rootCluster);
            foreach (DirectoryEntry e in finalList)
            {
                Console.WriteLine($"  Entry: {e.Name}, Attr: {e.Attribute}, FirstClus: {e.FirstCluster}, Size: {e.FileSize}");
            }


            // Close the disk
            vd.CloseDisk();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
    }
}
