/* ============================================================
   DominosApp — app.js
   Handles all API calls, page routing, auth, and UI state
   ============================================================ */

// ===== CONFIG - DominosApp v1.0 =====
const API = 'http://localhost:5000/api';

// ===== STATE =====
let token     = localStorage.getItem('dominosToken');
let isAdmin   = false;
let userName  = '';
let allPizzas = [];
let cart      = [];

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
  updateCartBadge();
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
  if (name === 'cart')     renderCart();
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
  cart = [];
  updateCartBadge();
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

// ===== CART =====
function updateCartBadge() {
  const total = cart.reduce((sum, i) => sum + i.quantity, 0);
  const badge = document.getElementById('cartBadge');
  if (badge) {
    badge.textContent = total > 0 ? total : '';
    badge.style.display = total > 0 ? 'inline-flex' : 'none';
  }
}

function addToCart(pizzaId, pizzaName, price, imageUrl) {
  if (!token) { showPage('login'); toast('Please login to order 🍕', 'err'); return; }

  const existing = cart.find(i => i.pizzaId === pizzaId);
  if (existing) {
    if (existing.quantity >= 10) { toast('Max 10 of the same pizza', 'err'); return; }
    existing.quantity++;
  } else {
    cart.push({ pizzaId, pizzaName, price, imageUrl, quantity: 1 });
  }

  updateCartBadge();
  toast(`${pizzaName} added to cart 🍕`, 'ok');
}

function removeFromCart(pizzaId) {
  cart = cart.filter(i => i.pizzaId !== pizzaId);
  updateCartBadge();
  renderCart();
}

function changeCartQty(pizzaId, delta) {
  const item = cart.find(i => i.pizzaId === pizzaId);
  if (!item) return;
  item.quantity = Math.max(1, Math.min(10, item.quantity + delta));
  renderCart();
  updateCartBadge();
}

function renderCart() {
  const el = document.getElementById('cartItems');
  const totalEl = document.getElementById('cartTotal');
  const emptyEl = document.getElementById('cartEmpty');
  const checkoutBtn = document.getElementById('checkoutBtn');

  if (!cart.length) {
    el.innerHTML = '';
    emptyEl.style.display = 'block';
    totalEl.textContent = '$0.00';
    checkoutBtn.style.display = 'none';
    return;
  }

  emptyEl.style.display = 'none';
  checkoutBtn.style.display = 'block';

  const total = cart.reduce((sum, i) => sum + i.price * i.quantity, 0);
  totalEl.textContent = '$' + total.toFixed(2);

  el.innerHTML = cart.map(item => `
    <div class="cart-item">
      <img src="${item.imageUrl || ''}" alt="${item.pizzaName}"
           onerror="this.style.display='none'"
           style="width:70px;height:70px;object-fit:cover;border-radius:10px;"/>
      <div class="cart-item-info">
        <div class="cart-item-name">${item.pizzaName}</div>
        <div class="cart-item-price">$${item.price.toFixed(2)} each</div>
      </div>
      <div class="cart-item-qty">
        <button class="qty-btn" onclick="changeCartQty('${item.pizzaId}', -1)">−</button>
        <span>${item.quantity}</span>
        <button class="qty-btn" onclick="changeCartQty('${item.pizzaId}', 1)">+</button>
      </div>
      <div class="cart-item-subtotal">$${(item.price * item.quantity).toFixed(2)}</div>
      <button class="cart-remove" onclick="removeFromCart('${item.pizzaId}')">✕</button>
    </div>`).join('');
}

async function checkout() {
  if (!cart.length) { toast('Your cart is empty', 'err'); return; }

  const items = cart.map(i => ({ pizzaId: i.pizzaId, quantity: i.quantity }));

  try {
    const res = await api('/order', {
      method: 'POST',
      body: JSON.stringify({ items })
    });

    if (res.ok) {
      cart = [];
      updateCartBadge();
      renderCart();
      toast('Order placed successfully! 🍕', 'ok');
      showPage('myorders');
    } else {
      const err = await res.json().catch(() => ({}));
      toast(err.message || err.error || 'Order failed', 'err');
    }
  } catch { toast('Could not reach server', 'err'); }
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
      <div class="pizza-card">
        ${imgHtml}
        <div class="pizza-body">
          <div class="pizza-name">${p.name}</div>
          <p class="pizza-desc">${p.description || 'Delicious pizza'}</p>
          <div class="pizza-footer">
            <span class="pizza-price">$${p.price.toFixed(2)}</span>
            <button class="btn-sm-red" onclick="addToCart('${p.id}','${esc(p.name)}',${p.price},'${esc(p.imageUrl||'')}')">
              🛒 Add to Cart
            </button>
          </div>
        </div>
      </div>`;
  }).join('');
}

function esc(s) { return String(s).replace(/'/g,"&#39;").replace(/"/g,"&quot;"); }

function filterPizzas() {
  const q = document.getElementById('searchInput').value.toLowerCase();
  renderPizzas(allPizzas.filter(p =>
    p.name.toLowerCase().includes(q) || (p.description||'').toLowerCase().includes(q)
  ));
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
        <div style="display:flex;flex-direction:column;align-items:flex-end;gap:0.3rem">
          <div class="order-price">$${o.totalPrice.toFixed(2)}</div>
          ${getStatusBadge(o.status)}
        </div>
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

async function loadAdminOrders() {
  const el = document.getElementById('allOrdersList');
  el.innerHTML = '<div class="loading"><div class="spin"></div></div>';
  try {
    const res = await api('/order');
    const orders = await res.json();
    if (!orders.length) { el.innerHTML = '<div class="empty"><p>No orders yet.</p></div>'; return; }
    el.innerHTML = `
      <table class="admin-table">
        <thead><tr><th>Order ID</th><th>User</th><th>Pizzas</th><th>Total</th><th>Status</th><th>Date</th><th>Actions</th></tr></thead>
        <tbody>${[...orders].reverse().map(o => `
          <tr>
            <td>#${o.id.substring(0,8).toUpperCase()}</td>
            <td>${o.userName}</td>
            <td>${o.pizzas.map(p => p.pizzaName + ' x' + p.quantity).join(', ')}</td>
            <td><strong>$${o.totalPrice.toFixed(2)}</strong></td>
            <td>${getStatusBadge(o.status)}</td>
            <td>${new Date(o.orderedAt).toLocaleDateString()}</td>
            <td style="display:flex;gap:0.4rem;flex-wrap:wrap">
              <select onchange="updateOrderStatus('${o.id}', this.value)" style="padding:0.3rem;border-radius:6px;border:1px solid #ddd;font-size:0.8rem">
                <option value="0" ${o.status==='Pending'?'selected':''}>Pending</option>
                <option value="1" ${o.status==='Preparing'?'selected':''}>Preparing</option>
                <option value="2" ${o.status==='Delivered'?'selected':''}>Delivered</option>
                <option value="3" ${o.status==='Cancelled'?'selected':''}>Cancelled</option>
              </select>
              <button class="btn-sm-del" onclick="deleteOrder('${o.id}')">Delete</button>
            </td>
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
            <td><button class="btn-sm-red" onclick="deletePizza('${p.id}')">Delete</button></td>
          </tr>`).join('')}
        </tbody>
      </table>`;
  } catch { el.innerHTML = '<p>Failed to load pizzas.</p>'; }
}

async function deletePizza(id) {
  if (!confirm('Remove this pizza from the menu?')) return;
  const res = await api('/pizza/' + id, { method: 'DELETE' });
  if (res.ok) { toast('Pizza removed', 'ok'); loadManagePizzas(); }
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

// ===== ORDER STATUS =====
const statusLabels = {
  'Pending':   '⏳ Pending',
  'Preparing': '👨‍🍳 Preparing',
  'Delivered': '✅ Delivered',
  'Cancelled': '❌ Cancelled'
};

const statusColors = {
  'Pending':   '#f59e0b',
  'Preparing': '#3b82f6',
  'Delivered': '#10b981',
  'Cancelled': '#ef4444'
};

function getStatusBadge(status) {
  const color = statusColors[status] || '#6b7280';
  const label = statusLabels[status] || status;
  return `<span style="background:${color};color:white;padding:0.2rem 0.6rem;border-radius:12px;font-size:0.78rem;font-weight:700">${label}</span>`;
}
async function updateOrderStatus(orderId, newStatus) {
  const res = await api('/order/' + orderId + '/status', {
    method: 'PUT',
    body: JSON.stringify({ status: parseInt(newStatus) })
  });
  if (res.ok) {
    toast('Order status updated! ✓', 'ok');
    await loadAdminOrders();
  } else {
    const err = await res.json().catch(() => ({}));
    const msg = err.error || err.message || 'Could not update status';
    toast(msg, 'err');
    console.error('Status update failed:', err);
  }
}

function setToken(t) {
  const p = parseJwt(t);
  if (p) applyToken(t, p);
}