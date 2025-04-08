# 🤖 VRChat World AI Chatbot

<div align="center">

![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)
![VRChat SDK](https://img.shields.io/badge/VRChat%20SDK-3.0-5865f2.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Last Updated](https://img.shields.io/badge/Updated-2025--04--07-orange.svg)

**Create interactive NPCs for your VRChat worlds with AI integration!**

[Features](#-features) •
[Requirements](#-requirements) •
[Installation](#-installation) •
[How It Works](#-how-it-works) •
[Configuration](#-configuration) •
[Performance](#-performance) •
[Guidelines](#-vrchat-guidelines) •
[Contributing](#-contributing)

</div>

## ✨ Features

- **🔒 Local Backend Service** - Python-based backend with no external dependencies
- **⚡ OSC Communication** - Seamless integration with VRChat's OSC protocol
- **👋 Proximity Detection** - Chat UI appears when players approach
- **💬 AI Response System** - Knowledge-based responses to player interactions
- **🎭 Avatar Interaction** - Responds to player gestures and movements
- **📢 Discord Notifications** - Get alerted when players interact with items
- **🌐 Multiplayer Ready** - Optimized for busy VRChat worlds
- **📝 Fully Documented** - Extensively commented code for easy customization
- **🚀 Performance Optimized** - Minimal impact on frame rates
- **📱 Desktop & VR Compatible** - Works seamlessly across platforms

## 📋 Requirements

- **Unity:** 2022.3 or newer
- **VRChat SDK:** SDK3 Worlds
- **UdonSharp:** Latest version
- **Python:** 3.8+ (for backend service)
- **Dependencies:** Flask, python-osc, requests (included in requirements.txt)
- **Basic knowledge:** Unity, Udon, Python (for customization)

## 📥 Installation

### VRChat World Setup (Unity)

1. **Download** the package from [Releases](https://github.com/LinuxRonin/Open-Source-AI-Bot/releases)
2. **Import** into your Unity VRChat world project
3. **Drag** the `AIAssistant` prefab into your scene
4. **Configure** the scripts:
   - Assign the `OSCManager` component to all `InteractableItem` instances
   - Set up the `ChatUIManager` with appropriate UI elements
5. **Build & Test** in Unity Play mode before uploading

### Backend Service Setup (Python)

1. **Clone** this repository to your local machine
2. **Install** dependencies: `pip install -r requirements.txt`
3. **Copy** `Config Example.json` to `config.json` and customize:
   ```json
   {
     "osc_server_ip": "0.0.0.0",
     "osc_server_port": 9001,
     "vrchat_client_ip": "127.0.0.1",
     "vrchat_client_port": 9000,
     "discord_webhook_url": "YOUR_DISCORD_WEBHOOK_URL_HERE",
     "knowledge_base_file": "knowledge_base.json",
     "owner_discord_ids": ["<@YOUR_DISCORD_USER_ID>"]
   }
   ```
4. **Create** a `knowledge_base.json` file for AI responses (example below)
5. **Run** the backend service: `python "Python Backend Service.py"`

## 🔍 How It Works

### System Architecture

The system consists of three core components:

1. **Unity/VRChat Components:**
   - `InteractableItem.cs`: Handles player interactions with objects
   - `OSCManager.cs`: Facilitates communication with the backend
   - `ChatUIManager.cs`: Manages the UI for player input and bot responses

2. **Python Backend Service:**
   - Listens for OSC messages from VRChat
   - Processes player inputs using a knowledge base
   - Sends responses back to VRChat via OSC
   - Dispatches Discord notifications for important events

3. **Configuration Files:**
   - `config.json`: Backend service settings
   - `knowledge_base.json`: Custom responses database
   - `requirements.txt`: Python dependencies

### Communication Flow

```
Player in VRChat → InteractableItem → OSCManager → Python Backend → AI Processing → OSC → VRChat Avatar Parameters → Chat Display
```

### Knowledge Base Structure

The system uses a simple but effective JSON-based knowledge base:

```json
{
  "coffee": {
    "aliases": ["java", "brew", "espresso"],
    "description": "We serve fresh coffee from local roasters!",
    "details": "Prices range from $3-$5.",
    "website": "coffee.example.com",
    "contact": "barista@example.com"
  },
  "art gallery": {
    "aliases": ["paintings", "artwork", "exhibit"],
    "description": "Our gallery features rotating exhibits from local artists.",
    "details": "Open weekends 10am-8pm.",
    "website": "gallery.example.com",
    "contact": "curator@example.com"
  }
}
```

## ⚙️ Configuration

### Backend Configuration

The `config.json` file controls the backend service:

| Setting | Description | Default |
|---------|-------------|---------|
| `osc_server_ip` | IP to listen on for OSC messages | `0.0.0.0` |
| `osc_server_port` | Port to listen on | `9001` |
| `vrchat_client_ip` | VRChat client IP | `127.0.0.1` |
| `vrchat_client_port` | VRChat client port | `9000` |
| `discord_webhook_url` | Discord notification URL | `""` |
| `knowledge_base_file` | Path to responses database | `knowledge_base.json` |
| `owner_discord_ids` | Discord IDs to ping | `[]` |

### UI Customization

All UI elements are easily customizable through the Unity Inspector:
- Chat bubble size and positioning
- Font styles and colors
- Input field appearance
- Background transparency

### Interactable Items

For each interactive item in your world:
1. Add the `InteractableItem` component
2. Set a unique `itemId` and descriptive `itemDisplayName`
3. Reference your scene's `OSCManager` instance

## 🚀 Performance

The system is designed with VRChat's performance requirements in mind:

- **Optimized OSC Communication**: Lightweight messaging system
- **Efficient Python Backend**: Low resource utilization
- **Event-Based Architecture**: Minimal Update() usage
- **Background Threading**: Server operations don't block

### Performance Metrics

| Component | Resource Usage | Notes |
|-----------|---------------|-------|
| Unity Scripts | ~0.5ms CPU | Per interactive object |
| Python Backend | ~15MB RAM | Single instance for all players |
| OSC Network | ~1KB/message | Minimal bandwidth required |

## 📊 VRChat Guidelines

This system is designed to comply with VRChat's world submission guidelines:

- ✅ **Limited External Services** - Only optional Discord notifications
- ✅ **No Sensitive Data Collection** - Only processes public username/ID
- ✅ **Performance Optimized** - Minimal impact on frame rates
- ✅ **Cross-Platform Compatible** - Works on PC and Quest
- ✅ **Appropriate Content Control** - Customizable knowledge base

## 🔧 Troubleshooting

<details>
<summary><b>Common Issues & Solutions</b></summary>

### VRChat Client Issues

- **OSC Communication Failing**
  - Ensure OSC is enabled in VRChat settings (Options → OSC → Enabled)
  - Verify ports are not blocked by firewall
  - Check Unity script references

### Backend Service Issues

- **Service Won't Start**
  - Check if another application is using port 9001
  - Verify Python 3.8+ is installed
  - Ensure all dependencies are installed via requirements.txt

### Discord Notification Issues

- **Notifications Not Arriving**
  - Verify webhook URL is correct
  - Check Discord server permissions
  - Ensure internet connectivity

</details>

## 📘 Advanced Usage

### Extending the Knowledge Base

The knowledge base system supports:
- Multiple aliases for the same concept
- Rich details with websites and contact info
- Hierarchical categorization (by nesting objects)

### Custom Backend Integration

Advanced users can modify the Python backend to:
- Connect to external AI services like OpenAI
- Add database storage for conversation history
- Implement more sophisticated NLP techniques

## 🤝 Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

Before submitting a pull request:
1. Ensure code follows project style guidelines
2. Test thoroughly in both desktop and VR modes
3. Document any new features or changes

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

Last Updated: 2025-04-07

Created by [LinuxRonin](https://github.com/LinuxRonin)

Co-Authored by [Rey](https://github.com/ReyingRexer)

Managed by [Ike](https://github.com/xMrIKEx)

**Made with ❤️ for the VRChat community**

</div>
