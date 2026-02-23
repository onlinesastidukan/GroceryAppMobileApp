using System;
using System.Collections.ObjectModel;
using System.Linq;
using GroceryApp.Models;

namespace GroceryApp.Services;

public class CartService
{
    private ObservableCollection<CartItem> _cartItems;
    
    public ObservableCollection<CartItem> CartItems
    {
        get => _cartItems ??= new ObservableCollection<CartItem>();
    }

    public decimal TotalPrice => CartItems.Sum(x => x.TotalPrice);
    
    public int TotalItems => CartItems.Sum(x => x.Quantity);

    public void AddToCart(Product product, int quantity = 1)
    {
        var existingItem = CartItems.FirstOrDefault(x => x.ProductId == product.ProductId);
        
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            CartItems.Add(new CartItem
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            });
        }
    }

    public void RemoveFromCart(int productId)
    {
        var item = CartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            CartItems.Remove(item);
        }
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var item = CartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            if (quantity <= 0)
            {
                RemoveFromCart(productId);
            }
            else
            {
                item.Quantity = quantity;
            }
        }
    }

    public void ClearCart()
    {
        CartItems.Clear();
    }

    public List<CartItem> GetCartItems()
    {
        return CartItems.ToList();
    }

    public Cart GetCart()
    {
        return new Cart { Items = CartItems.ToList() };
    }
}
