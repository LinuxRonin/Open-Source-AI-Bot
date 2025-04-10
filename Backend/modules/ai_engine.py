# AI Engine module for VRChat backend
# Handles the AI response generation using different modes

import logging
import re
import random
import time

logger = logging.getLogger(__name__)

class AIEngine:
    """AI Engine for processing and responding to messages"""
    
    def __init__(self, mode="keyword", knowledge_base=None, api_key=None):
        """
        Initialize the AI Engine
        
        Args:
            mode (str): Processing mode - 'keyword', 'nlp', or 'llm'
            knowledge_base (KnowledgeBase): Reference to the knowledge base
            api_key (str): API key for external LLM services if used
        """
        self.mode = mode
        self.knowledge_base = knowledge_base
        self.api_key = api_key
        
        # Initialize conversation context
        self.conversation_history = {}  # Format: {player_id: [{'role': 'user', 'content': '...'}]}
        
        # If using NLP mode, try to initialize libraries
        if self.mode == "nlp":
            try:
                import spacy
                self.nlp = spacy.load("en_core_web_sm")
                logger.info("NLP mode initialized with spaCy")
            except ImportError:
                logger.warning("spaCy not installed. Falling back to keyword mode.")
                self.mode = "keyword"
                self.nlp = None
            except OSError:
                logger.warning("spaCy language model not found. Falling back to keyword mode
