# FAT File System Simulator

A C# implementation of a simplified **FAT-style file system** built on top of a virtual disk. The project simulates how an operating system manages storage using clusters, a File Allocation Table (FAT), directories, and files.

## Features

* Virtual disk implemented as a binary file
* 1 MB virtual disk with 1024-byte clusters
* File Allocation Table (FAT)
* Cluster-chain allocation and deallocation
* Root directory and directory entries
* File and directory management
* FAT persistence to the virtual disk
* 8.3 filename formatting
* Interactive command-line file-system shell
* Protection against invalid cluster access and cyclic FAT chains

## Architecture

The file system is organized into several layers:

```text
                    ┌─────────────────────┐
                    │    Command Shell    │
                    └──────────┬──────────┘
                               │
              ┌────────────────┴────────────────┐
              │                                 │
      ┌───────▼────────┐               ┌────────▼────────┐
      │ Directory/File │               │ FAT Table       │
      │ Management     │               │ Manager         │
      └───────┬────────┘               └────────┬────────┘
              │                                 │
              └────────────────┬────────────────┘
                               │
                     ┌─────────▼─────────┐
                     │   Virtual Disk    │
                     └─────────┬─────────┘
                               │
                     virtual_disk.bin
```

### Main Components

| Component              | Responsibility                                                   |
| ---------------------- | ---------------------------------------------------------------- |
| `VirtualDisk.cs`       | Creates, opens, reads, writes, and closes the virtual disk       |
| `FSConstants.cs`       | Defines cluster sizes, reserved clusters, and file-system layout |
| `FatTableManager.cs`   | Manages FAT entries and cluster allocation                       |
| `Directory.cs`         | Handles directory lookup, insertion, and deletion                |
| `DirectoryEntry.cs`    | Represents and serializes directory entries                      |
| `SuperblockManager.cs` | Manages superblock information                                   |
| `Program.cs`           | Initializes the file system and starts the shell                 |

## Virtual Disk Layout

The simulated disk contains **1024 clusters**, with each cluster being **1024 bytes**, giving a total size of approximately **1 MB**.

```text
Cluster 0
┌──────────────────────────────┐
│          Superblock          │
└──────────────────────────────┘

Clusters 1 - 4
┌──────────────────────────────┐
│             FAT              │
│       1024 FAT entries       │
└──────────────────────────────┘

Cluster 5+
┌──────────────────────────────┐
│        File / Directory      │
│            Data              │
└──────────────────────────────┘
```

The first five clusters are reserved, while clusters starting from cluster 5 can be allocated for file-system content.

## File Allocation Table

The FAT keeps track of how files are distributed across the disk.

For example, a file might occupy:

```text
10 → 15 → 22 → -1
```

This means:

* The file starts at cluster `10`
* Cluster `10` points to `15`
* Cluster `15` points to `22`
* `-1` marks the end of the chain

The simulator supports:

* Reading and writing FAT entries
* Loading the FAT from disk
* Flushing the FAT back to disk
* Following cluster chains
* Allocating clusters using a first-fit strategy
* Releasing clusters when files are deleted

## Directory Entries

Each directory entry occupies **32 bytes** and stores information such as:

```text
┌───────────────┬───────┬──────────────┬───────────┐
│ Name (11 B)   │ Attr. │ First Cluster│ File Size │
└───────────────┴───────┴──────────────┴───────────┘
```

Filenames use a simplified **8.3 format**:

```text
FILENAMEEXT
```

The directory manager supports:

* Searching for files/directories
* Adding entries
* Removing entries
* Traversing directory cluster chains
* Automatically allocating additional directory space when needed

## Getting Started

### Requirements

* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* Windows, Linux, or macOS

### Clone the Repository

```bash
git clone https://github.com/rahmaaref/FATFileSystemSimulator.git
cd FATFileSystemSimulator
```

### Build the Project

```bash
dotnet build
```

### Run

```bash
dotnet run
```

## Technical Stack

* **Language:** C#
* **Framework:** .NET 9
* **Storage:** Binary file / virtual disk
* **Interface:** Command-line
* **Architecture:** Layered file-system simulation
