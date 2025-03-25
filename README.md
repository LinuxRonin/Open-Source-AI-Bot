# 🤖 VRChat World AI Chatbot

<div align="center">

![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)
![VRChat SDK](https://img.shields.io/badge/VRChat%20SDK-3.0-5865f2.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Last Updated](https://img.shields.io/badge/Updated-2025--03--25-orange.svg)

**Create interactive NPCs for your VRChat worlds - no external services required!**

[Features](#-features) •
[Requirements](#-requirements) •
[Installation](#-installation) •
[How It Works](#-how-it-works) •
[Customization](#-customization) •
[Performance](#-performance) •
[Guidelines](#-vrchat-guidelines)

</div>

## ✨ Features

- **🔒 No External Authentication** - Uses VRChat's native permission system
- **👋 Proximity Detection** - Chat UI appears when players approach
- **💬 Keyword Response System** - Simple NLP using customizable keyword matching
- **🎭 Avatar Interaction** - Responds to player gestures and movements
- **🌐 Multiplayer Ready** - Optimized for busy VRChat worlds
- **📝 Well-Documented** - Extensively commented code for easy customization
- **🚀 Performance Optimized** - Minimal impact on frame rates
- **📱 Desktop & VR Compatible** - Works seamlessly across platforms

## 📋 Requirements

- Unity 2022.3 or newer
- VRChat SDK3
- UdonSharp
- Basic understanding of Unity and Udon

## 📥 Installation

1. **Download** the package from [Releases](https://github.com/LinuxRonin/Open-Source-AI-Bot/releases)
2. **Import** into your Unity VRChat world project
3. **Drag** the `AIAssistant` prefab into your scene
4. **Adjust** the proximity trigger to your liking
5. **Customize** the response database (see Customization section)
6. **Test** in Play mode before uploading to VRChat

## 🔍 How It Works

### Architecture Overview

The system consists of three core components working together:


</div>

### Proximity Detection

Uses VRChat's EventTrigger system to detect when players are within a customizable range. When a player enters this zone, the chat UI automatically appears.

### Chat UI System

A clean, intuitive interface powered by Unity's Canvas system that:
- Displays NPC responses with typewriter effect
- Provides input field for player messages
- Shows conversation history
- Automatically scales for desktop and VR

### Response System

The heart of the AI chatbot using a keyword-matching NLP system:
- Analyzes player input for keywords
- Matches against a customizable database
- Applies priority weighting to select best response
- Supports random variation for natural conversation flow

### Avatar Interaction

The system can detect and respond to VRChat-specific player actions:
- Wave detection triggers greeting responses
- Jump detection for playful interactions
- Configurable gesture recognition

## 🎨 Customization

### Response Database Editor

The system comes with an intuitive editor for customizing responses:

<div align="center">


</div>

### UI Customization

All UI elements are easily customizable through the Unity Inspector:
- Colors, fonts, and background images
- Chat bubble size and positioning
- Animation timing and effects
- Sound effects (typing, message received)

### Personality Templates

Choose from pre-built personalities or create your own:
- **Friendly Guide** - Helpful, informative, perfect for tutorials
- **Mysterious Character** - Cryptic, intriguing, great for story-driven worlds
- **Comic Relief** - Funny, sarcastic, adds humor to any setting

## 🚀 Performance

The system is designed with VRChat's performance requirements in mind:

- **Efficient Trigger System** - Uses optimized colliders for detection
- **Event-Based Architecture** - Minimal Update() usage reduces CPU load
- **Batched UI Elements** - Reduced draw calls for better rendering performance
- **Memory-Efficient Responses** - Text-based system with minimal overhead

### Performance Metrics

| Player Count | FPS Impact | Memory Usage |
|--------------|------------|--------------|
| 1-10 players | < 0.5 ms   | ~5 MB        |
| 11-25 players| < 1.0 ms   | ~5 MB        |
| 26+ players  | < 1.5 ms   | ~5 MB        |

## 📊 VRChat Guidelines

This system is designed to comply with VRChat's world submission guidelines:

- ✅ **No External Services** - 100% self-contained within VRChat
- ✅ **No Sensitive Data Collection** - All processing happens locally
- ✅ **Performance Optimized** - Minimal impact on frame rates
- ✅ **Cross-Platform Compatible** - Works on PC, VR, and Quest
- ✅ **Appropriate Content Control** - Response database can be moderated

## 🔧 Troubleshooting

<details>
<summary><b>Common Issues & Solutions</b></summary>

### Chat UI doesn't appear when approaching NPC
- Check that the EventTrigger collider is properly sized
- Verify the Canvas is set to "World Space"
- Make sure the proximity script is active

### Bot doesn't respond to input
- Check that keywords in your response database match what users might type
- Verify input field is properly connected to the response system
- Make sure priority values are appropriate

### Performance issues in large worlds
- Reduce UI complexity (fewer visual effects)
- Decrease check frequency in proximity detection
- Optimize response database size

</details>

## 📘 Documentation

For complete documentation, visit the [Wiki](https://github.com/LinuxRonin/Open-Source-AI-Bot/wiki)

## 🤝 Contributing

Contributions welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

Created by [LinuxRonin](https://github.com/LinuxRonin) | Last Updated: 2025-03-25

**Made with ❤️ for the VRChat community**

</div>
