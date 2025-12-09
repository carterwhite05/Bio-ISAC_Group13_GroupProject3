const API_BASE = '/api/admin';

let dossierModal;

document.addEventListener('DOMContentLoaded', () => {
  dossierModal = new bootstrap.Modal(document.getElementById('dossierModal'));
  loadQuestions();
  loadCriteria();
  loadRedFlags();
  loadClients();
  
  // Handle hash navigation to tabs
  if (window.location.hash) {
    const hash = window.location.hash.substring(1);
    const tabButton = document.querySelector(`button[data-bs-target="#${hash}"]`);
    if (tabButton) {
      const tab = new bootstrap.Tab(tabButton);
      tab.show();
      if (hash === 'clients') {
        loadClients();
      }
    }
  }
});

// ===== QUESTIONS =====

async function loadQuestions() {
  const container = document.getElementById('questionsTable');
  container.innerHTML = '<div class="text-center"><div class="spinner-border"></div></div>';

  try {
    const response = await fetch(`${API_BASE}/questions`);
    if (!response.ok) throw new Error('Failed to load questions');

    const questions = await response.json();
    displayQuestions(questions);
  } catch (error) {
    console.error(error);
    container.innerHTML = '<div class="alert alert-danger">Failed to load questions</div>';
  }
}

function displayQuestions(questions) {
  const container = document.getElementById('questionsTable');

  if (questions.length === 0) {
    container.innerHTML = '<p class="text-muted">No questions yet.</p>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-hover"><thead><tr>';
  html += '<th>Question</th><th>Category</th><th>Priority</th><th>Required</th><th>Active</th><th>Actions</th>';
  html += '</tr></thead><tbody>';

  questions.forEach((q) => {
    html += `
      <tr>
        <td class="text-white fw-normal">${q.questionText}</td>
        <td><span class="badge bg-primary">${q.category}</span></td>
        <td class="text-white">${q.priority}</td>
        <td>${q.isRequired ? '<i class="bi bi-check-circle text-success fs-5"></i>' : '<i class="bi bi-x-circle text-muted fs-5"></i>'}</td>
        <td>${q.isActive ? '<i class="bi bi-check-circle text-success fs-5"></i>' : '<i class="bi bi-x-circle text-danger fs-5"></i>'}</td>
        <td>
          <button class="btn btn-sm btn-outline-primary" onclick="editQuestion(${q.id})">Edit</button>
          <button class="btn btn-sm btn-outline-danger" onclick="deleteQuestion(${q.id})">Delete</button>
        </td>
      </tr>
    `;
  });

  html += '</tbody></table></div>';
  container.innerHTML = html;
}

function showAddQuestionModal() {
  const modal = createQuestionModal();
  modal.show();
}

async function editQuestion(id) {
  try {
    const response = await fetch(`${API_BASE}/questions`);
    const questions = await response.json();
    const question = questions.find((q) => q.id === id);

    if (!question) return;

    const modal = createQuestionModal(question);
    modal.show();
  } catch (error) {
    console.error(error);
    alert('Failed to load question');
  }
}

function createQuestionModal(question = null) {
  const isEdit = question !== null;
  const modalId = 'questionModal';

  const modalHTML = `
    <div class="modal fade" id="${modalId}" tabindex="-1">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">${isEdit ? 'Edit' : 'Add'} Question</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form id="questionForm">
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Question Text *</label>
                <textarea class="form-control" id="questionText" rows="3" required>${
                  question?.questionText || ''
                }</textarea>
              </div>
              <div class="mb-3">
                <label class="form-label">Category *</label>
                <input type="text" class="form-control" id="category" value="${
                  question?.category || ''
                }" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Priority (0-10)</label>
                <input type="number" class="form-control" id="priority" min="0" max="10" value="${
                  question?.priority || 5
                }" />
              </div>
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" id="isRequired" ${
                  question?.isRequired ? 'checked' : ''
                } />
                <label class="form-check-label" for="isRequired">Required Question</label>
              </div>
              ${
                isEdit
                  ? `
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" id="isActive" ${
                  question?.isActive ? 'checked' : ''
                } />
                <label class="form-check-label" for="isActive">Active</label>
              </div>
              `
                  : ''
              }
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
              <button type="submit" class="btn btn-primary">Save</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `;

  document.getElementById('modalContainer').innerHTML = modalHTML;
  const modalElement = document.getElementById(modalId);
  const modal = new bootstrap.Modal(modalElement);

  document.getElementById('questionForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const data = {
      questionText: document.getElementById('questionText').value,
      category: document.getElementById('category').value,
      priority: parseInt(document.getElementById('priority').value),
      isRequired: document.getElementById('isRequired').checked,
    };

    if (isEdit) {
      data.isActive = document.getElementById('isActive').checked;
    }

    try {
      const url = isEdit ? `${API_BASE}/questions/${question.id}` : `${API_BASE}/questions`;
      const method = isEdit ? 'PUT' : 'POST';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });

      if (!response.ok) throw new Error('Failed to save question');

      modal.hide();
      loadQuestions();
    } catch (error) {
      console.error(error);
      alert('Failed to save question');
    }
  });

  return modal;
}

async function deleteQuestion(id) {
  if (!confirm('Delete this question?')) return;

  try {
    const response = await fetch(`${API_BASE}/questions/${id}`, { method: 'DELETE' });
    if (!response.ok) throw new Error('Failed to delete');

    loadQuestions();
  } catch (error) {
    console.error(error);
    alert('Failed to delete question');
  }
}

// ===== CRITERIA =====

async function loadCriteria() {
  const container = document.getElementById('criteriaTable');
  container.innerHTML = '<div class="text-center"><div class="spinner-border"></div></div>';

  try {
    const response = await fetch(`${API_BASE}/criteria`);
    if (!response.ok) throw new Error('Failed to load criteria');

    const criteria = await response.json();
    displayCriteria(criteria);
  } catch (error) {
    console.error(error);
    container.innerHTML = '<div class="alert alert-danger">Failed to load criteria</div>';
  }
}

function displayCriteria(criteriaList) {
  const container = document.getElementById('criteriaTable');

  if (criteriaList.length === 0) {
    container.innerHTML = '<p class="text-muted">No criteria yet.</p>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-hover"><thead><tr>';
  html += '<th>Name</th><th>Category</th><th>Weight</th><th>Active</th><th>Actions</th>';
  html += '</tr></thead><tbody>';

  criteriaList.forEach((c) => {
    html += `
      <tr>
        <td>
          <strong class="text-white">${c.name}</strong>
          ${c.description ? `<br><small class="text-muted">${c.description}</small>` : ''}
        </td>
        <td>${c.category ? `<span class="badge bg-primary">${c.category}</span>` : '<span class="text-muted">-</span>'}</td>
        <td class="text-white">${c.weight.toFixed(1)}</td>
        <td>${c.isActive ? '<i class="bi bi-check-circle text-success fs-5"></i>' : '<i class="bi bi-x-circle text-danger fs-5"></i>'}</td>
        <td>
          <button class="btn btn-sm btn-outline-primary" onclick="editCriteria(${c.id})">Edit</button>
          <button class="btn btn-sm btn-outline-danger" onclick="deleteCriteria(${c.id})">Delete</button>
        </td>
      </tr>
    `;
  });

  html += '</tbody></table></div>';
  container.innerHTML = html;
}

function showAddCriteriaModal() {
  const modal = createCriteriaModal();
  modal.show();
}

async function editCriteria(id) {
  try {
    const response = await fetch(`${API_BASE}/criteria`);
    const criteriaList = await response.json();
    const criteria = criteriaList.find((c) => c.id === id);

    if (!criteria) return;

    const modal = createCriteriaModal(criteria);
    modal.show();
  } catch (error) {
    console.error(error);
    alert('Failed to load criteria');
  }
}

function createCriteriaModal(criteria = null) {
  const isEdit = criteria !== null;
  const modalId = 'criteriaModal';

  const modalHTML = `
    <div class="modal fade" id="${modalId}" tabindex="-1">
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">${isEdit ? 'Edit' : 'Add'} Criteria</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form id="criteriaForm">
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Name *</label>
                <input type="text" class="form-control" id="criteriaName" value="${
                  criteria?.name || ''
                }" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Description</label>
                <textarea class="form-control" id="criteriaDescription" rows="2">${
                  criteria?.description || ''
                }</textarea>
              </div>
              <div class="mb-3">
                <label class="form-label">Category</label>
                <input type="text" class="form-control" id="criteriaCategory" value="${
                  criteria?.category || ''
                }" />
              </div>
              <div class="mb-3">
                <label class="form-label">Weight (0.1 - 5.0)</label>
                <input type="number" class="form-control" id="criteriaWeight" step="0.1" min="0.1" max="5" value="${
                  criteria?.weight || 1.0
                }" />
              </div>
              <div class="mb-3">
                <label class="form-label">Evaluation Prompt</label>
                <textarea class="form-control" id="criteriaPrompt" rows="3">${
                  criteria?.evaluationPrompt || ''
                }</textarea>
              </div>
              ${
                isEdit
                  ? `
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" id="criteriaActive" ${
                  criteria?.isActive ? 'checked' : ''
                } />
                <label class="form-check-label" for="criteriaActive">Active</label>
              </div>
              `
                  : ''
              }
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
              <button type="submit" class="btn btn-primary">Save</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `;

  document.getElementById('modalContainer').innerHTML = modalHTML;
  const modalElement = document.getElementById(modalId);
  const modal = new bootstrap.Modal(modalElement);

  document.getElementById('criteriaForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const data = {
      name: document.getElementById('criteriaName').value,
      description: document.getElementById('criteriaDescription').value || null,
      category: document.getElementById('criteriaCategory').value || null,
      weight: parseFloat(document.getElementById('criteriaWeight').value),
      evaluationPrompt: document.getElementById('criteriaPrompt').value || null,
    };

    if (isEdit) {
      data.isActive = document.getElementById('criteriaActive').checked;
    }

    try {
      const url = isEdit ? `${API_BASE}/criteria/${criteria.id}` : `${API_BASE}/criteria`;
      const method = isEdit ? 'PUT' : 'POST';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });

      if (!response.ok) throw new Error('Failed to save criteria');

      modal.hide();
      loadCriteria();
    } catch (error) {
      console.error(error);
      alert('Failed to save criteria');
    }
  });

  return modal;
}

async function deleteCriteria(id) {
  if (!confirm('Delete this criteria?')) return;

  try {
    const response = await fetch(`${API_BASE}/criteria/${id}`, { method: 'DELETE' });
    if (!response.ok) throw new Error('Failed to delete');

    loadCriteria();
  } catch (error) {
    console.error(error);
    alert('Failed to delete criteria');
  }
}

// ===== RED FLAGS =====

async function loadRedFlags() {
  const container = document.getElementById('redflagsTable');
  container.innerHTML = '<div class="text-center"><div class="spinner-border"></div></div>';

  try {
    const response = await fetch(`${API_BASE}/redflags`);
    if (!response.ok) throw new Error('Failed to load red flags');

    const redFlags = await response.json();
    displayRedFlags(redFlags);
  } catch (error) {
    console.error(error);
    container.innerHTML = '<div class="alert alert-danger">Failed to load red flags</div>';
  }
}

function displayRedFlags(redFlags) {
  const container = document.getElementById('redflagsTable');

  if (redFlags.length === 0) {
    container.innerHTML = '<p class="text-muted">No red flags yet.</p>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-hover"><thead><tr>';
  html += '<th>Name</th><th>Severity</th><th>Keywords</th><th>Active</th><th>Actions</th>';
  html += '</tr></thead><tbody>';

  redFlags.forEach((rf) => {
    const severityBadge = {
      Low: 'bg-secondary',
      Medium: 'bg-warning text-dark',
      High: 'bg-danger',
      Critical: 'bg-dark',
    }[rf.severity];

    html += `
      <tr>
        <td>
          <strong class="text-white">${rf.name}</strong>
          ${rf.description ? `<br><small class="text-muted">${rf.description}</small>` : ''}
        </td>
        <td><span class="badge ${severityBadge}">${rf.severity}</span></td>
        <td><small class="text-white">${rf.detectionKeywords || '<span class="text-muted">-</span>'}</small></td>
        <td>${rf.isActive ? '<i class="bi bi-check-circle text-success fs-5"></i>' : '<i class="bi bi-x-circle text-danger fs-5"></i>'}</td>
        <td>
          <button class="btn btn-sm btn-outline-primary" onclick="editRedFlag(${rf.id})">Edit</button>
          <button class="btn btn-sm btn-outline-danger" onclick="deleteRedFlag(${rf.id})">Delete</button>
        </td>
      </tr>
    `;
  });

  html += '</tbody></table></div>';
  container.innerHTML = html;
}

function showAddRedFlagModal() {
  const modal = createRedFlagModal();
  modal.show();
}

async function editRedFlag(id) {
  try {
    const response = await fetch(`${API_BASE}/redflags`);
    const redFlags = await response.json();
    const redFlag = redFlags.find((rf) => rf.id === id);

    if (!redFlag) return;

    const modal = createRedFlagModal(redFlag);
    modal.show();
  } catch (error) {
    console.error(error);
    alert('Failed to load red flag');
  }
}

function createRedFlagModal(redFlag = null) {
  const isEdit = redFlag !== null;
  const modalId = 'redFlagModal';

  const modalHTML = `
    <div class="modal fade" id="${modalId}" tabindex="-1">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">${isEdit ? 'Edit' : 'Add'} Red Flag</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <form id="redFlagForm">
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Name *</label>
                <input type="text" class="form-control" id="redFlagName" value="${
                  redFlag?.name || ''
                }" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Description</label>
                <textarea class="form-control" id="redFlagDescription" rows="2">${
                  redFlag?.description || ''
                }</textarea>
              </div>
              <div class="mb-3">
                <label class="form-label">Severity *</label>
                <select class="form-select" id="redFlagSeverity" required>
                  <option value="Low" ${redFlag?.severity === 'Low' ? 'selected' : ''}>Low</option>
                  <option value="Medium" ${redFlag?.severity === 'Medium' ? 'selected' : ''}>Medium</option>
                  <option value="High" ${redFlag?.severity === 'High' ? 'selected' : ''}>High</option>
                  <option value="Critical" ${redFlag?.severity === 'Critical' ? 'selected' : ''}>Critical</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label">Detection Keywords (comma-separated)</label>
                <input type="text" class="form-control" id="redFlagKeywords" value="${
                  redFlag?.detectionKeywords || ''
                }" />
              </div>
              ${
                isEdit
                  ? `
              <div class="form-check mb-3">
                <input class="form-check-input" type="checkbox" id="redFlagActive" ${
                  redFlag?.isActive ? 'checked' : ''
                } />
                <label class="form-check-label" for="redFlagActive">Active</label>
              </div>
              `
                  : ''
              }
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
              <button type="submit" class="btn btn-primary">Save</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `;

  document.getElementById('modalContainer').innerHTML = modalHTML;
  const modalElement = document.getElementById(modalId);
  const modal = new bootstrap.Modal(modalElement);

  document.getElementById('redFlagForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const data = {
      name: document.getElementById('redFlagName').value,
      description: document.getElementById('redFlagDescription').value || null,
      severity: document.getElementById('redFlagSeverity').value,
      detectionKeywords: document.getElementById('redFlagKeywords').value || null,
    };

    if (isEdit) {
      data.isActive = document.getElementById('redFlagActive').checked;
    }

    try {
      const url = isEdit ? `${API_BASE}/redflags/${redFlag.id}` : `${API_BASE}/redflags`;
      const method = isEdit ? 'PUT' : 'POST';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });

      if (!response.ok) throw new Error('Failed to save red flag');

      modal.hide();
      loadRedFlags();
    } catch (error) {
      console.error(error);
      alert('Failed to save red flag');
    }
  });

  return modal;
}

async function deleteRedFlag(id) {
  if (!confirm('Delete this red flag?')) return;

  try {
    const response = await fetch(`${API_BASE}/redflags/${id}`, { method: 'DELETE' });
    if (!response.ok) throw new Error('Failed to delete');

    loadRedFlags();
  } catch (error) {
    console.error(error);
    alert('Failed to delete red flag');
  }
}

// ===== CLIENTS =====

window.loadClients = async function() {
  const container = document.getElementById('clientsTable');
  if (!container) {
    console.error('clientsTable container not found');
    return;
  }
  
  container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary"></div><p class="text-secondary mt-2">Loading clients...</p></div>';

  try {
    const response = await fetch('/api/clients');
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to load clients: ${response.status} ${errorText}`);
    }

    const clients = await response.json();
    console.log('Loaded clients:', clients);
    displayClients(clients);
  } catch (error) {
    console.error('Error loading clients:', error);
    container.innerHTML = `<div class="alert alert-danger">Failed to load clients: ${error.message}</div>`;
  }
};

function displayClients(clients) {
  const container = document.getElementById('clientsTable');
  if (!container) {
    console.error('clientsTable container not found in displayClients');
    return;
  }

  if (!clients || clients.length === 0) {
    container.innerHTML = '<div class="alert alert-info"><p class="mb-0">No clients yet.</p></div>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-hover"><thead><tr>';
  html += '<th>Email</th><th>Name</th><th>Status</th><th>Score</th><th>Conversations</th><th>Entries</th><th>Red Flags</th><th>Created</th><th>Actions</th>';
  html += '</tr></thead><tbody>';

  clients.forEach((client) => {
    const fullName = client.firstName || client.lastName
      ? `${client.firstName || ''} ${client.lastName || ''}`.trim()
      : '-';

    const statusBadge = getClientStatusBadge(client.status);
    const scoreColor = getClientScoreColor(client.overallScore || 0);
    const score = (client.overallScore || 0).toFixed(1);

    // Escape single quotes in status for onclick
    const escapedStatus = (client.status || '').replace(/'/g, "\\'");

    html += `
      <tr>
        <td class="text-white fw-normal">${client.email || '-'}</td>
        <td class="text-white">${fullName}</td>
        <td>${statusBadge}</td>
        <td><span class="badge ${scoreColor}">${score}</span></td>
        <td class="text-white">${client.conversationCount || 0}</td>
        <td class="text-white">${client.dossierEntryCount || 0}</td>
        <td>${
          client.redFlagCount > 0
            ? `<span class="badge bg-danger">${client.redFlagCount}</span>`
            : '<span class="text-white">-</span>'
        }</td>
        <td class="text-white">${client.createdAt ? new Date(client.createdAt).toLocaleDateString() : '-'}</td>
        <td>
          <div class="btn-group" role="group">
            <button class="btn btn-sm btn-primary" onclick="viewClientDossier(${client.id})" title="View Dossier">
              <i class="bi bi-eye"></i> View
            </button>
            <button class="btn btn-sm btn-success" onclick="evaluateClient(${client.id})" title="Re-evaluate">
              <i class="bi bi-arrow-repeat"></i> Re-evaluate
            </button>
            <button class="btn btn-sm btn-outline-danger" onclick="deleteClient(${client.id})" title="Delete">
              <i class="bi bi-trash"></i> Delete
            </button>
            <button class="btn btn-sm btn-outline-warning" onclick="updateClientStatus(${client.id}, '${escapedStatus}')" title="Update Status">
              <i class="bi bi-pencil"></i> Update Status
            </button>
          </div>
        </td>
      </tr>
    `;
  });

  html += '</tbody></table></div>';
  container.innerHTML = html;
}

function getClientStatusBadge(status) {
  const badges = {
    Pending: '<span class="badge bg-warning">Pending</span>',
    Approved: '<span class="badge bg-success">Approved</span>',
    Rejected: '<span class="badge bg-danger">Rejected</span>',
    InProgress: '<span class="badge bg-info">In Progress</span>',
    InterviewCompleted: '<span class="badge bg-primary">Interview Completed</span>',
    UnderReview: '<span class="badge bg-secondary">Under Review</span>'
  };
  return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
}

function getClientScoreColor(score) {
  if (score >= 70) return 'bg-success';
  if (score >= 50) return 'bg-warning';
  return 'bg-danger';
}

window.viewClientDossier = async function(clientId) {
  const content = document.getElementById('dossierContent');
  if (!content) return;
  
  content.innerHTML = `
    <div class="text-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>
  `;

  dossierModal.show();

  try {
    const response = await fetch(`/api/clients/${clientId}/dossier`);
    if (!response.ok) throw new Error('Failed to load dossier');

    const dossier = await response.json();
    displayClientDossier(dossier);
  } catch (error) {
    console.error(error);
    content.innerHTML = `
      <div class="alert alert-danger">
        Failed to load dossier. Please try again.
      </div>
    `;
  }
}

function displayClientDossier(dossier) {
  const content = document.getElementById('dossierContent');
  if (!content) return;

  const fullName = dossier.firstName || dossier.lastName
    ? `${dossier.firstName || ''} ${dossier.lastName || ''}`.trim()
    : 'No name provided';

  let html = `
    <div class="row mb-4">
      <div class="col-md-6">
        <h5 class="text-white">${fullName}</h5>
        <p class="text-secondary mb-1">${dossier.clientEmail}</p>
        <p class="mb-0">
          ${getClientStatusBadge(dossier.status)}
          <span class="badge ${getClientScoreColor(dossier.overallScore)} ms-2">
            Score: ${dossier.overallScore.toFixed(1)}
          </span>
        </p>
      </div>
      <div class="col-md-6 text-end">
        <small class="text-secondary">
          Created: ${new Date(dossier.createdAt).toLocaleString()}
        </small>
      </div>
    </div>
  `;

  // Red Flags
  if (dossier.redFlags && dossier.redFlags.length > 0) {
    html += `
      <div class="alert alert-danger">
        <h6 class="alert-heading"><i class="bi bi-exclamation-triangle"></i> Red Flags Detected</h6>
        <ul class="mb-0">
    `;
    dossier.redFlags.forEach((flag) => {
      html += `
        <li>
          <strong>${flag.redFlagName}</strong> (${flag.severity})
          ${flag.detectionReason ? ` - ${flag.detectionReason}` : ''}
          <small class="text-secondary">(Confidence: ${(flag.confidenceScore * 100).toFixed(0)}%)</small>
        </li>
      `;
    });
    html += '</ul></div>';
  }

  // Dossier Entries by Category
  html += '<h6 class="mt-4 mb-3 text-white">Dossier Information</h6>';

  const categories = {};
  if (dossier.entries) {
    dossier.entries.forEach((entry) => {
      if (!categories[entry.category]) {
        categories[entry.category] = [];
      }
      categories[entry.category].push(entry);
    });
  }

  if (Object.keys(categories).length === 0) {
    html += '<p class="text-secondary">No information extracted yet.</p>';
  } else {
    html += '<div class="accordion" id="dossierAccordion">';

    Object.entries(categories).forEach(([category, entries], index) => {
      const categoryId = `category-${index}`;
      html += `
        <div class="accordion-item" style="background: var(--bg-secondary); border-color: var(--border);">
          <h2 class="accordion-header" id="heading-${categoryId}">
            <button
              class="accordion-button ${index !== 0 ? 'collapsed' : ''}"
              type="button"
              data-bs-toggle="collapse"
              data-bs-target="#collapse-${categoryId}"
              aria-expanded="${index === 0 ? 'true' : 'false'}"
              aria-controls="collapse-${categoryId}"
              style="background: var(--bg-secondary); color: var(--text-primary);"
            >
              ${formatDossierCategory(category)} (${entries.length})
            </button>
          </h2>
          <div
            id="collapse-${categoryId}"
            class="accordion-collapse collapse ${index === 0 ? 'show' : ''}"
            aria-labelledby="heading-${categoryId}"
            data-bs-parent="#dossierAccordion"
          >
            <div class="accordion-body" style="background: var(--bg-card);">
              <table class="table table-sm">
                <thead>
                  <tr>
                    <th>Key</th>
                    <th>Value</th>
                    <th>Confidence</th>
                  </tr>
                </thead>
                <tbody>
      `;

      entries.forEach((entry) => {
        const confidenceColor = entry.confidenceScore >= 0.7 ? 'success' : 'warning';
        html += `
          <tr>
            <td class="text-white"><strong>${entry.keyName}</strong></td>
            <td class="text-white">${entry.value}</td>
            <td>
              <span class="badge bg-${confidenceColor}">
                ${(entry.confidenceScore * 100).toFixed(0)}%
              </span>
            </td>
          </tr>
        `;
      });

      html += `
                </tbody>
              </table>
            </div>
          </div>
        </div>
      `;
    });

    html += '</div>';
  }

  content.innerHTML = html;
}

function formatDossierCategory(category) {
  return category
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (str) => str.toUpperCase())
    .trim();
}

window.evaluateClient = async function(clientId) {
  if (!confirm('Re-evaluate this client? This may take a moment.')) {
    return;
  }

  try {
    const response = await fetch(`/api/clients/${clientId}/evaluate`, {
      method: 'POST',
    });

    if (!response.ok) throw new Error('Failed to evaluate client');

    const data = await response.json();
    alert(`Client evaluated successfully. Score: ${data.score.toFixed(1)}`);
    loadClients();
  } catch (error) {
    console.error(error);
    alert('Failed to evaluate client. Please try again.');
  }
}

window.deleteClient = async function(clientId) {
  if (!confirm('Are you sure you want to delete this client and all associated data? This action cannot be undone.')) {
    return;
  }

  try {
    const response = await fetch(`/api/clients/${clientId}`, {
      method: 'DELETE',
    });

    if (!response.ok) throw new Error('Failed to delete client');

    alert('Client deleted successfully.');
    loadClients();
  } catch (error) {
    console.error(error);
    alert('Failed to delete client. Please try again.');
  }
}

window.updateClientStatus = async function(clientId, currentStatus) {
  const statuses = ['Pending', 'Approved', 'Rejected', 'InProgress', 'InterviewCompleted', 'UnderReview'];
  const currentIndex = statuses.indexOf(currentStatus);
  
  const newStatus = prompt(`Select new status:\n\n${statuses.map((s, i) => `${i + 1}. ${s.replace(/([A-Z])/g, ' $1').trim()}`).join('\n')}\n\nEnter status name:`, currentStatus);
  
  if (!newStatus || newStatus === currentStatus) {
    return;
  }

  if (!statuses.includes(newStatus)) {
    alert('Invalid status. Please enter one of the valid statuses.');
    return;
  }

  try {
    const response = await fetch(`/api/clients/${clientId}/status`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status: newStatus }),
    });

    if (!response.ok) throw new Error('Failed to update client status');

    alert('Client status updated successfully.');
    loadClients();
  } catch (error) {
    console.error(error);
    alert('Failed to update client status. Please try again.');
  }
}

