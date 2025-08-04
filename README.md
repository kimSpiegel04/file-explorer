# File Explorer SPA

A single-page file explorer built with C# (.NET 9) and vanilla JavaScript. 

This proof-of-concept demonstrates file system browsing, searching, uploading, downloading, and basic file operations — all rendered client-side via deep-linkable URLs. No third-party frameworks present.

---

## Features

- Browse and search directories and files
- Deep-linkable SPA (state lives in the URL)
- Folder/file counts and total size summary
- Upload and download files directly from the browser
- Delete, copy, and move files or folders
- Configurable root directory set at server startup
- Pagination, sorting, and search performance tuning
- Clean, reactive frontend UI using plain JS

---

## Setup Instructions

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### Run Locally

```bash
git clone https://github.com/kimSpiegel04/file-explorer.git
cd file-explorer
dotnet run "/absolute/path/to/your/root/directory"
