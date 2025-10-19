# Northern Roads Patcher

A Synthesis patcher for Skyrim Special Edition that fixes terrain holes and resolves conflicts with Northern Roads and its patches.

## Overview

This patcher automatically detects and forwards landscape records from Northern Roads (and patches containing "Northern Roads" in their name) to resolve conflicts and prevent terrain holes that can occur when multiple mods modify the same landscape areas.

## Features

- **Automatic Conflict Resolution**: Forwards the last winning Northern Roads landscape record to prevent conflicts with other mods
- **Patch-Aware Detection**: Uses name heuristics to detect Northern Roads patches automatically
- **Comprehensive Data Forwarding**: Forwards landscape data along with associated cell data including:
  - Water height
  - Max height data
  - Occlusion data
  - Image space settings
- **Terrain Hole Prevention**: Ensures terrain continuity by properly handling landscape records

## Requirements

- Skyrim Special Edition
- [Synthesis](https://github.com/Mutagen-Modding/Synthesis) patcher runner
- Northern Roads.esp

## Installation

1. Download and install [Synthesis](https://github.com/Mutagen-Modding/Synthesis)
2. Add this patcher to your Synthesis pipeline:
   - Open Synthesis
   - Click "Add Patcher" → "Git Repository"
   - Enter the repository URL: `https://github.com/JunaMeinhold/NorthernRoadsPatcher`
3. Ensure Northern Roads.esp is in your load order
4. Run Synthesis to generate the patch

## How It Works

The patcher:
1. Scans all landscape records in your load order
2. Identifies records where Northern Roads (or any mod with "Northern Roads" in its name) has made changes
3. Forwards the last winning Northern Roads landscape record
4. Copies associated cell data (water height, max height data, occlusion data)
5. Generates a patch (NorthernRoadsPatcher.esp) that resolves conflicts

## Usage

Simply run Synthesis after adding this patcher to your pipeline. The patcher will automatically:
- Detect Northern Roads and its patches
- Process all relevant landscape records
- Generate a patch that should be loaded after Northern Roads and its patches

## Load Order

Place the generated `NorthernRoadsPatcher.esp` after:
- Northern Roads.esp
- Any Northern Roads patches
- Any other landscape-modifying mods you want to be compatible with

## Compatibility

This patcher is designed to be compatible with:
- Northern Roads and all its patches
- Other landscape-modifying mods
- Mods that add or modify worldspaces

## Building from Source

Requirements:
- .NET 8 SDK
- Visual Studio 2022 or later (optional)

Build steps:
```bash
git clone https://github.com/JunaMeinhold/NorthernRoadsPatcher.git
cd NorthernRoadsPatcher
dotnet build
```

## Troubleshooting

**Terrain holes still appear:**
- Ensure the generated patch is loaded after Northern Roads and its patches
- Check that Northern Roads.esp is in your load order
- Verify the patch was generated successfully in Synthesis

**Patcher doesn't detect Northern Roads patches:**
- The patcher looks for "Northern Roads" in the mod name (case-insensitive)
- Ensure your patch files contain "Northern Roads" in their filename

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/JunaMeinhold/NorthernRoadsPatcher/blob/master/LICENSE.txt) file for more details.

## Credits

Built with:
- [Mutagen](https://github.com/Mutagen-Modding/Mutagen) - Skyrim mod manipulation library
- [Synthesis](https://github.com/Mutagen-Modding/Synthesis) - Patcher pipeline framework

## Links

- [GitHub Repository](https://github.com/JunaMeinhold/NorthernRoadsPatcher)
- [Northern Roads](https://www.nexusmods.com/skyrimspecialedition/mods/77386) on Nexus Mods
- [Synthesis](https://github.com/Mutagen-Modding/Synthesis)
