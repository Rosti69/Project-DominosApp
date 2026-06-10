/* ============================================================
   DominosApp — app.js
   Handles all API calls, page routing, auth, and UI state
   ============================================================ */

// ===== CONFIG =====
const API = 'http://localhost:5000/api';

// ===== STATE =====
let token     = localStorage.getItem('dominosToken');
let isAdmin   = false;
let userName  = '';
let selPizza  = null;   // currently selected pizza for ordering
let qty       = 1;
let allPizzas = [];     // cached pizza list for search filtering

// ===== BOOT =====
(function init() {
  if (token) {
    const p = parseJwt(token);
    if (p && p.exp * 1000 > Date.now()) {
      applyToken(token, p);
    } else {
      clearAuth();
    }
  }
  updateNav();
  showPage('home');
})();

// ===== PAGE ROUTING =====
function showPage(name) {
  document.querySelectorAll('.page').forEach(el => el.classList.remove('active'));
  const pg = document.getElementById('page-' + name);
  if (!pg) return;
  pg.classList.add('active');
  window.scrollTo(0, 0);

  if (name === 'menu')     loadPizzas();
  if (name === 'myorders') loadMyOrders();
  if (name === 'admin')    loadAdminPage();
}

// ===== NAVBAR =====
function updateNav() {
  const loggedIn = !!token;
  document.getElementById('nav-guest').style.display = loggedIn ? 'none' : 'flex';
  document.getElementById('nav-auth').style.display  = loggedIn ? 'flex' : 'none';
  document.getElementById('nav-admin-link').style.display = isAdmin ? 'inline' : 'none';
  if (loggedIn) {
    document.getElementById('nav-username').textContent = '👤 ' + userName;
  }
}

// ===== AUTH HELPERS =====
function parseJwt(t) {
  try { return JSON.parse(atob(t.split('.')[1])); }
  catch { return null; }
}

function applyToken(t, payload) {
  token = t;
  const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  isAdmin = role === 'Administrator' || (Array.isArray(role) && role.includes('Administrator'));
  userName = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
          || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
          || 'User';
}

function clearAuth() {
  token = null; isAdmin = false; userName = '';
  localStorage.removeItem('dominosToken');
}

function logout() {
  clearAuth();
  updateNav();
  showPage('home');
  toast('Logged out successfully');
}

// ===== API FETCH WRAPPER =====
async function api(path, opts = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = 'Bearer ' + token;
  return fetch(API + path, { ...opts, headers: { ...headers, ...opts.headers } });
}

// ===== TOAST =====
let toastTimer;
function toast(msg, type = '') {
  const el = document.getElementById('toast');
  el.textContent = msg;
  el.className = 'toast show ' + type;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.className = 'toast', 3200);
}

// ===== PIZZA — MENU =====
async function loadPizzas() {
  const grid = document.getElementById('pizzaGrid');
  grid.innerHTML = '<div class="loading"><div class="spin"></div><br/>Loading menu…</div>';
  try {
    const res = await api('/pizza');
    allPizzas = await res.json();
    renderPizzas(allPizzas);
  } catch {
    grid.innerHTML = '<div class="empty"><div class="emo">⚠️</div><p>Could not reach the API.<br/>Make sure the server is running.</p></div>';
  }
}

function renderPizzas(list) {
  const grid = document.getElementById('pizzaGrid');
  if (!list.length) {
    grid.innerHTML = '<div class="empty"><div class="emo">🍕</div><p>No pizzas found.</p></div>';
    return;
  }
  grid.innerHTML = list.map(p => {
    const imgHtml = p.imageUrl
      ? `<img src="${p.imageUrl}" alt="${p.name}" loading="lazy" onerror="this.parentElement.innerHTML='<div class=pizza-card-img-placeholder>🍕</div>'"/>`
      : `<div class="pizza-card-img-placeholder">🍕</div>`;
    return `
      <div class="pizza-card" onclick="openModal('${p.id}','${esc(p.name)}','${esc(p.description||'')}',${p.price},'${esc(p.imageUrl||'')}')">
        ${imgHtml}
        <div class="pizza-body">
          <div class="pizza-name">${p.name}</div>
          <p class="pizza-desc">${p.description || 'Delicious pizza'}</p>
          <div class="pizza-footer">
            <span class="pizza-price">$${p.price.toFixed(2)}</span>
            <button class="btn-sm-red">Order</button>
          </div>
        </div>
      </div>`;
  }).join('');
}

// Escape string for inline onclick attribute
function esc(s) { return String(s).replace(/'/g,"&#39;").replace(/"/g,"&quot;"); }

function filterPizzas() {
  const q = document.getElementById('searchInput').value.toLowerCase();
  renderPizzas(allPizzas.filter(p =>
    p.name.toLowerCase().includes(q) || (p.description||'').toLowerCase().includes(q)
  ));
}

// ===== ORDER MODAL =====
function openModal(id, name, desc, price, imgUrl) {
  if (!token) { showPage('login'); toast('Please login to order 🍕', 'err'); return; }
  selPizza = { id, name, desc, price, imgUrl };
  qty = 1;

  document.getElementById('modalName').textContent = name;
  document.getElementById('modalDesc').textContent = desc || '';
  document.getElementById('modalUnitPrice').textContent = `$${price.toFixed(2)} each`;
  document.getElementById('qtyVal').textContent = 1;
  document.getElementById('modalTotal').textContent = `$${price.toFixed(2)}`;

  // image handling
  const imgEl = document.getElementById('modalImg');
  const emoEl = document.getElementById('modalEmoji');
  if (imgUrl) {
    imgEl.src = imgUrl; imgEl.style.display = 'block'; emoEl.style.display = 'none';
  } else {
    imgEl.style.display = 'none'; emoEl.style.display = 'flex';
  }

  document.getElementById('modal').classList.add('open');
}

function changeQty(d) {
  qty = Math.min(10, Math.max(1, qty + d));
  document.getElementById('qtyVal').textContent = qty;
  document.getElementById('modalTotal').textContent = `$${(selPizza.price * qty).toFixed(2)}`;
}

function closeModal() { document.getElementById('modal').classList.remove('open'); }
function bgClose(e)   { if (e.target === e.currentTarget) closeModal(); }

async function placeOrder() {
  if (!selPizza) return;
  try {
    const res = await api('/order', {
      method: 'POST',
      body: JSON.stringify({ items: [{ pizzaId: selPizza.id, quantity: qty }] })
    });
    closeModal();
    if (res.ok) {
      toast(`Order placed! 🍕 Total: $${(selPizza.price * qty).toFixed(2)}`, 'ok');
    } else {
      const e = await res.json().catch(() => ({}));
      toast(e.message || 'Order failed', 'err');
    }
  } catch { toast('Could not reach server', 'err'); }
}

// ===== MY ORDERS =====
async function loadMyOrders() {
  const el = document.getElementById('orderList');
  el.innerHTML = '<div class="loading"><div class="spin"></div><br/>Loading orders…</div>';
  if (!token) { showPage('login'); return; }
  try {
    const res = await api('/order/my');
    if (res.status === 401) { clearAuth(); updateNav(); showPage('login'); return; }
    const orders = await res.json();
    if (!orders.length) {
      el.innerHTML = `<div class="empty"><div class="emo">📭</div><p>No orders yet.</p><br/><button class="btn-cta" onclick="showPage('menu')">Order Now</button></div>`;
      return;
    }
    el.innerHTML = [...orders].reverse().map(o => `
      <div class="order-card">
        <div>
          <div class="order-id">Order #${o.id.substring(0,8).toUpperCase()}</div>
          <div class="order-date">${new Date(o.orderedAt).toLocaleString()}</div>
        </div>
        <div class="order-price">$${o.totalPrice.toFixed(2)}</div>
        <div class="order-items">🍕 ${o.pizzas.map(p => `${p.pizzaName} ×${p.quantity}`).join(' · ')}</div>
      </div>`).join('');
  } catch { el.innerHTML = '<div class="empty"><p>Failed to load orders.</p></div>'; }
}

// ===== LOGIN =====
async function doLogin() {
  const email    = document.getElementById('loginEmail').value.trim();
  const password = document.getElementById('loginPassword').value;
  document.getElementById('loginError').textContent = '';
  try {
    const res = await api('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) });
    if (res.ok) {
      const data = await res.json();
      localStorage.setItem('dominosToken', data.token);
      const p = parseJwt(data.token);
      applyToken(data.token, p);
      updateNav();
      showPage('menu');
      toast('Welcome back! 🍕', 'ok');
    } else {
      document.getElementById('loginError').textContent = 'Invalid email or password.';
    }
  } catch { document.getElementById('loginError').textContent = 'Could not connect to server.'; }
}

// ===== REGISTER =====
async function doRegister() {
  document.getElementById('registerError').textContent = '';
  document.getElementById('registerSuccess').textContent = '';
  const body = {
    fullName:    document.getElementById('regFullName').value.trim(),
    email:       document.getElementById('regEmail').value.trim(),
    password:    document.getElementById('regPassword').value,
    phoneNumber: document.getElementById('regPhone').value.trim(),
    address:     document.getElementById('regAddress').value.trim()
  };
  try {
    const res = await api('/auth/register', { method: 'POST', body: JSON.stringify(body) });
    if (res.ok) {
      document.getElementById('registerSuccess').textContent = '✓ Account created! You can now login.';
      toast('Account created!', 'ok');
    } else {
      const err = await res.json().catch(() => ({}));
      const msg = Array.isArray(err)
        ? err.map(e => e.description).join(' ')
        : err.message || 'Registration failed.';
      document.getElementById('registerError').textContent = msg;
    }
  } catch { document.getElementById('registerError').textContent = 'Could not connect.'; }
}

// ===== ADMIN =====
function loadAdminPage() {
  loadAdminUsers();
  loadAdminOrders();
  loadManagePizzas();
}

function switchTab(btn, id) {
  document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
  document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
  btn.classList.add('active');
  document.getElementById(id).classList.add('active');
}

// Users table
async function loadAdminUsers() {
  const el = document.getElementById('userList');
  el.innerHTML = '<div class="loading"><div class="spin"></div></div>';
  try {
    const res = await api('/admin/users');
    const users = await res.json();
    el.innerHTML = `
      <table class="admin-table">
        <thead><tr><th>Email</th><th>Full Name</th><th>Address</th><th>Phone</th><th>Action</th></tr></thead>
        <tbody>${users.map(u => `
          <tr>
            <td>${u.email}</td>
            <td>${u.fullName}</td>
            <td>${u.address || '—'}</td>
            <td>${u.phoneNumber || '—'}</td>
            <td><button class="btn-sm-del" onclick="deleteUser('${u.id}')">Delete</button></td>
          </tr>`).join('')}
        </tbody>
      </table>`;
  } catch { el.innerHTML = '<p>Failed to load users.</p>'; }
}

async function deleteUser(id) {
  if (!confirm('Delete this user permanently?')) return;
  const res = await api('/admin/users/' + id, { method: 'DELETE' });
  if (res.ok) { toast('User deleted', 'ok'); loadAdminUsers(); }
  else toast('Could not delete user', 'err');
}

// All orders table (admin)
async function loadAdminOrders() {
  const el = document.getElementById('allOrdersList');
  el.innerHTML = '<div class="loading"><div class="spin"></div></div>';
  try {
    const res = await api('/order');
    const orders = await res.json();
    if (!orders.length) { el.innerHTML = '<div class="empty"><p>No orders yet.</p></div>'; return; }
    el.innerHTML = `
      <table class="admin-table">
        <thead><tr><th>Order ID</th><th>User</th><th>Pizzas</th><th>Total</th><th>Date</th><th>Action</th></tr></thead>
        <tbody>${[...orders].reverse().map(o => `
          <tr>
            <td>#${o.id.substring(0,8).toUpperCase()}</td>
            <td>${o.userName}</td>
            <td>${o.pizzas.map(p => p.pizzaName + ' ×' + p.quantity).join(', ')}</td>
            <td><strong>$${o.totalPrice.toFixed(2)}</strong></td>
            <td>${new Date(o.orderedAt).toLocaleDateString()}</td>
            <td><button class="btn-sm-del" onclick="deleteOrder('${o.id}')">Delete</button></td>
          </tr>`).join('')}
        </tbody>
      </table>`;
  } catch { el.innerHTML = '<p>Failed to load orders.</p>'; }
}

async function deleteOrder(id) {
  if (!confirm('Delete this order?')) return;
  const res = await api('/order/' + id, { method: 'DELETE' });
  if (res.ok) { toast('Order deleted', 'ok'); loadAdminOrders(); }
  else toast('Could not delete order', 'err');
}

// Manage pizzas (edit/delete)
async function loadManagePizzas() {
  const el = document.getElementById('managePizzaList');
  el.innerHTML = '<div class="loading"><div class="spin"></div></div>';
  try {
    const res = await api('/pizza');
    const pizzas = await res.json();
    el.innerHTML = `
      <table class="admin-table">
        <thead><tr><th>Name</th><th>Price</th><th>Description</th><th>Available</th><th>Actions</th></tr></thead>
        <tbody>${pizzas.map(p => `
          <tr>
            <td><strong>${p.name}</strong></td>
            <td>$${p.price.toFixed(2)}</td>
            <td>${p.description || '—'}</td>
            <td>${p.isAvailable ? '✅' : '❌'}</td>
            <td style="display:flex;gap:0.5rem">
              <button class="btn-sm-red" onclick="deletePizza('${p.id}')">Delete</button>
            </td>
          </tr>`).join('')}
        </tbody>
      </table>`;
  } catch { el.innerHTML = '<p>Failed to load pizzas.</p>'; }
}

async function deletePizza(id) {
  if (!confirm('Remove this pizza from the menu?')) return;
  const res = await api('/pizza/' + id, { method: 'DELETE' });
  if (res.ok) { toast('Pizza removed', 'ok'); loadManagePizzas(); loadPizzas(); }
  else toast('Could not delete pizza', 'err');
}

async function addPizza() {
  document.getElementById('addMsg').textContent = '';
  document.getElementById('addErr').textContent = '';
  const body = {
    name:        document.getElementById('newName').value.trim(),
    price:       parseFloat(document.getElementById('newPrice').value) || 0,
    description: document.getElementById('newDesc').value.trim(),
    imageUrl:    document.getElementById('newImg').value.trim(),
    isAvailable: true
  };
  if (!body.name || body.price <= 0) {
    document.getElementById('addErr').textContent = 'Name and a valid price are required.';
    return;
  }
  const res = await api('/pizza', { method: 'POST', body: JSON.stringify(body) });
  if (res.ok) {
    document.getElementById('addMsg').textContent = '✓ Pizza added to menu!';
    document.getElementById('newName').value = '';
    document.getElementById('newPrice').value = '';
    document.getElementById('newDesc').value = '';
    document.getElementById('newImg').value = '';
    toast('Pizza added! 🍕', 'ok');
    loadManagePizzas();
  } else {
    document.getElementById('addErr').textContent = 'Failed to add pizza. Check all fields.';
  }
}
