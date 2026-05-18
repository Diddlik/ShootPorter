# Phlow

A cross-platform photo and video downloader and organizer for professional photographers.

Phlow transfers images from card readers, cameras (PTP/MTP), and local folders with smart deduplication, token-based dynamic file/folder naming from EXIF/IPTC metadata, GPS sync, and plugin-based post-processing.

Built with **.NET 10** and **Avalonia UI** — runs on Windows, macOS, and Linux.

---

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/b197a7e2-2f0c-412f-ae81-647e6cda6533" />
---
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/0f2e2b06-55ab-4790-8912-79a5b45dc47c" />

## Features

### Core Download Engine
- **Multi-source support** — card readers, USB cameras, local/network folders
- **Parallel file transfers** with configurable concurrency and progress reporting
- **Smart deduplication** — size, timestamp, and SHA-256 verification to prevent overwrites
- **Backup destinations** — copy to up to 2 backup paths simultaneously
- **Auto-delete from source** after verified transfer (optional)

### Token-Based Dynamic Naming
A powerful template engine for destination paths and filenames using `{tokens}`:

| Token | Description | Example |
|-------|-------------|---------|
| `{Y}`, `{m}`, `{D}` | Year, month, day | `2026`, `03`, `29` |
| `{H}`, `{M}`, `{S}` | Hour, minute, second | `14`, `30`, `05` |
| `{J}` | Job code | `Wedding_Smith` |
| `{F}`, `{e}` | Original filename, extension | `IMG_1234`, `CR3` |
| `{seq#4}` | Sequence counter (zero-padded) | `0001`, `0002` |
| `{file[5-10]}` | Filename character slice | Substring extraction |

String functions: `{upper,...}`, `{lower,...}`, `{left,n,...}`, `{right,n,...}`, `{mid,n,m,...}`, `{capitalize,...}`, `{default,...}`, `{if,...}`

**Example template:** `{Y}-{m}-{D}\{J}\{F}.{e}` produces `2026-03-29\Wedding_Smith\IMG_1234.CR3`

### Metadata Extraction
- EXIF: date/time, ISO, aperture, shutter speed, camera model, serial number
- GPS coordinates (latitude, longitude, altitude)
- IPTC keywords
- Image dimensions, copyright, artist

### GPS & Geo-tagging
- GPX track file parsing and photo matching via timestamp correlation
- Reverse geocoding through the [GeoNames](https://www.geonames.org/) API
- Binary search with linear interpolation for accurate track point matching

### Plugin System
Interface-based post-download processing with ordered execution:
- **DirectoryMakerPlugin** — auto-create subdirectories (originals, edited, etc.)
- **DngConverterPlugin** — Adobe DNG Converter integration
- **JpegRotatorPlugin** — auto-rotation based on EXIF orientation

### Profiles & Settings
- Named download profiles (destination, template, backups, parallelism)
- Camera alias mapping — assign friendly names to camera model IDs
- Job code history with 60-day auto-pruning
- JSON-based persistence

### Drive Watcher
- Automatic detection of USB/SD card insertion
- Removable drive enumeration with event-driven notifications

### CLI Mode
```
phlow --source E:\ --dest "C:\Photos" --profile Wedding --jobcode "Smith_2026" --parallel 4 --headless
```

| Flag | Description |
|------|-------------|
| `--source` | Source path |
| `--dest` | Destination root |
| `--profile` | Named profile to use |
| `--jobcode` | Job code value |
| `--parallel` | Number of concurrent copies |
| `--no-verify` | Skip SHA-256 verification |
| `--auto-delete` | Delete source files after transfer |
| `--headless` | Run without UI |

### Auto-Update
Automatic update checking and installation via [Velopack](https://velopack.io/) with GitHub Releases.

---

## Supported Formats

| Category | Formats |
|----------|---------|
| **Images** | JPEG, TIFF, PNG, BMP, GIF, WEBP, HEIF, HEIC |
| **RAW** | CR2, CR3, NEF, ARW, DNG, RAF, ORF, PEF, GPR, RW2, SRW, 3FR, IIQ, MOS, ERF, KDC, DCR, X3F |
| **Video** | MOV, MP4, AVI, MTS, M2TS, CRM, MXF |

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 10 |
| **UI Framework** | [Avalonia UI](https://avaloniaui.net/) 11.x |
| **UI Theme** | [FluentAvalonia](https://github.com/amwx/FluentAvalonia) (Windows 11 Fluent Design) |
| **MVVM** | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| **Metadata** | [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) |
| **Auto-Update** | [Velopack](https://velopack.io/) |
| **Testing** | [xUnit](https://xunit.net/) |
| **Persistence** | JSON files |

---

## Project Structure

```
Phlow_V2/
├── src/
│   ├── Phlow.App/              # Avalonia desktop application
│   │   ├── Models/             # Data models
│   │   ├── Services/           # Application services (updates, etc.)
│   │   ├── ViewModels/         # MVVM ViewModels
│   │   └── Views/              # AXAML views and settings pages
│   │
│   └── Phlow.Core/             # Business logic library
│       ├── Cli/                # Command-line argument parsing
│       ├── Dedup/              # File deduplication engine
│       ├── Discovery/          # File system scanning & drive monitoring
│       ├── Download/           # Parallel download orchestration
│       ├── Geo/                # GPS sync & geocoding
│       ├── Metadata/           # EXIF/IPTC extraction
│       ├── Plugins/            # Post-download plugin system
│       ├── Profiles/           # User profiles & settings persistence
│       └── Tokens/             # Token template engine
│
├── tests/
│   └── Phlow.Core.Tests/       # 130 xUnit tests
│
└── Docs/                       # Requirements & design documents
```

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/your-username/phlow.git
cd phlow

# Build
dotnet build

# Run
dotnet run --project src/Phlow.App

# Run tests
dotnet test
```

---

## Architecture

Phlow follows **strict MVVM** with a clean separation between UI and business logic:

- **Phlow.Core** — platform-agnostic library containing all business logic. Zero UI dependencies.
- **Phlow.App** — thin Avalonia shell that binds ViewModels to Views. Views contain no business logic.

Key design decisions:
- **Async end-to-end** — no sync-over-async; `CancellationToken` propagated throughout
- **Plugin architecture** — `IPostDownloadPlugin` interface for extensible post-processing
- **Atomic file operations** — SHA-256 verification before any destructive action
- **Record types** for immutable DTOs and data structures

---

## Legacy Context

Phlow is a modern rewrite of **Downloader Pro** by Breeze Systems, a Windows utility used by professional photographers for over a decade. The `_old/` directory contains screenshots and documentation from the original application for feature reference.

---

## License

TBD
