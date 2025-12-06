# FTPSheep.NET

A command-line deployment tool designed specifically for .NET developers who build and deploy ASP.NET applications to servers using FTP protocol.

## Features

- 🚀 **Auto-discovery** - Automatically finds and uses Visual Studio publish profiles
- 🔐 **Secure** - Encrypted credential storage using Windows DPAPI
- ⚡ **Fast** - Concurrent file uploads with progress tracking
- 🎯 **Direct deployment** - Overwrites files in place with optional cleanup
- 🛡️ **IIS-friendly** - Automatic app_offline.htm handling to unlock files
- 🤖 **CI/CD ready** - Non-interactive mode for automation

## Quick Start

```bash
# Navigate to your project
cd MyWebProject

# Deploy (first run will auto-discover VS profiles)
ftpsheep deploy
```

## Status

🚧 **In Development** - This project is currently under active development.

## Documentation

- [Product Requirements Document](docs/prd.md)
- [Development Plan](docs/plan.md)

## Requirements

- .NET 8.0 SDK
- Windows (V1), macOS/Linux planned for future versions

## Building from Source

```bash
git clone https://github.com/yourusername/FTPSheep.git
cd FTPSheep
dotnet build
dotnet test
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.
