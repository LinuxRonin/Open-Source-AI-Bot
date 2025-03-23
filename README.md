# VRChat AI Chatbot 🤖🎮

An open-source AI chatbot designed to enhance social interactions in VRChat. Powered by natural language processing (NLP) and customizable personalities!

![VRChat AI Chatbot Demo](https://via.placeholder.com/800x400.png?text=Demo+Preview+Placeholder) *Replace with actual screenshot/video*

## Features ✨
- **Auto-Response System**: Reacts to in-game text/voice chat in real-time
- **NLP Integration**: Compatible with OpenAI GPT, Dialogflow, or Rasa
- **Customizable Personality**: Set moods (friendly, sarcastic, shy) via config
- **VR-Compatible Workflow**: Optimized for VRChat's API and performance
- **Voice Synthesis**: Optional text-to-speech (TTS) support
- **Open Source**: MIT Licensed - modify and share freely!

## Prerequisites 📋
- Python 3.8+
- [VRChat API Key](https://vrchat.com/home/developer)
- NLP Service API Key (OpenAI/Dialogflow/Rasa)
- (Optional) Voice Synthesis Tool (e.g., [gTTS](https://gtts.readthedocs.io/))
- VRChat Account with Moderation Permissions



## Setup

Follow these steps to get the VRChat AI Suite up and running:

1. **Clone the repository**:
   ```bash
   git clone https://github.com/LinuxRonin/Open-Source-AI-Bot.git
   cd vrchat-ai-suite
2. **Install Decencies'**
   ```bash
   pip install -r requirements.txt
3. **Configure Enviroment Tables**
- Copy .env.example to .env and fill in the required values (e.g., API keys).
4. **Configure the Bot**
- Copy config.json.example to config.json and adjust the settings as needed (e.g., VRChat rooms, blocked words, animations).
5. **Start the Bot**
   ```bash
   python bot/main.py

## Configuration
- The bot requires configuration through two files: **.env** and **config.json.**
