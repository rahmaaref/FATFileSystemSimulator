// TASK 6
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class Shell
{
    private VirtualDisk disk;
    private FatTableManager fat;
    private DirectoryManager dir;
    private FileManager fm;
    private int currentDirCluster;

    public Shell(VirtualDisk vd, FatTableManager fatManager, DirectoryManager dirManager, FileManager fileManager)
    {
        disk = vd;
        fat = fatManager;
        dir = dirManager;
        fm = fileManager;
        currentDirCluster = FSConstants.ROOT_DIR_FIRST_CLUSTER;
    }

    public void Run()
    {
        // Initialize the file system
        InitializeFileSystem();

        Console.WriteLine("FAT File System Shell - Type 'help' for commands\n");

        bool running = true;
        while (running)
        {
            Console.Write($"[Cluster {currentDirCluster}]> ");
            string input = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(input))
                continue;

            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLower();

            try
            {
                switch (command)
                {
                    case "help":
                        ShowHelp();
                        break;

                    case "cd":
                        ChangeDirectory(parts);
                        break;

                    case "clear":
                        Console.Clear();
                        break;

                    case "ls":
                        ListDirectory(parts);
                        break;

                    case "exit":
                        running = false;
                        Console.WriteLine("Exiting shell...");
                        fat.FlushFatToDisk();
                        break;

                    case "cp":
                        CopyFile(parts);
                        break;

                    case "mv":
                        MoveFile(parts);
                        break;

                    case "rm":
                        DeleteFile(parts);
                        break;

                    case "mkdir":
                        MakeDirectory(parts);
                        break;

                    case "rmdir":
                        RemoveDirectory(parts);
                        break;

                    case "cat":
                        DisplayFile(parts);
                        break;

                    case "touch":
                        TouchFile(parts);
                        break;

                    case "echo":
                        EchoToFile(parts, input);
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        disk.CloseDisk();
    }

    private void InitializeFileSystem()
    {
        try
        {
            fat.LoadFatFromDisk();

            // Check if root directory exists
            int[] fatEntries = fat.ReadAllFat();
            if (fatEntries[FSConstants.ROOT_DIR_FIRST_CLUSTER] == 0)
            {
                Console.WriteLine("Initializing new file system...");
                // Mark root directory cluster as end of chain
                fat.SetFatEntry(FSConstants.ROOT_DIR_FIRST_CLUSTER, -1);
                fat.FlushFatToDisk();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File system initialization error: {ex.Message}");
        }
    }

    private void ShowHelp()
    {
        Console.WriteLine("\nAvailable Commands:");
        Console.WriteLine("  cd [dir]              - Change directory or show current directory");
        Console.WriteLine("  clear                 - Clear the screen");
        Console.WriteLine("  ls [dir]              - List directory contents");
        Console.WriteLine("  exit                  - Exit the shell");
        Console.WriteLine("  cp <src> <dst>        - Copy file");
        Console.WriteLine("  mv <src> <dst>        - Move/rename file");
        Console.WriteLine("  rm <file>             - Delete file");
        Console.WriteLine("  mkdir <dir>           - Create directory");
        Console.WriteLine("  rmdir <dir>           - Remove empty directory");
        Console.WriteLine("  cat <file>            - Display file contents");
        Console.WriteLine("  touch <file>          - Create empty file");
        Console.WriteLine("  echo \"text\" <file>    - Write text to file (overwrite)");
        Console.WriteLine("  echo \"text\" <file> --append - Append text to file");
        Console.WriteLine("  help                  - Show this help\n");
    }

    private void ChangeDirectory(string[] parts)
    {
        if (parts.Length == 1)
        {
            Console.WriteLine($"Current directory cluster: {currentDirCluster}");
            return;
        }

        string targetDir = parts[1];

        if (targetDir == "..")
        {
            // Go to parent (for simplicity, just go to root)
            currentDirCluster = FSConstants.ROOT_DIR_FIRST_CLUSTER;
            Console.WriteLine("Changed to root directory");
            return;
        }

        if (targetDir == "/" || targetDir == "root")
        {
            currentDirCluster = FSConstants.ROOT_DIR_FIRST_CLUSTER;
            Console.WriteLine("Changed to root directory");
            return;
        }

        try
        {
            DirectoryEntry entry = dir.FindEntry(currentDirCluster, targetDir);
            if (entry.Attribute == 1) // Directory attribute
            {
                currentDirCluster = entry.FirstCluster;
                Console.WriteLine($"Changed to directory: {targetDir}");
            }
            else
            {
                Console.WriteLine($"'{targetDir}' is not a directory");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Directory not found: {targetDir}");
        }
    }

    private void ListDirectory(string[] parts)
    {
        int targetCluster = currentDirCluster;

        if (parts.Length > 1)
        {
            try
            {
                DirectoryEntry entry = dir.FindEntry(currentDirCluster, parts[1]);
                if (entry.Attribute == 1)
                    targetCluster = entry.FirstCluster;
                else
                {
                    Console.WriteLine($"'{parts[1]}' is not a directory");
                    return;
                }
            }
            catch
            {
                Console.WriteLine($"Directory not found: {parts[1]}");
                return;
            }
        }

        List<DirectoryEntry> entries = dir.ReadDirectory(targetCluster);

        if (entries.Count == 0)
        {
            Console.WriteLine("(empty directory)");
            return;
        }

        Console.WriteLine("\nType  Name           Size      Cluster  Attr");
        Console.WriteLine("----  -----------    --------  -------  ----");
        foreach (DirectoryEntry entry in entries)
        {
            string type = entry.Attribute == 1 ? "DIR " : "FILE";
            string name = entry.Name.Trim();
            Console.WriteLine($"{type}  {name,-13}  {entry.FileSize,8}  {entry.FirstCluster,7}  {entry.Attribute,4}");
        }
        Console.WriteLine();
    }

    private void CopyFile(string[] parts)
    {
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: cp <source> <destination>");
            return;
        }

        string src = parts[1];
        string dst = parts[2];

        fm.CopyFile(currentDirCluster, src, dst);
        Console.WriteLine($"Copied '{src}' to '{dst}'");
    }

    private void MoveFile(string[] parts)
    {
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: mv <source> <destination>");
            return;
        }

        string src = parts[1];
        string dst = parts[2];

        // Copy then delete
        fm.CopyFile(currentDirCluster, src, dst);
        fm.DeleteFile(currentDirCluster, src);
        Console.WriteLine($"Moved '{src}' to '{dst}'");
    }

    private void DeleteFile(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: rm <file>");
            return;
        }

        string filename = parts[1];
        fm.DeleteFile(currentDirCluster, filename);
        Console.WriteLine($"Deleted '{filename}'");
    }

    private void MakeDirectory(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: mkdir <directory>");
            return;
        }

        string dirname = parts[1];

        // Check if directory already exists
        try
        {
            DirectoryEntry existing = dir.FindEntry(currentDirCluster, dirname);
            Console.WriteLine($"Directory '{dirname}' already exists");
            return;
        }
        catch
        {
            // Directory doesn't exist, proceed to create
        }

        string formattedName = dir.Format8Dot3(dirname);

        // Allocate one cluster for the new directory
        int newCluster = fat.AllocateChain(1);
        if (newCluster == -1)
        {
            Console.WriteLine("Not enough space");
            return;
        }

        // Create directory entry with attribute = 1 (directory)
        DirectoryEntry newDir = new DirectoryEntry(formattedName, 1, newCluster, 0);
        dir.AddEntry(currentDirCluster, newDir);

        // Initialize the new directory cluster with empty data
        byte[] empty = new byte[1024];
        disk.WriteCluster(newCluster, empty);

        Console.WriteLine($"Created directory '{dirname}' (cluster {newCluster})");
    }

    private void RemoveDirectory(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: rmdir <directory>");
            return;
        }

        string dirname = parts[1];

        try
        {
            DirectoryEntry entry = dir.FindEntry(currentDirCluster, dirname);

            if (entry.Attribute != 1)
            {
                Console.WriteLine($"'{dirname}' is not a directory");
                return;
            }

            // Check if directory is empty
            List<DirectoryEntry> contents = dir.ReadDirectory(entry.FirstCluster);
            if (contents.Count > 0)
            {
                Console.WriteLine($"Directory '{dirname}' is not empty");
                return;
            }

            // Free the directory's clusters and remove entry
            fat.FreeChain(entry.FirstCluster);
            dir.RemoveEntry(currentDirCluster, dirname);

            Console.WriteLine($"Removed directory '{dirname}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void DisplayFile(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: cat <file>");
            return;
        }

        string filename = parts[1];

        try
        {
            DirectoryEntry entry = dir.FindEntry(currentDirCluster, filename);

            if (entry.Attribute == 1)
            {
                Console.WriteLine($"'{filename}' is a directory");
                return;
            }

            byte[] data = fm.ReadFile(entry);
            string text = Encoding.ASCII.GetString(data);
            Console.WriteLine(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void TouchFile(string[] parts)
    {
        if (parts.Length < 2)
        {
            Console.WriteLine("Usage: touch <file>");
            return;
        }

        string filename = parts[1];

        try
        {
            // Check if file already exists
            DirectoryEntry existing = dir.FindEntry(currentDirCluster, filename);
            Console.WriteLine($"File '{filename}' already exists");
        }
        catch
        {
            // File doesn't exist, create empty file
            byte[] emptyData = new byte[0];
            fm.CreateFile(currentDirCluster, filename, emptyData);
            Console.WriteLine($"Created empty file '{filename}'");
        }
    }

    private void EchoToFile(string[] parts, string fullInput)
    {
        // Parse: echo "text" <file> [--append]
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: echo \"text\" <file> [--append]");
            return;
        }

        // Extract text between quotes
        int firstQuote = fullInput.IndexOf('"');
        int lastQuote = fullInput.LastIndexOf('"');

        if (firstQuote == -1 || lastQuote == -1 || firstQuote == lastQuote)
        {
            Console.WriteLine("Text must be enclosed in quotes");
            return;
        }

        string text = fullInput.Substring(firstQuote + 1, lastQuote - firstQuote - 1);

        // Get remaining parts after the closing quote
        string afterQuote = fullInput.Substring(lastQuote + 1).Trim();
        string[] fileParts = afterQuote.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (fileParts.Length < 1)
        {
            Console.WriteLine("Usage: echo \"text\" <file> [--append]");
            return;
        }

        string filename = fileParts[0];
        bool append = fileParts.Length > 1 && fileParts[1] == "--append";

        try
        {
            // Check if file exists
            DirectoryEntry existing = dir.FindEntry(currentDirCluster, filename);

            // File exists, write or append
            fm.WriteText(currentDirCluster, filename, text, append);
            Console.WriteLine($"{(append ? "Appended" : "Wrote")} text to '{filename}'");
        }
        catch
        {
            // File doesn't exist, create it
            byte[] data = Encoding.ASCII.GetBytes(text);
            fm.CreateFile(currentDirCluster, filename, data);
            Console.WriteLine($"Created '{filename}' with text");
        }
    }
}