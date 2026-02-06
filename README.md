# AvionX Ground Control Station

Modern Ground Control Station (GCS) for UAV operations, built with Avalonia UI and .NET.

## Features

### 📍 GPS Tracking & Map Display
- Real-time GPS position tracking
- Satellite map background
- Flight path visualization
- Drone-centered camera system
- Distance to home calculation

### 🎯 Flight Instruments
- **Artificial Horizon**: Real-time pitch and roll display with HUD overlay
- **Heading Compass**: 360° rotating compass with cardinal directions
- **Altimeter**: Altitude monitoring
- **Speed Indicators**: Ground speed, airspeed, and vertical speed

### 🔋 Telemetry Monitoring
- Battery level with visual indicator (8-bar display)
- Voltage monitoring
- Estimated flight time remaining
- Connection status (Uplink/Downlink)

### 🎮 Flight Control
- Connect/Disconnect to UAV
- Real-time telemetry updates
- Simulated flight data for testing

## Technology Stack

- **Framework**: .NET 10.0
- **UI**: Avalonia 11.3.11 (Cross-platform XAML)
- **Architecture**: MVVM with ReactiveUI
- **DI**: Microsoft.Extensions.DependencyInjection
- **Language**: C#

## Project Structure

```
AvionX/
├── Models/              # Data models (GPS coordinates, telemetry)
├── ViewModels/          # MVVM ViewModels with ReactiveUI
├── Views/               # Avalonia XAML views
│   ├── Controls/        # Custom controls (Compass, Horizon, Map, Battery)
│   └── MainWindow.axaml # Main application window
├── Services/            # Business logic and connection services
│   ├── Interfaces/      # Service interfaces
│   └── Impl/            # Service implementations
└── Assets/              # Images, fonts, and resources

```

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- macOS / Linux / Windows

### Build & Run

```bash
# Clone the repository
git clone https://github.com/yourusername/AvionX-GCS.git
cd AvionX-GCS

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

### Publish Self-Contained Executable

```bash
# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true

# macOS x64 (Intel)
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

## Features in Detail

### Custom Controls

#### Artificial Horizon
- Real-time pitch and roll visualization
- Rotating horizon line
- Pitch ladder markings
- HUD-style aircraft symbol
- Compass strip at top

#### Heading Compass
- Mathematically perfect symmetry
- 15° interval tick marks
- Cardinal and intercardinal directions
- Rotating compass dial
- Fixed aircraft symbol at center

#### GPS Map
- Drone-centered camera system
- Satellite imagery background support
- Real-time position updates
- Flight path trail
- Home location marker

#### Battery Gauge
- Horizontal battery icon with 8 bars
- Color-coded status (green/yellow/orange/red)
- Percentage display
- Voltage and time remaining indicators

## Architecture Highlights

### MVVM Pattern
- Clean separation of concerns
- ReactiveUI for reactive programming
- Property change notifications
- Command pattern for user interactions

### Dependency Injection
- Service-based architecture
- Interface-driven design
- Easy testing and mocking
- Loose coupling

### Simulated Telemetry
- Physics-based drone simulation
- Roll affects heading
- Heading affects GPS position
- Realistic flight dynamics

## Customization

### Adding Custom Map Tiles
Place satellite imagery in `Assets/satellite-map.jpg` for custom map backgrounds.

### Modifying Telemetry Update Rate
Edit `SimulatedConnectionService.cs` timer interval (default: 100ms).

### Changing UI Colors
All colors are defined in XAML for easy theming.

## Contributing

This project was developed as an internship application project for Baykar Technologies.

## License

MIT License - See LICENSE file for details

## Acknowledgments

- Avalonia UI team for the excellent cross-platform framework
- ReactiveUI for reactive MVVM support
- Baykar Technologies for the internship opportunity

---

**Built with ❤️ using Avalonia UI and .NET**
