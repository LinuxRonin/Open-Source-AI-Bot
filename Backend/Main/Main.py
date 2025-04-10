#!/usr/bin/env python3
# VRChat AI Backend Service
# Main entry point for the backend service

import os
import sys
import logging
import threading
from flask import Flask
from argparse import ArgumentParser

# Import service modules
from modules.osc_handler import OSCHandler
from modules.knowledge_base import KnowledgeBase
from modules.ai_engine import AIEngine
from modules.discord_integration import DiscordIntegration
from modules.web_api import setup_flask_routes

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler('backend.log')
    ]
)
logger = logging.getLogger("main")

# Global references to major components
knowledge_base = None
ai_engine = None
discord_integration = None
osc_handler = None
flask_app = Flask(__name__)

def load_config(config_path='config.json'):
    """Load configuration from file or use defaults"""
    import json
    
    # Default configuration
    default_config = {
        "osc_server_ip": "0.0.0.0",
        "osc_server_port": 9001,
        "vrchat_client_ip": "127.0.0.1",
        "vrchat_client_port": 9000,
        "discord_webhook_url": "",
        "knowledge_base_file": "knowledge_base.json",
        "owner_discord_ids": [],
        "web_server_port": 5000,
        "web_server_host": "127.0.0.1",
        "log_level": "INFO",
        "ai_mode": "keyword",  # keyword, nlp, or llm
        "api_key": ""  # For LLM API if used
    }
    
    # Create config file with defaults if it doesn't exist
    if not os.path.exists(config_path):
        logger.warning(f"Config file '{config_path}' not found. Creating with defaults.")
        try:
            with open(config_path, 'w') as f:
                json.dump(default_config, f, indent=4)
            return default_config
        except Exception as e:
            logger.error(f"Failed to create config file: {e}")
            return default_config
    
    # Load existing config and merge with defaults
    try:
        with open(config_path, 'r') as f:
            user_config = json.load(f)
        
        # Merge with defaults (defaults are fallback)
        config = default_config.copy()
        config.update(user_config)
        
        logger.info(f"Configuration loaded from '{config_path}'")
        return config
    except Exception as e:
        logger.error(f"Error loading config: {e}")
        return default_config

def initialize_components(config):
    """Initialize all backend components"""
    global knowledge_base, ai_engine, discord_integration, osc_handler
    
    # Initialize knowledge base
    kb_file = config.get("knowledge_base_file")
    knowledge_base = KnowledgeBase(kb_file)
    knowledge_base.load()
    
    # Initialize AI engine based on configured mode
    ai_mode = config.get("ai_mode", "keyword")
    if ai_mode == "llm":
        api_key = config.get("api_key")
        if not api_key:
            logger.warning("LLM mode selected but no API key provided. Falling back to keyword mode.")
            ai_mode = "keyword"
    
    ai_engine = AIEngine(mode=ai_mode, knowledge_base=knowledge_base, api_key=config.get("api_key"))
    
    # Initialize Discord integration if webhook provided
    webhook_url = config.get("discord_webhook_url")
    owner_ids = config.get("owner_discord_ids", [])
    if webhook_url:
        discord_integration = DiscordIntegration(webhook_url, owner_ids)
        logger.info("Discord integration initialized")
    else:
        logger.warning("No Discord webhook URL provided. Notifications disabled.")
        discord_integration = None
    
    # Initialize OSC handler
    osc_handler = OSCHandler(
        server_ip=config.get("osc_server_ip"),
        server_port=config.get("osc_server_port"),
        client_ip=config.get("vrchat_client_ip"),
        client_port=config.get("vrchat_client_port"),
        ai_engine=ai_engine,
        discord=discord_integration
    )
    
    # Setup Flask routes
    setup_flask_routes(flask_app, knowledge_base, ai_engine, osc_handler)

def parse_arguments():
    """Parse command line arguments"""
    parser = ArgumentParser(description='VRChat AI Backend Service')
    parser.add_argument('--config', dest='config_path', default='config.json',
                        help='Path to configuration file (default: config.json)')
    parser.add_argument('--debug', action='store_true',
                        help='Enable debug mode with more verbose logging')
    return parser.parse_args()

def main():
    """Main entry point"""
    # Parse command line arguments
    args = parse_arguments()
    
    # Set logging level
    if args.debug:
        logging.getLogger().setLevel(logging.DEBUG)
        logger.info("Debug mode enabled")
    
    # Print welcome message
    logger.info("-" * 30)
    logger.info(" VRChat AI Backend Service ")
    logger.info("-" * 30)
    
    # Load configuration
    config = load_config(args.config_path)
    
    # Set log level from config (if debug wasn't explicitly enabled)
    if not args.debug:
        log_level = getattr(logging, config.get("log_level", "INFO").upper(), logging.INFO)
        logging.getLogger().setLevel(log_level)
    
    # Initialize all components
    try:
        initialize_components(config)
    except Exception as e:
        logger.error(f"Failed to initialize components: {e}")
        sys.exit(1)
    
    # Start OSC server
    try:
        osc_handler.start()
    except Exception as e:
        logger.error(f"Failed to start OSC server: {e}")
        sys.exit(1)
    
    # Start Flask web server
    try:
        host = config.get("web_server_host", "127.0.0.1")
        port = config.get("web_server_port", 5000)
        
        # Run Flask in a separate thread to avoid blocking
        flask_thread = threading.Thread(
            target=lambda: flask_app.run(host=host, port=port, debug=False, use_reloader=False)
        )
        flask_thread.daemon = True
        flask_thread.start()
        logger.info(f"Web server started on http://{host}:{port}")
    except Exception as e:
        logger.error(f"Failed to start web server: {e}")
        # Continue even if web server fails
    
    # Keep main thread alive with a message
    logger.info("\nBackend service running. Press Ctrl+C to exit.")
    
    try:
        # Keep main thread alive until interrupted
        while True:
            threading.Event().wait(1.0)
    except KeyboardInterrupt:
        logger.info("\nShutting down...")
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
    finally:
        # Clean shutdown
        if osc_handler:
            osc_handler.stop()
        logger.info("Backend service stopped.")

if __name__ == "__main__":
    main()
