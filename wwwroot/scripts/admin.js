const API_BASE = '/api/admin';

document.addEventListener('DOMContentLoaded', () => {
  loadQuestions();
  loadCriteria();
  loadRedFlags();
  loadSettings();
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
    container.innerHTML = '<p class="text-secondary">No questions yet.</p>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-hover"><thead class="table-light"><tr>';
  html += '<th>Question</th><th>Category</th><th>Priority</th><th>Required</th><th>Active</th><th>Actions</th>';
  html += '</tr></thead><tbody>';

  questions.forEach((q) => {
    html += `
      <tr>
        <td>${q.questionText}</td>
        <td><span class="badge bg-secondary">${q.category}</span></td>
        <td>${q.priority}</td>
        <td>${q.isRequired ? '<i class="bi bi-check-circle text-success"></i>' : ''}</td>
        <td>${q.isActive ? '<i class="bi bi-check-circle text-success"></i>' : '<i class="bi bi-x-circle text-danger"></i>'}</td>
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
    container.innerHTML = '<p class="text-secondary">No criteria yet.</p>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-hover"><thead class="table-light"><tr>';
  html += '<th>Name</th><th>Category</th><th>Weight</th><th>Active</th><th>Actions</th>';
  html += '</tr></thead><tbody>';

  criteriaList.forEach((c) => {
    html += `
      <tr>
        <td>
          <strong>${c.name}</strong>
          ${c.description ? `<br><small class="text-secondary">${c.description}</small>` : ''}
        </td>
        <td>${c.category ? `<span class="badge bg-secondary">${c.category}</span>` : '-'}</td>
        <td>${c.weight.toFixed(1)}</td>
        <td>${c.isActive ? '<i class="bi bi-check-circle text-success"></i>' : '<i class="bi bi-x-circle text-danger"></i>'}</td>
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
    container.innerHTML = '<p class="text-secondary">No red flags yet.</p>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-hover"><thead class="table-light"><tr>';
  html += '<th>Name</th><th>Severity</th><th>Keywords</th><th>Active</th><th>Actions</th>';
  html += '</tr></thead><tbody>';

  redFlags.forEach((rf) => {
    const severityBadge = {
      Low: 'bg-secondary',
      Medium: 'bg-warning',
      High: 'bg-danger',
      Critical: 'bg-dark',
    }[rf.severity];

    html += `
      <tr>
        <td>
          <strong>${rf.name}</strong>
          ${rf.description ? `<br><small class="text-secondary">${rf.description}</small>` : ''}
        </td>
        <td><span class="badge ${severityBadge}">${rf.severity}</span></td>
        <td><small>${rf.detectionKeywords || '-'}</small></td>
        <td>${rf.isActive ? '<i class="bi bi-check-circle text-success"></i>' : '<i class="bi bi-x-circle text-danger"></i>'}</td>
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

// ===== SETTINGS =====

async function loadSettings() {
  const container = document.getElementById('settingsForm');
  container.innerHTML = '<div class="text-center"><div class="spinner-border"></div></div>';

  try {
    const response = await fetch(`${API_BASE}/settings`);
    if (!response.ok) throw new Error('Failed to load settings');

    const settings = await response.json();
    displaySettings(settings);
  } catch (error) {
    console.error(error);
    container.innerHTML = '<div class="alert alert-danger">Failed to load settings</div>';
  }
}

function displaySettings(settings) {
  const container = document.getElementById('settingsForm');

  let html = '<form id="settingsUpdateForm">';

  settings.forEach((setting) => {
    html += `
      <div class="mb-3">
        <label class="form-label"><strong>${setting.settingKey}</strong></label>
        ${setting.description ? `<div class="form-text">${setting.description}</div>` : ''}
        <input 
          type="text" 
          class="form-control" 
          data-setting-key="${setting.settingKey}" 
          value="${setting.settingValue || ''}"
        />
      </div>
    `;
  });

  html += '<button type="submit" class="btn btn-primary">Save Settings</button>';
  html += '</form>';

  container.innerHTML = html;

  document.getElementById('settingsUpdateForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    await updateSettings();
  });
}

async function updateSettings() {
  const inputs = document.querySelectorAll('[data-setting-key]');
  const updates = [];

  inputs.forEach((input) => {
    updates.push({
      settingKey: input.dataset.settingKey,
      settingValue: input.value,
    });
  });

  try {
    for (const update of updates) {
      const response = await fetch(`${API_BASE}/settings`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(update),
      });

      if (!response.ok) throw new Error(`Failed to update ${update.settingKey}`);
    }

    alert('Settings updated successfully');
  } catch (error) {
    console.error(error);
    alert('Failed to update some settings');
  }
}

