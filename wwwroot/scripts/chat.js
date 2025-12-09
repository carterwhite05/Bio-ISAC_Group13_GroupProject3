const API_BASE = '/api';

let conversationId = null;
let clientId = null;
let messageCount = 0;
let currentQuestionId = null;
let waitingForAdditionalInfo = false;
let totalQuestions = 0;
let answeredQuestions = 0;

// Load auth script if not already loaded
if (typeof getCurrentUser === 'undefined') {
  const script = document.createElement('script');
  script.src = './scripts/auth.js';
  document.head.appendChild(script);
}

// DOM elements - wait for DOM to be ready
let welcomeScreen, chatScreen, completionScreen, startForm, messageForm;
let chatMessages, messageInput, sendButton, messageCountDisplay, chatHeader, progressBar;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', async () => {
  if (typeof requireAuth === 'undefined') {
    console.error('auth.js not loaded');
    // Try to load it
    const script = document.createElement('script');
    script.src = './scripts/auth.js';
    document.head.appendChild(script);
    script.onload = async () => {
      const authResult = await requireAuth();
      if (!authResult) {
        return; // Redirected to login
      }
      initializeInterview();
    };
    return;
  }

  const authResult = await requireAuth();
  if (!authResult) {
    return; // Redirected to login page
  }

  initializeInterview();
});

async function initializeInterview() {
  console.log('initializeInterview called');
  
  chatScreen = document.getElementById('chatScreen');
  completionScreen = document.getElementById('completionScreen');
  messageForm = document.getElementById('messageForm');
  chatMessages = document.getElementById('chatMessages');
  messageInput = document.getElementById('messageInput');
  sendButton = document.getElementById('sendButton');
  messageCountDisplay = document.getElementById('messageCount');
  chatHeader = document.getElementById('chatHeader');
  progressBar = document.getElementById('progressBar');

  console.log('DOM elements:', {
    chatScreen: !!chatScreen,
    chatMessages: !!chatMessages,
    messageInput: !!messageInput,
    messageForm: !!messageForm
  });

  // Get logged-in user (required at this point)
  const user = getCurrentUser();
  if (!user) {
    console.error('User not found, redirecting to login');
    window.location.href = 'login.html';
    return;
  }
  
  console.log('User found:', user);

  // Check if user has an active conversation
  try {
    const convResponse = await fetch(`${API_BASE}/conversations/current`, {
      credentials: 'include'
    });

    if (convResponse.ok) {
      const conversation = await convResponse.json();
      
      // Load existing conversation
      conversationId = conversation.conversationId;
      clientId = conversation.clientId;
      
      // Load messages
      const messagesResponse = await fetch(`${API_BASE}/conversations/${conversationId}/messages`, {
        credentials: 'include'
      });
      
      if (messagesResponse.ok) {
        const messages = await messagesResponse.json();
        console.log('Loaded messages:', messages);
        
        // Display all messages
        if (chatMessages) {
          chatMessages.innerHTML = '';
          if (messages.length > 0) {
            messages.forEach(msg => {
              addMessage(msg.role, msg.content);
              messageCount++;
            });
            updateMessageCount();
          } else {
            // No messages found - conversation exists but is empty
            console.warn('Conversation exists but has no messages');
            addMessage('assistant', 'Welcome! It looks like your conversation was interrupted. Please refresh the page or contact support.');
          }
        }
        
        // Get total questions for progress
        await fetchTotalQuestions();
        updateProgress();
        
        // Show chat screen
        if (chatScreen) {
          showScreen(chatScreen);
        }
        
        // Update header
        const displayName = user.firstName || user.email.split('@')[0];
        if (chatHeader) {
          chatHeader.textContent = `Interview with ${displayName}`;
        }
        
        // Check if conversation is completed
        if (conversation.status === 'Completed') {
          if (completionScreen) {
            showScreen(completionScreen);
          }
        } else {
          // Focus input
          if (messageInput) {
            setTimeout(() => messageInput.focus(), 100);
          }
        }
      } else {
        console.error('Failed to load messages:', messagesResponse.status);
        // If we can't load messages, try starting fresh
        await startNewInterview(user);
      }
    } else {
      // No conversation exists, start new one
      await startNewInterview(user);
    }
  } catch (error) {
    console.error('Error checking conversation:', error);
    // Try to start new interview
    await startNewInterview(user);
  }

  // Initialize event listeners
  if (messageForm) {
    messageForm.addEventListener('submit', handleMessageForm);
  }
  if (messageInput) {
    messageInput.addEventListener('keydown', handleKeyDown);
  }
}

async function startNewInterview(user) {
  console.log('Starting new interview for user:', user);
  
  // Check if skipUploads is in URL
  const urlParams = new URLSearchParams(window.location.search);
  const skipUploads = urlParams.get('skipUploads') === 'true';
  
  try {
    const url = skipUploads 
      ? `${API_BASE}/conversations/start?skipUploads=true`
      : `${API_BASE}/conversations/start`;
    
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ 
        email: user.email, 
        firstName: user.firstName, 
        lastName: user.lastName 
      }),
    });

    console.log('Start conversation response status:', response.status);

    if (!response.ok) {
      let errorMessage = 'Failed to start conversation';
      try {
        const error = await response.json();
        errorMessage = error.detail || error.message || errorMessage;
        console.error('Start conversation error:', error);
        
        // Allow multiple interviews - no longer blocking on active interview
        // Removed the check for "already have an active interview" to allow multiple attempts
      } catch (parseError) {
        console.error('Failed to parse error response:', parseError);
        errorMessage = `Server returned ${response.status}: ${response.statusText}`;
      }
      throw new Error(errorMessage);
    }

    const data = await response.json();
    console.log('Start conversation response data:', data);
    console.log('Response keys:', Object.keys(data));
    
    // Handle both camelCase and PascalCase property names
    conversationId = data.conversationId || data.ConversationId;
    clientId = data.clientId || data.ClientId;
    currentQuestionId = data.currentQuestionId || data.CurrentQuestionId || null;
    
    if (!conversationId) {
      console.error('Invalid response data:', data);
      throw new Error('Invalid response from server - missing conversationId');
    }
    
    console.log('Conversation started:', { conversationId, clientId, currentQuestionId });

    // Get total questions for progress
    await fetchTotalQuestions();

    // Update UI
    const displayName = user.firstName || user.email.split('@')[0];
    if (chatHeader) {
      chatHeader.textContent = `Interview with ${displayName}`;
    }

    // Show chat screen
    if (chatScreen) {
      showScreen(chatScreen);
    } else {
      console.error('chatScreen element not found!');
    }

    // Display initial message
    if (chatMessages) {
      // Check for both camelCase and PascalCase property names
      const initialMessage = data.initialMessage || data.InitialMessage;
      
      if (initialMessage) {
        console.log('Displaying initial message:', initialMessage);
        addMessage('assistant', initialMessage);
        messageCount = 1;
        updateMessageCount();
        updateProgress();
      } else {
        console.error('No initial message in response:', data);
        console.error('Response keys:', Object.keys(data));
        
        // Fallback: try to load messages from server
        addMessage('assistant', 'Hello! Let\'s begin the interview. Loading your conversation...');
        
        // Try to reload messages from server after a short delay
        setTimeout(async () => {
          try {
            console.log('Attempting to load messages for conversation:', conversationId);
            const messagesResponse = await fetch(`${API_BASE}/conversations/${conversationId}/messages`, {
              credentials: 'include'
            });
            if (messagesResponse.ok) {
              const messages = await messagesResponse.json();
              console.log('Loaded messages from server:', messages);
              if (messages.length > 0) {
                chatMessages.innerHTML = '';
                messages.forEach(msg => {
                  addMessage(msg.role, msg.content);
                  messageCount++;
                });
                updateMessageCount();
                updateProgress();
              } else {
                console.warn('No messages found in database for conversation');
                addMessage('assistant', 'I\'m ready to start! Please answer the questions as they appear.');
              }
            } else {
              console.error('Failed to load messages:', messagesResponse.status);
            }
          } catch (err) {
            console.error('Error loading messages:', err);
          }
        }, 1000);
      }
    } else {
      console.error('chatMessages element not found!');
    }
    
    // Focus input
    if (messageInput) {
      setTimeout(() => messageInput.focus(), 100);
    } else {
      console.error('messageInput element not found!');
    }
  } catch (error) {
    console.error('Error starting interview:', error);
    console.error('Error stack:', error.stack);
    
    // Check if error is about missing documents
    if (error.message && error.message.includes('documents')) {
      alert('Please upload all required documents (ID and Passport) before starting the interview.\n\nYou can upload them from your dashboard.');
      window.location.href = 'dashboard.html';
    } else {
      alert('Failed to start interview: ' + error.message + '\n\nCheck the browser console for more details.');
    }
  }
}

// Screen management
function showScreen(screen) {
  if (!screen) {
    console.error('showScreen called with null/undefined screen');
    return;
  }
  [chatScreen, completionScreen].forEach((s) => {
    if (s) s.classList.remove('active');
  });
  screen.classList.add('active');
  console.log('Screen shown:', screen.id);
}

// Start conversation handler
async function handleStartForm(e) {
  e.preventDefault();

  // User must be logged in to start interview
  const user = getCurrentUser();
  if (!user) {
    alert('You must be logged in to start an interview. Redirecting to login page...');
    window.location.href = 'login.html';
    return;
  }

  // Use logged-in user's information (required)
  const email = user.email;
  const firstName = user.firstName;
  const lastName = user.lastName;
  const username = user.username;

  if (!email) {
    alert('Email is required. Please log in again.');
    window.location.href = 'login.html';
    return;
  }

  // Disable form
  const submitBtn = startForm.querySelector('button[type="submit"]');
  submitBtn.disabled = true;
  submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Starting...';

  try {
    const response = await fetch(`${API_BASE}/conversations/start`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ email, firstName, lastName }),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to start conversation');
    }

    const data = await response.json();
    conversationId = data.conversationId;
    clientId = data.clientId;
    currentQuestionId = data.currentQuestionId || null;

    // Get total questions for progress
    await fetchTotalQuestions();

    // Update UI
    const displayName = firstName || email.split('@')[0];
    chatHeader.textContent = `Interview with ${displayName}`;

    // Show chat screen
    if (!chatScreen) {
      console.error('chatScreen element not found');
      throw new Error('Chat screen element not found');
    }
    showScreen(chatScreen);

    // Display initial message
    if (!chatMessages) {
      console.error('chatMessages element not found');
      throw new Error('Chat messages container not found');
    }
    addMessage('assistant', data.initialMessage);
    messageCount = 1;
    updateMessageCount();
    updateProgress();
    
    // Focus input
    if (messageInput) {
      setTimeout(() => messageInput.focus(), 100);
    }
  } catch (error) {
    console.error(error);
    alert(error.message || 'Failed to start conversation. Please try again.');
    submitBtn.disabled = false;
    submitBtn.innerHTML = '<i class="bi bi-arrow-right-circle me-2"></i>Begin Interview';
  }
}

// Fetch total questions for progress tracking
async function fetchTotalQuestions() {
  try {
    const response = await fetch(`${API_BASE}/admin/questions`, {
      credentials: 'include'
    });
    if (response.ok) {
      const questions = await response.json();
      totalQuestions = questions.filter(q => q.isActive).length;
    }
  } catch (error) {
    console.error('Failed to fetch questions:', error);
  }
}

// Send message handler
async function handleMessageForm(e) {
  e.preventDefault();

  const message = messageInput.value.trim();
  if (!message) return;

  // Add user message to UI
  addMessage('user', message);
  messageInput.value = '';
  messageCount++;
  updateMessageCount();

  // Disable input while processing
  messageInput.disabled = true;
  sendButton.disabled = true;
  sendButton.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

  try {
    const response = await fetch(`${API_BASE}/conversations/message`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        conversationId,
        message,
      }),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to send message');
    }

    const data = await response.json();

    // Update state
    currentQuestionId = data.currentQuestionId || null;
    waitingForAdditionalInfo = data.waitingForAdditionalInfo || false;

    // If we moved to a new question or completed, update progress
    if (!waitingForAdditionalInfo && !data.conversationEnded) {
      answeredQuestions++;
    }

    // Add assistant response
    addMessage('assistant', data.assistantMessage);
    messageCount = data.totalMessages;
    updateMessageCount();
    updateProgress();

    // Check if conversation ended
    if (data.conversationEnded) {
      // Redirect to dashboard after a brief delay
      // If user is logged in, they'll be automatically authenticated
      setTimeout(() => {
        window.location.href = 'dashboard.html';
      }, 2000);
    } else {
      // Re-enable input
      messageInput.disabled = false;
      sendButton.disabled = false;
      sendButton.innerHTML = '<i class="bi bi-send-fill"></i>';
      setTimeout(() => messageInput.focus(), 100);
    }
  } catch (error) {
    console.error(error);
    alert(error.message || 'Failed to send message. Please try again.');
    // Re-enable input on error
    messageInput.disabled = false;
    sendButton.disabled = false;
    sendButton.innerHTML = '<i class="bi bi-send-fill"></i>';
    setTimeout(() => messageInput.focus(), 100);
  }
}

// Add Enter key support
function handleKeyDown(e) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault();
    if (messageForm) {
      messageForm.dispatchEvent(new Event('submit'));
    }
  }
}


function updateMessageCount() {
  if (messageCountDisplay) {
    messageCountDisplay.textContent = `${messageCount} message${
      messageCount !== 1 ? 's' : ''
    }`;
  }
}

function updateProgress() {
  if (progressBar && totalQuestions > 0) {
    const progress = Math.min((answeredQuestions / totalQuestions) * 100, 100);
    progressBar.style.width = `${progress}%`;
    progressBar.setAttribute('aria-valuenow', progress);
  }
}

function addMessage(role, content) {
  if (!chatMessages) return;
  
  const messageDiv = document.createElement('div');
  messageDiv.className = `message message-${role}`;

  const bubbleDiv = document.createElement('div');
  bubbleDiv.className = 'message-bubble';
  
  // Preserve line breaks and format
  const formattedContent = content
    .replace(/\n/g, '<br>')
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.+?)\*/g, '<em>$1</em>');
  bubbleDiv.innerHTML = formattedContent;

  const timeDiv = document.createElement('div');
  timeDiv.className = 'message-time';
  timeDiv.textContent = new Date().toLocaleTimeString([], { 
    hour: '2-digit', 
    minute: '2-digit' 
  });

  messageDiv.appendChild(bubbleDiv);
  messageDiv.appendChild(timeDiv);
  chatMessages.appendChild(messageDiv);

  // Scroll to bottom with smooth animation
  setTimeout(() => {
    chatMessages.scrollTo({
      top: chatMessages.scrollHeight,
      behavior: 'smooth'
    });
  }, 100);
}
