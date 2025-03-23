# OpenSource_AI_Bot
Open Source AI Chat bot for use in VRCHAT. Similar to "Celeste-AI"

# VRChat AI Suite

A monorepo for a VRChat bot that provides real-time chat interactions, avatar control, and content moderation using OpenAI and VRChat APIs.

## Features

- **Real-time Chat**: Engages with users in VRChat via WebSocket, generating responses using OpenAI's GPT-3.
- **Avatar Control**: Triggers animations on the bot's avatar using VRChat's REST API.
- **Content Moderation**: Filters inappropriate messages based on a configurable list of blocked words.
- **Monorepo Structure**: Unified development, shared configuration, and modular design for easier maintenance and deployment.


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
