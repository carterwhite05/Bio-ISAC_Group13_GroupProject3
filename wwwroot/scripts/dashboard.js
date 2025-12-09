const API_BASE = '/api';

// Logout function
async function logout() {
  if (confirm('Are you sure you want to logout?')) {
    try {
      await fetch(`${API_BASE}/auth/logout`, {
        method: 'POST',
        credentials: 'include'
      });
    } catch (error) {
      console.error('Logout error:', error);
    }
    clearSession();
    window.location.href = 'login.html';
  }
}

// Get status badge HTML
function getStatusBadge(status) {
  const badges = {
    Pending: '<span class="badge bg-warning status-badge">Pending</span>',
    Approved: '<span class="badge bg-success status-badge">Approved</span>',
    Rejected: '<span class="badge bg-danger status-badge">Rejected</span>',
    InProgress: '<span class="badge bg-info status-badge">In Progress</span>',
    InterviewCompleted: '<span class="badge bg-primary status-badge">Interview Completed</span>',
    UnderReview: '<span class="badge bg-warning status-badge">Under Review</span>',
  };
  return badges[status] || `<span class="badge bg-secondary status-badge">${status}</span>`;
}

// Get conversation status badge
function getConversationStatusBadge(status) {
  const badges = {
    Active: '<span class="badge bg-info status-badge">Active</span>',
    Completed: '<span class="badge bg-success status-badge">Completed</span>',
    Abandoned: '<span class="badge bg-secondary status-badge">Abandoned</span>',
  };
  return badges[status] || `<span class="badge bg-secondary status-badge">${status}</span>`;
}

// Get status message
function getStatusMessage(status) {
  const messages = {
    Pending: 'Your interview is pending review. We will update you once the review process begins.',
    Approved: 'Congratulations! Your application has been approved. You will be contacted shortly with next steps.',
    Rejected: 'We appreciate your interest. Unfortunately, your application was not approved at this time.',
    InProgress: 'Your interview is currently in progress. Please complete all questions to finish.',
    InterviewCompleted: 'Thank you for completing the interview! Your responses are being reviewed. We will update your status shortly.',
    UnderReview: 'Your interview is currently under review. Our team is carefully evaluating your responses. We will update you as soon as possible.',
  };
  return messages[status] || 'Your interview status is being processed.';
}

// Load dashboard data
async function loadDashboard() {
  // Check authentication first
  try {
    const authResponse = await fetch(`${API_BASE}/auth/me`, {
      credentials: 'include'
    });
    
    if (!authResponse.ok) {
      window.location.href = 'login.html';
      return;
    }

    const user = await authResponse.json();
    
    // Store in localStorage
    if (typeof setUserSession !== 'undefined') {
      setUserSession({
        email: user.email,
        username: user.username,
        firstName: user.firstName,
        lastName: user.lastName,
        clientId: user.id
      });
    }

    const loadingState = document.getElementById('loadingState');
    const dashboardContent = document.getElementById('dashboardContent');
    const errorState = document.getElementById('errorState');

    try {
      // Get client info
      const clientResponse = await fetch(`${API_BASE}/clients/by-email?email=${encodeURIComponent(user.email)}`, {
        credentials: 'include'
      });
      
      if (!clientResponse.ok) {
        loadingState.classList.add('d-none');
        errorState.classList.remove('d-none');
        return;
      }

      const client = await clientResponse.json();

      // Check documents status
      const documentsCheckResponse = await fetch(`${API_BASE}/documents/check`, {
        credentials: 'include'
      });
      
      let documentsStatus = { allUploaded: false, uploadedTypes: [], missingTypes: ['ID', 'Passport', 'ProofOfResidency', 'VerificationPhoto', 'VerificationVideo'] };
      if (documentsCheckResponse.ok) {
        documentsStatus = await documentsCheckResponse.json();
      }

      // Get uploaded documents
      const documentsResponse = await fetch(`${API_BASE}/documents`, {
        credentials: 'include'
      });
      let documents = [];
      if (documentsResponse.ok) {
        documents = await documentsResponse.json();
      }

      // Get current conversation
      const conversationResponse = await fetch(`${API_BASE}/conversations/current`, {
        credentials: 'include'
      });

      const hasInterview = conversationResponse.ok;
      let conversation = null;
      
      if (hasInterview) {
        conversation = await conversationResponse.json();
      }

      // Update documents section
      updateDocumentsSection(documentsStatus, documents);

      // Update UI
      const fullName = client.firstName || client.lastName
        ? `${client.firstName || ''} ${client.lastName || ''}`.trim()
        : user.username || 'User';

      document.getElementById('clientName').textContent = `${fullName}'s Dashboard`;
      document.getElementById('clientEmail').textContent = client.email;
      document.getElementById('emailDisplay').textContent = client.email;
      document.getElementById('username').textContent = user.username || client.email.split('@')[0];
      document.getElementById('fullName').textContent = fullName || 'Not provided';
      
      document.getElementById('statusBadge').innerHTML = getStatusBadge(client.status);
      document.getElementById('statusText').textContent = client.status.replace(/([A-Z])/g, ' $1').trim();
      document.getElementById('createdAt').textContent = new Date(client.createdAt).toLocaleDateString();
      document.getElementById('updatedAt').textContent = client.updatedAt 
        ? new Date(client.updatedAt).toLocaleDateString() 
        : new Date(client.createdAt).toLocaleDateString();

      // Update interview section
      const interviewSection = document.getElementById('interviewSection');
      if (hasInterview && conversation) {
        // Show interview stats
        interviewSection.innerHTML = `
          <div class="status-card">
            <h4 class="text-white mb-3">
              <i class="bi bi-chat-dots me-2"></i>Your Interview
            </h4>
            <div class="info-item">
              <span class="info-label">Status</span>
              <span class="info-value">${getConversationStatusBadge(conversation.status)}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Started</span>
              <span class="info-value">${new Date(conversation.startedAt).toLocaleString()}</span>
            </div>
            ${conversation.endedAt ? `
            <div class="info-item">
              <span class="info-label">Completed</span>
              <span class="info-value">${new Date(conversation.endedAt).toLocaleString()}</span>
            </div>
            ` : ''}
            <div class="info-item">
              <span class="info-label">Messages</span>
              <span class="info-value">${conversation.totalMessages}</span>
            </div>
            <div class="mt-3">
              ${conversation.status === 'Active' ? `
                <div class="d-flex gap-2 justify-content-center">
                  <a href="index.html" class="btn btn-primary">
                    <i class="bi bi-arrow-right-circle me-2"></i>Continue Interview
                  </a>
                  <a href="index.html?skipUploads=true" class="btn btn-outline-primary">
                    <i class="bi bi-plus-circle me-2"></i>Start New Interview
                  </a>
                </div>
              ` : `
                <div class="d-flex gap-2 justify-content-center flex-wrap">
                  <p class="text-secondary mb-0 me-3 align-self-center">Interview ${conversation.status.toLowerCase()}. Your responses are being reviewed.</p>
                  <a href="index.html?skipUploads=true" class="btn btn-outline-primary">
                    <i class="bi bi-plus-circle me-2"></i>Start New Interview
                  </a>
                </div>
              `}
            </div>
          </div>
        `;
      } else {
        // Show start interview button (only if documents are uploaded)
        // Check if all required documents are uploaded (ID, Passport, ProofOfResidency, VerificationPhoto, VerificationVideo)
        const hasID = documentsStatus.uploadedTypes.includes('ID');
        const hasPassport = documentsStatus.uploadedTypes.includes('Passport');
        const hasProofOfResidency = documentsStatus.uploadedTypes.includes('ProofOfResidency');
        const hasPhoto = documentsStatus.uploadedTypes.includes('VerificationPhoto');
        const hasVideo = documentsStatus.uploadedTypes.includes('VerificationVideo');
        const allDocumentsUploaded = hasID && hasPassport && hasProofOfResidency && hasPhoto && hasVideo;
        
        if (allDocumentsUploaded) {
          interviewSection.innerHTML = `
            <div class="status-card text-center">
              <div class="mb-4">
                <i class="bi bi-chat-dots" style="font-size: 4rem; color: var(--primary);"></i>
              </div>
              <h4 class="text-white mb-3">No Interview Started</h4>
              <p class="text-secondary mb-4">You haven't started an interview yet. Click the button below to begin.</p>
              <a href="index.html" class="btn btn-primary btn-lg">
                <i class="bi bi-arrow-right-circle me-2"></i>Start Interview
              </a>
            </div>
          `;
        } else {
          interviewSection.innerHTML = `
            <div class="status-card text-center">
              <div class="mb-4">
                <i class="bi bi-chat-dots" style="font-size: 4rem; color: var(--text-secondary);"></i>
              </div>
              <h4 class="text-white mb-3">Upload Documents First</h4>
              <p class="text-secondary mb-4">Please upload all required documents before starting your interview.</p>
              <p class="text-warning mb-3">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Missing: ${documentsStatus.missingTypes.join(', ')}
              </p>
              <div class="d-flex gap-2 justify-content-center">
                <a href="index.html?skipUploads=true" class="btn btn-outline-warning">
                  <i class="bi bi-skip-forward me-2"></i>Skip Uploads (Demo)
                </a>
              </div>
            </div>
          `;
        }
      }

      document.getElementById('statusMessage').textContent = getStatusMessage(client.status);

      loadingState.classList.add('d-none');
      dashboardContent.classList.remove('d-none');
    } catch (error) {
      console.error(error);
      loadingState.classList.add('d-none');
      errorState.classList.remove('d-none');
    }
  } catch (error) {
    console.error('Auth check failed:', error);
    window.location.href = 'login.html';
  }
}

// Update documents section
function updateDocumentsSection(documentsStatus, documents) {
  const documentsSection = document.getElementById('documentsSection');
  if (!documentsSection) return;

  const requiredTypes = ['ID', 'Passport', 'ProofOfResidency'];
  const uploadedByType = {};
  documents.forEach(doc => {
    if (!uploadedByType[doc.documentType]) {
      uploadedByType[doc.documentType] = [];
    }
    uploadedByType[doc.documentType].push(doc);
  });

  let documentsHTML = `
    <div class="status-card">
      <h4 class="text-white mb-3">
        <i class="bi bi-file-earmark-arrow-up me-2"></i>Required Documents
      </h4>
  `;

  requiredTypes.forEach(type => {
    const uploaded = uploadedByType[type] || [];
    const isUploaded = uploaded.length > 0;
    
    // Custom descriptions for specific document types
    let description = '';
    if (type === 'ProofOfResidency') {
      description = '<small class="text-secondary d-block"><i class="bi bi-info-circle me-1"></i>Bills or statements showing name, email, phone, and address</small>';
    }
    
    documentsHTML += `
      <div class="info-item">
        <div class="d-flex justify-content-between align-items-center">
          <div>
            <span class="info-label">${type === 'ProofOfResidency' ? 'Proof of Residency' : type}</span>
            ${description}
            ${isUploaded ? `
              <small class="text-success d-block mt-1">
                <i class="bi bi-check-circle me-1"></i>Uploaded: ${uploaded[0].fileName}
              </small>
            ` : `
              <small class="text-warning d-block mt-1">
                <i class="bi bi-x-circle me-1"></i>Not uploaded
              </small>
            `}
          </div>
          ${isUploaded ? `
            <button class="btn btn-sm btn-outline-danger" onclick="deleteDocument(${uploaded[0].id})">
              <i class="bi bi-trash"></i>
            </button>
          ` : `
            <button class="btn btn-sm btn-primary" onclick="showUploadModal('${type}')">
              <i class="bi bi-upload me-1"></i>Upload
            </button>
          `}
        </div>
      </div>
    `;
  });

  // Add verification photo section
  const verificationPhoto = uploadedByType['VerificationPhoto'] || [];
  const hasVerificationPhoto = verificationPhoto.length > 0;
  documentsHTML += `
    <hr class="my-3" style="border-color: var(--border);">
    <div class="info-item">
      <div class="d-flex justify-content-between align-items-center">
        <div>
          <span class="info-label">Verification Photo</span>
          <small class="text-secondary d-block">
            <i class="bi bi-info-circle me-1"></i>Photo of you holding paper with name and date
          </small>
          ${hasVerificationPhoto ? `
            <small class="text-success d-block mt-1">
              <i class="bi bi-check-circle me-1"></i>Uploaded: ${verificationPhoto[0].fileName}
            </small>
          ` : `
            <small class="text-warning d-block mt-1">
              <i class="bi bi-x-circle me-1"></i>Not uploaded
            </small>
          `}
        </div>
        ${hasVerificationPhoto ? `
          <button class="btn btn-sm btn-outline-danger" onclick="deleteDocument(${verificationPhoto[0].id})">
            <i class="bi bi-trash"></i>
          </button>
        ` : `
          <button class="btn btn-sm btn-primary" onclick="showVerificationPhotoModal()">
            <i class="bi bi-camera me-1"></i>Upload Photo
          </button>
        `}
      </div>
    </div>
  `;

  // Add verification video section
  const verificationVideo = uploadedByType['VerificationVideo'] || [];
  const hasVerificationVideo = verificationVideo.length > 0;
  documentsHTML += `
    <div class="info-item">
      <div class="d-flex justify-content-between align-items-center">
        <div>
          <span class="info-label">Verification Video</span>
          <small class="text-secondary d-block">
            <i class="bi bi-info-circle me-1"></i>Record video saying random phrase (120 seconds)
          </small>
          ${hasVerificationVideo ? `
            <small class="text-success d-block mt-1">
              <i class="bi bi-check-circle me-1"></i>Submitted: ${verificationVideo[0].fileName}
            </small>
          ` : `
            <small class="text-warning d-block mt-1">
              <i class="bi bi-x-circle me-1"></i>Not submitted
            </small>
          `}
        </div>
        ${hasVerificationVideo ? `
          <button class="btn btn-sm btn-outline-danger" onclick="deleteDocument(${verificationVideo[0].id})">
            <i class="bi bi-trash"></i>
          </button>
        ` : `
          <button class="btn btn-sm btn-primary" onclick="showVerificationVideoModal()">
            <i class="bi bi-camera-video me-1"></i>Record Video
          </button>
        `}
      </div>
    </div>
  `;

  // Show uploaded documents list
  if (documents.length > 0) {
    documentsHTML += `
      <hr class="my-3" style="border-color: var(--border);">
      <h5 class="text-white mb-2">All Uploaded Documents</h5>
    `;
    documents.forEach(doc => {
      const fileSizeMB = (doc.fileSize / (1024 * 1024)).toFixed(2);
      documentsHTML += `
        <div class="info-item">
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <span class="info-label">${doc.documentType}</span>
              <small class="text-secondary d-block">${doc.fileName} (${fileSizeMB} MB)</small>
            </div>
            <button class="btn btn-sm btn-outline-danger" onclick="deleteDocument(${doc.id})">
              <i class="bi bi-trash"></i>
            </button>
          </div>
        </div>
      `;
    });
  }

  documentsHTML += `</div>`;

  documentsSection.innerHTML = documentsHTML;
}

// Show upload modal
window.showUploadModal = function(documentType) {
  const modal = document.getElementById('uploadModal');
  const documentTypeInput = document.getElementById('uploadDocumentType');
  const fileInput = document.getElementById('uploadFileInput');
  const instructionsDiv = document.getElementById('uploadDocumentInstructions');
  
  if (documentTypeInput) {
    documentTypeInput.value = documentType;
  }
  if (fileInput) {
    fileInput.value = '';
  }
  
  // Show specific instructions for Proof of Residency
  if (instructionsDiv) {
    if (documentType === 'ProofOfResidency') {
      instructionsDiv.innerHTML = `
        <div class="alert alert-info mb-0 mt-2">
          <small>
            <strong>Acceptable documents:</strong> Utility bills, bank statements, credit card statements, 
            or other official documents that clearly show your name, email address, phone number, and physical address.
          </small>
        </div>
      `;
    } else {
      instructionsDiv.innerHTML = '';
    }
  }
  
  // Show Bootstrap modal
  const bootstrapModal = new bootstrap.Modal(modal);
  bootstrapModal.show();
};

// Show upload modal
window.showUploadModal = function(documentType) {
  const modal = document.getElementById('uploadModal');
  const documentTypeInput = document.getElementById('uploadDocumentType');
  const fileInput = document.getElementById('uploadFileInput');
  const instructionsDiv = document.getElementById('uploadDocumentInstructions');
  
  if (documentTypeInput) {
    documentTypeInput.value = documentType;
  }
  if (fileInput) {
    fileInput.value = '';
  }
  
  // Show specific instructions for Proof of Residency
  if (instructionsDiv) {
    if (documentType === 'ProofOfResidency') {
      instructionsDiv.innerHTML = `
        <div class="alert alert-info mb-0 mt-2">
          <small>
            <strong>Acceptable documents:</strong> Utility bills, bank statements, credit card statements, 
            or other official documents that clearly show your name, email address, phone number, and physical address.
          </small>
        </div>
      `;
    } else {
      instructionsDiv.innerHTML = '';
    }
  }
  
  // Show Bootstrap modal
  const bootstrapModal = new bootstrap.Modal(modal);
  bootstrapModal.show();
};

// Delete document
async function deleteDocument(documentId) {
  if (!confirm('Are you sure you want to delete this document?')) {
    return;
  }

  try {
    const response = await fetch(`${API_BASE}/documents/${documentId}`, {
      method: 'DELETE',
      credentials: 'include'
    });

    if (response.ok) {
      // Reload dashboard to refresh document list
      loadDashboard();
    } else {
      alert('Failed to delete document');
    }
  } catch (error) {
    console.error('Error deleting document:', error);
    alert('Error deleting document');
  }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
  loadDashboard();
});
