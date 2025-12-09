// Authentication and session management
const AUTH_STORAGE_KEY = 'interview_user_session';
const SESSION_DURATION = 7 * 24 * 60 * 60 * 1000; // 7 days in milliseconds

// Get current user session
function getCurrentUser() {
  try {
    const sessionData = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!sessionData) return null;

    const session = JSON.parse(sessionData);
    
    // Check if session is expired
    if (session.expiresAt && new Date(session.expiresAt) < new Date()) {
      clearSession();
      return null;
    }

    return session.user;
  } catch (error) {
    console.error('Error reading session:', error);
    clearSession();
    return null;
  }
}

// Set user session
function setUserSession(user) {
  try {
    const expiresAt = new Date(Date.now() + SESSION_DURATION);
    const session = {
      user: {
        email: user.email,
        username: user.username,
        firstName: user.firstName || null,
        lastName: user.lastName || null,
        clientId: user.clientId
      },
      expiresAt: expiresAt.toISOString(),
      createdAt: new Date().toISOString()
    };
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session));
    console.log('Session saved to localStorage:', session);
    
    // Verify it was saved
    const saved = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!saved) {
      console.error('Failed to verify session was saved');
      return false;
    }
    
    return true;
  } catch (error) {
    console.error('Error setting session:', error);
    return false;
  }
}

// Clear user session (logout)
function clearSession() {
  localStorage.removeItem(AUTH_STORAGE_KEY);
}

// Check if user is authenticated
function isAuthenticated() {
  return getCurrentUser() !== null;
}

// Require authentication - redirect to login if not authenticated
async function requireAuth() {
  // First check server session
  try {
    const response = await fetch('/api/auth/me', {
      credentials: 'include'
    });
    
    if (response.ok) {
      const user = await response.json();
      // Update localStorage with server data
      setUserSession({
        email: user.email,
        username: user.username,
        firstName: user.firstName,
        lastName: user.lastName,
        clientId: user.id
      });
      return true;
    }
  } catch (error) {
    console.error('Auth check failed:', error);
  }
  
  // Fallback to localStorage check
  if (!isAuthenticated()) {
    window.location.href = 'login.html';
    return false;
  }
  return true;
}

// Get user's full name
function getUserFullName() {
  const user = getCurrentUser();
  if (!user) return '';
  
  if (user.firstName || user.lastName) {
    return `${user.firstName || ''} ${user.lastName || ''}`.trim();
  }
  return user.username || user.email.split('@')[0];
}

