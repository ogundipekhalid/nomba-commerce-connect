const state = {
  products: [],
  cart: new Map(), // productId -> { product, quantity }
};

const productsEl = document.getElementById("products");
const cartItemsEl = document.getElementById("cart-items");
const cartTotalEl = document.getElementById("cart-total");
const checkoutBtn = document.getElementById("checkout-btn");
const checkoutForm = document.getElementById("checkout-form");
const checkoutResultEl = document.getElementById("checkout-result");
const orderDetailsEl = document.getElementById("order-details");

async function loadProducts() {
  const res = await fetch("/api/products");
  if (!res.ok) {
    productsEl.textContent = "Could not load products.";
    return;
  }
  state.products = await res.json();
  renderProducts();
}

function renderProducts() {
  productsEl.innerHTML = "";
  if (state.products.length === 0) {
    productsEl.innerHTML = "<p>No products yet. Add one via POST /api/products (see README/Swagger).</p>";
    return;
  }

  for (const product of state.products) {
    const card = document.createElement("div");
    card.className = "product-card";
    card.innerHTML = `
      <img src="${product.imageUrl || "https://placehold.co/300x200?text=" + encodeURIComponent(product.name)}" alt="${product.name}" />
      <strong>${product.name}</strong>
      <span class="vendor">Sold by ${product.vendorName || "Unknown vendor"}</span>
      <span class="price">₦${Number(product.price).toLocaleString()}</span>
      <span>${product.stockQuantity} in stock</span>
      <button data-id="${product.id}">Add to cart</button>
    `;
    card.querySelector("button").addEventListener("click", () => addToCart(product));
    productsEl.appendChild(card);
  }
}

function addToCart(product) {
  const existing = state.cart.get(product.id);
  if (existing) {
    existing.quantity += 1;
  } else {
    state.cart.set(product.id, { product, quantity: 1 });
  }
  renderCart();
}

function removeFromCart(productId) {
  state.cart.delete(productId);
  renderCart();
}

function renderCart() {
  cartItemsEl.innerHTML = "";
  let total = 0;

  for (const { product, quantity } of state.cart.values()) {
    const lineTotal = product.price * quantity;
    total += lineTotal;

    const line = document.createElement("div");
    line.className = "cart-line";
    line.innerHTML = `
      <span>${product.name} × ${quantity}</span>
      <span>₦${lineTotal.toLocaleString()} <button data-id="${product.id}">✕</button></span>
    `;
    line.querySelector("button").addEventListener("click", () => removeFromCart(product.id));
    cartItemsEl.appendChild(line);
  }

  cartTotalEl.textContent = state.cart.size > 0 ? `Total: ₦${total.toLocaleString()}` : "Cart is empty.";
  checkoutBtn.disabled = state.cart.size === 0;
}

checkoutForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  checkoutBtn.disabled = true;
  checkoutResultEl.textContent = "Creating Nomba checkout order...";

  const payload = {
    customerEmail: document.getElementById("customerEmail").value,
    customerFullName: document.getElementById("customerName").value,
    callbackUrl: window.location.origin + "/index.html",
    items: Array.from(state.cart.values()).map(({ product, quantity }) => ({
      productId: product.id,
      quantity,
    })),
  };

  try {
    const res = await fetch("/api/orders", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });

    const body = await res.json();

    if (!res.ok) {
      checkoutResultEl.textContent = `Checkout failed: ${body.error || "Unknown error"}`;
      checkoutBtn.disabled = false;
      return;
    }

    checkoutResultEl.innerHTML = `
      Order ${body.orderReference} created (₦${Number(body.totalAmount).toLocaleString()}).<br/>
      <a href="${body.checkoutLink}" target="_blank">Open Nomba checkout link ↗</a><br/>
      Order ID: ${body.orderId}
    `;

    state.cart.clear();
    renderCart();
  } catch (err) {
    checkoutResultEl.textContent = `Network error: ${err.message}`;
    checkoutBtn.disabled = false;
  }
});

document.getElementById("lookup-btn").addEventListener("click", async () => {
  const id = document.getElementById("order-lookup").value.trim();
  if (!id) return;

  const res = await fetch(`/api/orders/${id}`);
  if (!res.ok) {
    orderDetailsEl.textContent = "Order not found.";
    return;
  }

  const order = await res.json();
  orderDetailsEl.textContent = JSON.stringify(order, null, 2);
});

loadProducts();
