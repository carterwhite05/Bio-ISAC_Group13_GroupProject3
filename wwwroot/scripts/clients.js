const API_BASE = '/api';

let dossierModal;

document.addEventListener('DOMContentLoaded', () => {
  dossierModal = new bootstrap.Modal(document.getElementById('dossierModal'));
  loadClients();
});

async function loadClients() {
  const loading = document.getElementById('clientsLoading');
  const table = document.getElementById('clientsTable');
  const tbody = document.getElementById('clientsTableBody');

  try {
    const response = await fetch(`${API_BASE}/clients`);
    if (!response.ok) throw new Error('Failed to load clients');

    const clients = await response.json();

    tbody.innerHTML = '';

    if (clients.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="9" class="text-center text-secondary">No clients yet</td>
        </tr>
      `;
    } else {
      clients.forEach((client) => {
        const row = createClientRow(client);
        tbody.appendChild(row);
      });
    }

    loading.classList.add('d-none');
    table.classList.remove('d-none');
  } catch (error) {
    console.error(error);
    loading.innerHTML = `
      <div class="alert alert-danger">
        Failed to load clients. Please refresh the page.
      </div>
    `;
  }
}

function createClientRow(client) {
  const tr = document.createElement('tr');

  const fullName =
    client.firstName || client.lastName
      ? `${client.firstName || ''} ${client.lastName || ''}`.trim()
      : '-';

  const statusBadge = getStatusBadge(client.status);
  const scoreColor = getScoreColor(client.overallScore);

  tr.innerHTML = `
    <td>${client.email}</td>
    <td>${fullName}</td>
    <td>${statusBadge}</td>
    <td><span class="badge ${scoreColor}">${client.overallScore.toFixed(
    1
  )}</span></td>
    <td>${client.conversationCount}</td>
    <td>${client.dossierEntryCount}</td>
    <td>${
      client.redFlagCount > 0
        ? `<span class="badge bg-danger">${client.redFlagCount}</span>`
        : '-'
    }</td>
    <td>${new Date(client.createdAt).toLocaleDateString()}</td>
    <td>
      <button class="btn btn-sm btn-primary" onclick="viewDossier(${
        client.id
      })">
        <i class="bi bi-eye"></i> View
      </button>
      <button class="btn btn-sm btn-success" onclick="evaluateClient(${
        client.id
      })">
        <i class="bi bi-arrow-repeat"></i> Re-evaluate
      </button>
    </td>
  `;

  return tr;
}

function getStatusBadge(status) {
  const badges = {
    Pending: '<span class="badge bg-warning">Pending</span>',
    Approved: '<span class="badge bg-success">Approved</span>',
    Rejected: '<span class="badge bg-danger">Rejected</span>',
    InProgress: '<span class="badge bg-info">In Progress</span>',
  };
  return badges[status] || status;
}

function getScoreColor(score) {
  if (score >= 70) return 'bg-success';
  if (score >= 50) return 'bg-warning';
  return 'bg-danger';
}

async function viewDossier(clientId) {
  const content = document.getElementById('dossierContent');
  content.innerHTML = `
    <div class="text-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>
  `;

  dossierModal.show();

  try {
    const response = await fetch(`${API_BASE}/clients/${clientId}/dossier`);
    if (!response.ok) throw new Error('Failed to load dossier');

    const dossier = await response.json();
    displayDossier(dossier);
  } catch (error) {
    console.error(error);
    content.innerHTML = `
      <div class="alert alert-danger">
        Failed to load dossier. Please try again.
      </div>
    `;
  }
}

function displayDossier(dossier) {
  const content = document.getElementById('dossierContent');

  const fullName =
    dossier.firstName || dossier.lastName
      ? `${dossier.firstName || ''} ${dossier.lastName || ''}`.trim()
      : 'No name provided';

  let html = `
    <div class="row mb-4">
      <div class="col-md-6">
        <h5>${fullName}</h5>
        <p class="text-secondary mb-1">${dossier.clientEmail}</p>
        <p class="mb-0">
          ${getStatusBadge(dossier.status)}
          <span class="badge ${getScoreColor(dossier.overallScore)} ms-2">
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
  if (dossier.redFlags.length > 0) {
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
          <small class="text-secondary">(Confidence: ${(
            flag.confidenceScore * 100
          ).toFixed(0)}%)</small>
        </li>
      `;
    });
    html += '</ul></div>';
  }

  // Dossier Entries by Category
  html += '<h6 class="mt-4 mb-3">Dossier Information</h6>';

  const categories = {};
  dossier.entries.forEach((entry) => {
    if (!categories[entry.category]) {
      categories[entry.category] = [];
    }
    categories[entry.category].push(entry);
  });

  if (Object.keys(categories).length === 0) {
    html += '<p class="text-secondary">No information extracted yet.</p>';
  } else {
    html += '<div class="accordion" id="dossierAccordion">';

    Object.entries(categories).forEach(([category, entries], index) => {
      const categoryId = `category-${index}`;
      html += `
        <div class="accordion-item">
          <h2 class="accordion-header" id="heading-${categoryId}">
            <button
              class="accordion-button ${index !== 0 ? 'collapsed' : ''}"
              type="button"
              data-bs-toggle="collapse"
              data-bs-target="#collapse-${categoryId}"
              aria-expanded="${index === 0 ? 'true' : 'false'}"
              aria-controls="collapse-${categoryId}"
            >
              ${formatCategory(category)} (${entries.length})
            </button>
          </h2>
          <div
            id="collapse-${categoryId}"
            class="accordion-collapse collapse ${index === 0 ? 'show' : ''}"
            aria-labelledby="heading-${categoryId}"
            data-bs-parent="#dossierAccordion"
          >
            <div class="accordion-body">
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
            <td><strong>${entry.keyName}</strong></td>
            <td>${entry.value}</td>
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

function formatCategory(category) {
  return category
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (str) => str.toUpperCase())
    .trim();
}

async function evaluateClient(clientId) {
  if (!confirm('Re-evaluate this client? This may take a moment.')) {
    return;
  }

  try {
    const response = await fetch(`${API_BASE}/clients/${clientId}/evaluate`, {
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

