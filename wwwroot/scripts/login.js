const API_BASE = '/api';

let isRegisterMode = false;

document.addEventListener('DOMContentLoaded', () => {
  // Check if already logged in
  checkAuthStatus();

  const loginForm = document.getElementById('loginForm');
  const errorAlert = document.getElementById('errorAlert');
  const successAlert = document.getElementById('successAlert');
  const switchLink = document.getElementById('switchLink');
  const loginTitle = document.getElementById('loginTitle');
  const loginSubtitle = document.getElementById('loginSubtitle');
  const usernameField = document.getElementById('usernameField');
  const firstNameField = document.getElementById('firstNameField');
  const lastNameField = document.getElementById('lastNameField');
  const submitText = document.getElementById('submitText');
  const switchText = document.getElementById('switchText');

  // Toggle between login and register
  switchLink.addEventListener('click', (e) => {
    e.preventDefault();
    toggleMode();
  });

  function toggleMode() {
    isRegisterMode = !isRegisterMode;
    const usernameInput = document.getElementById('username');
    const passwordInput = document.getElementById('password');
    
    if (isRegisterMode) {
      // Register mode
      loginTitle.textContent = 'Create Account';
      loginSubtitle.textContent = 'Sign up to start your interview';
      switchText.textContent = 'Already have an account?';
      switchLink.textContent = 'Sign In';
      submitText.textContent = 'Create Account';
      usernameField.style.display = 'block';
      firstNameField.style.display = 'block';
      lastNameField.style.display = 'block';
      usernameInput.required = true;
      passwordInput.autocomplete = 'new-password';
    } else {
      // Login mode
      loginTitle.textContent = 'Welcome Back';
      loginSubtitle.textContent = 'Sign in to access your interview dashboard';
      switchText.textContent = "Don't have an account?";
      switchLink.textContent = 'Create Account';
      submitText.textContent = 'Sign In';
      usernameField.style.display = 'block';
      firstNameField.style.display = 'none';
      lastNameField.style.display = 'none';
      usernameInput.required = false;
      passwordInput.autocomplete = 'current-password';
    }
    
    // Clear form
    loginForm.reset();
    hideAlerts();
  }

  loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideAlerts();

    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;
    const username = document.getElementById('username').value.trim();
    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();

    if (!email || !password) {
      showError('Email and password are required');
      return;
    }

    if (isRegisterMode && !username) {
      showError('Username is required for registration');
      return;
    }

    const submitBtn = document.getElementById('submitBtn');
    submitBtn.disabled = true;
    const originalText = submitText.textContent;
    submitText.textContent = isRegisterMode ? 'Creating Account...' : 'Signing in...';

    try {
      let response;
      if (isRegisterMode) {
        // Register
        response = await fetch(`${API_BASE}/auth/register`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({
            email,
            username,
            password,
            firstName: firstName || null,
            lastName: lastName || null
          })
        });
      } else {
        // Login
        response = await fetch(`${API_BASE}/auth/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({
            email,
            password
          })
        });
      }

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Authentication failed' }));
        throw new Error(errorData.message || 'Authentication failed');
      }

      const user = await response.json();
      
      // Store user in localStorage for frontend use
      if (typeof setUserSession !== 'undefined') {
        setUserSession({
          email: user.email,
          username: user.username,
          firstName: user.firstName,
          lastName: user.lastName,
          clientId: user.id
        });
      }

      // Show success message briefly
      if (isRegisterMode) {
        showSuccess('Account created successfully! Redirecting...');
        await new Promise(resolve => setTimeout(resolve, 1000));
      }

      // Redirect to dashboard
      window.location.href = 'dashboard.html';
    } catch (error) {
      console.error('Auth error:', error);
      showError(error.message || 'Authentication failed. Please try again.');
      submitBtn.disabled = false;
      submitText.textContent = originalText;
    }
  });

  async function checkAuthStatus() {
    try {
      const response = await fetch(`${API_BASE}/auth/me`, {
        credentials: 'include'
      });
      
      if (response.ok) {
        const user = await response.json();
        // Already logged in, redirect
        if (typeof setUserSession !== 'undefined') {
          setUserSession({
            email: user.email,
            username: user.username,
            firstName: user.firstName,
            lastName: user.lastName,
            clientId: user.id
          });
        }
        window.location.href = 'dashboard.html';
      }
    } catch (error) {
      // Not authenticated, stay on login page
      console.log('Not authenticated');
    }
  }

  function showError(message) {
    if (errorAlert) {
      errorAlert.textContent = message;
      errorAlert.classList.remove('d-none');
      setTimeout(() => {
        errorAlert.classList.add('d-none');
      }, 5000);
    }
  }

  function showSuccess(message) {
    if (successAlert) {
      successAlert.textContent = message;
      successAlert.classList.remove('d-none');
    }
  }

  function hideAlerts() {
    if (errorAlert) errorAlert.classList.add('d-none');
    if (successAlert) successAlert.classList.add('d-none');
  }
});
