const API_BASE = '/api';

let conversationId = null;
let clientId = null;
let messageCount = 0;

// DOM elements
const welcomeScreen = document.getElementById('welcomeScreen');
const chatScreen = document.getElementById('chatScreen');
const completionScreen = document.getElementById('completionScreen');
const startForm = document.getElementById('startForm');
const messageForm = document.getElementById('messageForm');
const chatMessages = document.getElementById('chatMessages');
const messageInput = document.getElementById('messageInput');
const sendButton = document.getElementById('sendButton');
const messageCountDisplay = document.getElementById('messageCount');
const chatHeader = document.getElementById('chatHeader');

// Start conversation
startForm.addEventListener('submit', async (e) => {
  e.preventDefault();

  const email = document.getElementById('email').value;
  const firstName = document.getElementById('firstName').value;
  const lastName = document.getElementById('lastName').value;

  try {
    const response = await fetch(`${API_BASE}/conversations/start`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, firstName, lastName }),
    });

    if (!response.ok) throw new Error('Failed to start conversation');

    const data = await response.json();
    conversationId = data.conversationId;
    clientId = data.clientId;

    // Update UI
    const displayName = firstName || email;
    chatHeader.textContent = `Conversation with ${displayName}`;

    // Show chat screen
    welcomeScreen.classList.add('d-none');
    chatScreen.classList.remove('d-none');

    // Display initial message
    addMessage('assistant', data.initialMessage);
    messageCount = 1;
    updateMessageCount();
  } catch (error) {
    console.error(error);
    alert('Failed to start conversation. Please try again.');
  }
});

// Send message
messageForm.addEventListener('submit', async (e) => {
  e.preventDefault();

  const message = messageInput.value.trim();
  if (!message) return;

  // Add user message to UI
  addMessage('user', message);
  messageInput.value = '';
  messageCount++;
  updateMessageCount();

  // Show typing indicator
  const typingDiv = showTypingIndicator();

  try {
    const response = await fetch(`${API_BASE}/conversations/message`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        conversationId,
        message,
      }),
    });

    if (!response.ok) throw new Error('Failed to send message');

    const data = await response.json();

    // Remove typing indicator
    typingDiv.remove();

    // Add assistant response
    addMessage('assistant', data.assistantMessage);
    messageCount = data.totalMessages;
    updateMessageCount();

    // Check if conversation ended
    if (data.conversationEnded) {
      setTimeout(() => {
        chatScreen.classList.add('d-none');
        completionScreen.classList.remove('d-none');
      }, 2000);
    }
  } catch (error) {
    console.error(error);
    typingDiv.remove();
    alert('Failed to send message. Please try again.');
  }
});

function addMessage(role, content) {
  const messageDiv = document.createElement('div');
  messageDiv.className = `message message-${role}`;

  const bubbleDiv = document.createElement('div');
  bubbleDiv.className = 'message-bubble';
  bubbleDiv.textContent = content;

  const timeDiv = document.createElement('div');
  timeDiv.className = 'message-time';
  timeDiv.textContent = new Date().toLocaleTimeString();

  messageDiv.appendChild(bubbleDiv);
  messageDiv.appendChild(timeDiv);
  chatMessages.appendChild(messageDiv);

  // Scroll to bottom
  chatMessages.scrollTop = chatMessages.scrollHeight;
}

function showTypingIndicator() {
  const typingDiv = document.createElement('div');
  typingDiv.className = 'message message-assistant';
  typingDiv.id = 'typing-indicator';

  const indicatorDiv = document.createElement('div');
  indicatorDiv.className = 'typing-indicator';
  indicatorDiv.innerHTML = '<span></span><span></span><span></span>';

  typingDiv.appendChild(indicatorDiv);
  chatMessages.appendChild(typingDiv);

  // Scroll to bottom
  chatMessages.scrollTop = chatMessages.scrollHeight;

  return typingDiv;
}

function updateMessageCount() {
  messageCountDisplay.textContent = `${messageCount} message${
    messageCount !== 1 ? 's' : ''
  }`;
}

