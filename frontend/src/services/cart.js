import { reactive, computed } from 'vue'

const savedCart = localStorage.getItem('online_cart')
const items = savedCart ? JSON.parse(savedCart) : []

export const cartState = reactive({
  items
})

export const cartService = {
  items: computed(() => cartState.items),
  
  count: computed(() => {
    return cartState.items.reduce((sum, item) => sum + item.quantity, 0)
  }),

  total: computed(() => {
    return cartState.items.reduce((sum, item) => sum + (item.price * item.quantity), 0)
  }),

  addToCart(product, qty = 1) {
    if (product.stock <= 0) {
      alert('Sản phẩm đã hết hàng!')
      return
    }
    const existing = cartState.items.find(i => i.productId === product.id)
    const isDiscounted = !!(product.discountExpiry && new Date(product.discountExpiry) > new Date())
    if (existing) {
      if (existing.quantity + qty > product.stock) {
        alert('Số lượng trong giỏ vượt quá tồn kho khả dụng!')
        return
      }
      existing.quantity += qty
    } else {
      cartState.items.push({
        productId: product.id,
        name: product.name,
        sku: product.sku,
        image: product.image,
        price: product.price,
        originalPrice: isDiscounted ? product.originalPrice : product.price,
        isDiscounted: isDiscounted,
        quantity: qty,
        stock: product.stock
      })
    }
    this.save()
  },

  removeFromCart(productId) {
    cartState.items = cartState.items.filter(i => i.productId !== productId)
    this.save()
  },

  updateQuantity(productId, qty) {
    const item = cartState.items.find(i => i.productId === productId)
    if (item) {
      item.quantity = Math.max(1, Math.min(item.stock, qty))
      this.save()
    }
  },

  clearCart() {
    cartState.items = []
    this.save()
  },

  save() {
    localStorage.setItem('online_cart', JSON.stringify(cartState.items))
  }
}
