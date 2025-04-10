# OSC Setup Guide for VRChat

This guide explains how to set up and configure OSC (Open Sound Control) for the AI Bot in VRChat.

## What is OSC?

OSC (Open Sound Control) is a protocol that allows applications to communicate with each other using network messages. VRChat supports sending and receiving OSC messages, which our AI Bot uses to receive chat messages and send responses.

## VRChat OSC Setup

### 1. Enable OSC in VRChat

1. Open VRChat
2. Open the Options menu
3. Navigate to the "OSC" tab
4. Ensure "Enable OSC" is checked
5. Take note of the port numbers:
   - VRChat receives on port 9000 by default
   - VRChat sends on port 9001 by default

### 2. Avatar Setup for Receiving Messages

For the AI Bot to send messages that appear in your VRChat world, you'll need an avatar that supports receiving OSC messages for its chatbox parameter.

#### Option A: Use a VRChat-Ready Avatar

Many avatars already support OSC chatbox functionality. To test if your avatar supports this:

1. Enable OSC in VRChat (step 1)
2. Run a simple OSC test tool like [OSC Pilot](https://oscpilot.com/) or [TouchOSC](https://hexler.net/touchosc)
3. Send a message to the address: `/avatar/parameters/Chatbox` with a string value
4. If your avatar displays the text, it's compatible

#### Option B: Configure Your Custom Avatar

If you're creating a custom avatar or modifying an existing one:

1. Open your avatar in Unity
2. Ensure your avatar has an Animator component
3. In the VRChat Avatar Descriptor, go to Parameters
4. Add a parameter named "Chatbox" of type String
5. In your avatar's Animator Controller, add logic to display this string value
6. Publish your updated avatar to VRChat

## Backend Connection

Our Python backend service is designed to:

1. Listen for messages from VRChat on port 9001
2. Send messages back to VRChat on port 9000

### Configuration Check

Ensure your `config.json` file has the correct ports:

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

## Testing the Connection

1. Start the Python backend service: `python main.py`
2. Launch VRChat with OSC enabled
3. In your world, approach the AI Bot's chat UI
4. Type a message and press Enter
5. You should see:
   - The message sent to the backend (visible in backend console)
   - A response sent back to VRChat (displayed in your avatar's chatbox)

## Troubleshooting

### Messages Not Being Sent/Received

- Ensure VRChat's OSC is enabled
- Check firewall settings to allow OSC traffic
- Verify the ports in `config.json` match VRChat's ports
- Try running both VRChat and the backend on the same computer initially

### Avatar Not Displaying Messages

- Confirm your avatar has a "Chatbox" parameter
- Test with a known OSC-compatible avatar
- Check if the parameter name is exactly "Chatbox" (case-sensitive)

### Backend Connection Issues

- Check console for error messages
- Ensure no other applications are using port 9001
- Try restarting both VRChat and the backend service

## Advanced: Parameter Visualization

For advanced users, you can use VRChat's built-in Parameter Debugging tool to visualize OSC parameters:

1. In VRChat, open the Action Menu
2. Navigate to Options > OSC
3. Enable "Parameter Debug View"
4. When messages are received, you'll see parameter changes in real-time
