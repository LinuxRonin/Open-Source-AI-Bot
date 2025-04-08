# --- Backend AI Chatbot Service for VRChat ---
# Designed for running on a personal desktop computer alongside VRChat.
# Uses Flask web framework and python-osc library.
# Communicates with VRChat via OSC.
# Sends notifications to Discord via Webhooks.
# Loads configuration from 'config.json'.

# --- Installation ---
# 1. Ensure Python 3 is installed.
# 2. Create a 'requirements.txt' file with the content below.
# 3. Run: pip install -r requirements.txt

import json
import threading
import datetime
import os
import sys
from flask import Flask, request, jsonify
from pythonosc import dispatcher, osc_server, udp_client
import requests # For sending Discord webhooks

# --- Default Configuration ---
# These values are used if 'config.json' is missing or incomplete.
DEFAULT_CONFIG = {
    "osc_server_ip": "0.0.0.0",  # Listen on all local network interfaces
    "osc_server_port": 9001,     # Port this script listens on for messages FROM VRChat
    "vrchat_client_ip": "127.0.0.1", # IP address VRChat listens on (usually localhost)
    "vrchat_client_port": 9000,     # Port VRChat listens on for messages TO VRChat (default)
    "discord_webhook_url": "",   # <<< IMPORTANT: Set this in config.json >>>
    "knowledge_base_file": "knowledge_base.json",
    "owner_discord_ids": []      # <<< IMPORTANT: Set user IDs in config.json (e.g., ["<@12345>", "<@67890>"]) >>>
}

# Global variable to hold the loaded configuration
CONFIG = {}

# --- Configuration Loading ---
def load_config():
    """Loads configuration from config.json, using defaults if keys are missing."""
    global CONFIG
    config_file = 'config.json'
    loaded_config = {}

    if not os.path.exists(config_file):
        print(f"Warning: '{config_file}' not found. Creating one with default values.")
        print("!!! Please edit 'config.json' with your Discord Webhook URL and Owner IDs! !!!")
        try:
            with open(config_file, 'w') as f:
                json.dump(DEFAULT_CONFIG, f, indent=4)
            loaded_config = DEFAULT_CONFIG.copy()
        except IOError as e:
            print(f"Error: Could not create '{config_file}': {e}")
            print("Using default configuration values.")
            loaded_config = DEFAULT_CONFIG.copy()
    else:
        try:
            with open(config_file, 'r') as f:
                user_config = json.load(f)
            # Merge user config with defaults (defaults are fallback)
            loaded_config = DEFAULT_CONFIG.copy()
            loaded_config.update(user_config)
            print(f"Configuration loaded successfully from '{config_file}'.")
        except json.JSONDecodeError:
            print(f"Error: Could not decode JSON from '{config_file}'. Check its format.")
            print("Using default configuration values.")
            loaded_config = DEFAULT_CONFIG.copy()
        except Exception as e:
            print(f"An unexpected error occurred loading config: {e}")
            print("Using default configuration values.")
            loaded_config = DEFAULT_CONFIG.copy()

    # Assign to global CONFIG
    CONFIG = loaded_config

    # Validate critical configurations
    if not CONFIG.get("discord_webhook_url"):
        print("Warning: Discord Webhook URL is not set in config.json. Notifications will be disabled.")
    if not CONFIG.get("owner_discord_ids"):
        print("Warning: Owner Discord IDs are not set in config.json. Notifications will not ping anyone.")


# --- Knowledge Base ---
knowledge_base = {}

def load_knowledge_base():
    """Loads business/community info from the JSON file specified in config."""
    global knowledge_base
    kb_file = CONFIG.get("knowledge_base_file", "knowledge_base.json") # Fallback filename
    try:
        with open(kb_file, 'r') as f:
            knowledge_base = json.load(f)
        print(f"Knowledge base loaded successfully from '{kb_file}'.")
    except FileNotFoundError:
        print(f"Warning: Knowledge base file '{kb_file}' not found. Starting with empty base.")
        knowledge_base = {}
    except json.JSONDecodeError:
        print(f"Error: Could not decode JSON from '{kb_file}'. Check file format.")
        knowledge_base = {}
    except Exception as e:
        print(f"An unexpected error occurred loading the knowledge base: {e}")
        knowledge_base = {}

# --- OSC Communication ---
# OSC Client to send messages back to VRChat
# Initialized later, after config is loaded
vrchat_osc_client = None

def initialize_osc_client():
    """Initializes the OSC client based on loaded configuration."""
    global vrchat_osc_client
    try:
        ip = CONFIG.get("vrchat_client_ip", "127.0.0.1")
        port = CONFIG.get("vrchat_client_port", 9000)
        vrchat_osc_client = udp_client.SimpleUDPClient(ip, port)
        print(f"OSC Client initialized to send to VRChat at {ip}:{port}")
    except Exception as e:
        print(f"Error initializing OSC Client: {e}")
        vrchat_osc_client = None # Ensure it's None if initialization fails

def send_osc_to_vrchat(address, args):
    """Sends an OSC message to the VRChat client."""
    if vrchat_osc_client is None:
        print("OSC Client not initialized. Cannot send message.")
        return
    try:
        vrchat_osc_client.send_message(address, args)
        # Reduce console spam for frequent messages like chat
        if address != "/avatar/parameters/Chatbox":
             print(f"Sent OSC to VRChat ({CONFIG['vrchat_client_ip']}:{CONFIG['vrchat_client_port']}): {address} {args}")
    except Exception as e:
        print(f"Error sending OSC to VRChat: {e}")

# --- Notification Manager ---
def send_discord_notification(message):
    """Sends a message to the configured Discord webhook."""
    webhook_url = CONFIG.get("discord_webhook_url")
    if not webhook_url:
        # print("Discord webhook URL not configured. Skipping notification.") # Already warned at startup
        return

    owner_ids = CONFIG.get("owner_discord_ids", [])
    owner_pings = " ".join(owner_ids) if owner_ids else ""

    payload = {
        "content": f"{owner_pings}\n{message}".strip() # Ping owners and add the message content
        # You can customize this payload further (embeds, username, avatar)
        # "username": "VRChat Bot",
        # "avatar_url": "URL_TO_AVATAR_IMAGE",
        # "embeds": [{ ... }]
    }
    try:
        response = requests.post(webhook_url, json=payload, timeout=10)
        response.raise_for_status() # Raise an exception for bad status codes (4xx or 5xx)
        print("Discord notification sent successfully.")
    except requests.exceptions.RequestException as e:
        print(f"Error sending Discord notification: {e}")
    except Exception as e:
        print(f"An unexpected error occurred sending Discord notification: {e}")


# --- AI Core (Simple Placeholder) ---
def get_ai_response(player_name, message):
    """
    Processes a player's message and generates a response based on the knowledge base.
    Replace this with a more sophisticated NLP/LLM approach if needed.
    """
    message_lower = message.lower().strip()
    response = f"Sorry {player_name}, I didn't quite catch that. You can ask me about the businesses or topics I know."

    # Check if the message asks about a known business/topic
    matched = False
    for key, data in knowledge_base.items():
        # Check if the business name (key) or any defined aliases are mentioned
        triggers = [key.lower()] + [alias.lower() for alias in data.get("aliases", [])]
        # Use word boundaries or smarter matching if needed to avoid partial matches
        if any(f" {trigger} " in f" {message_lower} " or message_lower.startswith(trigger) or message_lower.endswith(trigger) or message_lower == trigger for trigger in triggers):
            # Found a match, provide the detailed description
            description = data.get("description", "No description available.")
            details = data.get("details", "") # Empty if not present
            website = data.get("website", "")
            contact = data.get("contact", "")

            response = f"Regarding {key}:\n" # Start response clearly
            response += f"{description}"
            if details: # Only add if details exist
                response += f"\nMore Info: {details}"
            if website:
                response += f"\nWebsite: {website}"
            if contact:
                response += f"\nContact: {contact}"

            matched = True
            break # Stop after first match

    # Simple fallback greetings or help if no specific topic matched
    if not matched:
        if "hello" in message_lower or "hi" in message_lower:
            response = f"Hello {player_name}! How can I help? Ask me about the businesses or items here."
        elif "help" in message_lower or "what can you do" in message_lower:
            business_names = ", ".join(knowledge_base.keys())
            if not business_names: business_names = "any specific topics yet"
            response = f"I can tell you about: {business_names}. I also notify owners when certain items are interacted with."
        elif "thank" in message_lower:
            response = f"You're welcome, {player_name}!"
        # Keep the default "didn't understand" response if none of the above hit

    return response.strip()


# --- OSC Message Handlers ---
def handle_interaction(unused_addr, itemId, itemName, playerName, playerId):
    """Handles '/vrchat/interaction' messages."""
    print(f"Received interaction: Item='{itemName}' (ID: {itemId}), Player='{playerName}' (ID: {playerId})")

    # Prepare notification message
    timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    notification_message = (
        f":bell: **Item Interaction Alert** ({timestamp} UTC)\n"
        f"> **Player:** `{playerName}` (ID: `{playerId}`)\n"
        f"> **Item:** `{itemName}` (ID: `{itemId}`)"
    )

    # Send notification to Discord
    send_discord_notification(notification_message)

    # Optional: Send confirmation back to VRChat (e.g., to trigger a sound)
    # send_osc_to_vrchat("/vrchat/feedback", ["interaction_received", itemId])


def handle_chat_message(unused_addr, playerName, playerId, message):
    """Handles '/vrchat/chat' messages."""
    # Basic input sanitation/validation
    if not isinstance(message, str) or not message.strip():
        print(f"Received empty or invalid chat message type from {playerName} ({playerId}). Ignoring.")
        return
    message = message.strip()

    print(f"Received chat from '{playerName}' (ID: {playerId}): {message}")

    # Get response from AI core
    ai_response = get_ai_response(playerName, message)
    print(f"Generated AI response for {playerName}: {ai_response}")

    # Send the response back to the VRChat client via OSC
    # Using the common OSC Chatbox method via avatar parameters.
    # Requires VRChat OSC to be enabled and an avatar with a parameter like 'Chatbox'
    # configured to receive OSC input and display it.
    osc_chat_address = "/avatar/parameters/Chatbox" # Common parameter name
    max_len = 144 # Typical VRChat chatbox limit

    if len(ai_response) > max_len:
       ai_response_truncated = ai_response[:max_len-3] + "..."
       print(f"Warning: AI response truncated to {max_len} chars for VRChat Chatbox.")
    else:
       ai_response_truncated = ai_response

    # VRChat OSC Chatbox typically needs a boolean true trigger first, then the string.
    # Send True to activate/show the message, then the message itself.
    send_osc_to_vrchat(osc_chat_address, [True, ai_response_truncated])
    # A short delay might sometimes help ensure VRChat processes the bool before the string,
    # but often it's not needed. If issues occur, consider adding a small time.sleep(0.02) here.

# --- Flask Web Server (Optional but useful for status/webhooks/config) ---
flask_app = Flask(__name__)

@flask_app.route('/')
def index():
    """Basic status page for browser access."""
    return "VRChat AI Backend Service is running. See /status for details."

@flask_app.route('/status')
def status():
    """Provide basic status info as JSON."""
    return jsonify({
        "status": "running",
        "osc_listening_on": f"{CONFIG.get('osc_server_ip')}:{CONFIG.get('osc_server_port')}",
        "sending_osc_to": f"{CONFIG.get('vrchat_client_ip')}:{CONFIG.get('vrchat_client_port')}",
        "discord_notifications_enabled": bool(CONFIG.get("discord_webhook_url")),
        "knowledge_base_file": CONFIG.get("knowledge_base_file"),
        "knowledge_base_topics_loaded": list(knowledge_base.keys())
    })

# Add more endpoints here for web integration, maybe reloading config/kb?

# --- Main Execution ---
if __name__ == "__main__":
    print("-" * 30)
    print(" Starting VRChat AI Backend Service ")
    print("-" * 30)

    # Load configuration from config.json
    load_config()

    # Initialize OSC client for sending messages to VRChat
    initialize_osc_client()

    # Load the knowledge base data
    load_knowledge_base()

    # --- Setup OSC Server ---
    osc_dispatcher = dispatcher.Dispatcher()
    osc_dispatcher.map("/vrchat/interaction", handle_interaction)
    osc_dispatcher.map("/vrchat/chat", handle_chat_message)
    # Add more mappings here if needed, e.g., osc_dispatcher.map("/vrchat/some_other_event", handle_other_event)
    # Catch-all handler (optional, can be noisy)
    # osc_dispatcher.set_default_handler(lambda addr, *args: print(f"Received unhandled OSC: {addr} {args}"))

    # Start the OSC server in a separate thread
    osc_server_ip = CONFIG.get("osc_server_ip", "0.0.0.0")
    osc_server_port = CONFIG.get("osc_server_port", 9001)

    try:
        osc_server_instance = osc_server.ThreadingOSCUDPServer(
            (osc_server_ip, osc_server_port), osc_dispatcher)
        print(f"OSC Server listening on {osc_server_instance.server_address}")
    except OSError as e:
        print(f"\n!!! Error starting OSC Server on {osc_server_ip}:{osc_server_port} !!!")
        print(f"  > {e}")
        print(f"  > Is another application already using port {osc_server_port}?")
        print(f"  > Check your config.json if the IP/port needs changing.")
        sys.exit(1) # Exit if OSC server cannot start
    except Exception as e:
         print(f"\n!!! Unexpected error starting OSC Server: {e} !!!")
         sys.exit(1)


    osc_thread = threading.Thread(target=osc_server_instance.serve_forever)
    osc_thread.daemon = True # Allows main thread to exit even if this thread is running
    osc_thread.start()
    print("OSC server thread started.")

    # --- Start Flask Web Server (Optional) ---
    # Useful for checking status via a web browser on http://localhost:5000 (or the configured port)
    # You can disable this by commenting out the flask_app.run line if not needed.
    print("Starting Flask web server (for status checks)...")
    # Use try-except block for Flask server start as well
    try:
        # Note: Use host='0.0.0.0' to make Flask accessible from other devices on your network,
        # or host='127.0.0.1' to only allow access from the same machine.
        # The 'debug=False' is recommended for stability.
        flask_thread = threading.Thread(target=lambda: flask_app.run(host='127.0.0.1', port=5000, debug=False, use_reloader=False))
        flask_thread.daemon = True
        flask_thread.start()
        print("Flask server thread started on http://127.0.0.1:5000")
    except Exception as e:
        print(f"Could not start Flask server: {e}")


    # Keep the main thread alive until interrupted
    try:
        print("\nBackend service running. Press Ctrl+C here to exit.")
        while True:
            # Keep main thread alive, OSC and Flask servers run in background threads
            threading.Event().wait(timeout=1.0) # Wait with a timeout to allow checking loop condition
    except KeyboardInterrupt:
        print("\nShutting down...")
        osc_server_instance.shutdown()
        osc_server_instance.server_close()
        print("OSC server stopped.")
        # Flask thread is daemon, should exit automatically. Explicit shutdown if needed.
    except Exception as e:
        print(f"An unexpected error occurred in the main loop: {e}")
    finally:
        print("Backend service stopped.")
        sys.exit(0)

