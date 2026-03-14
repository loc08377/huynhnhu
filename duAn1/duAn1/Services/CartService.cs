using duAn1.Models;
using duAn1.Repository;

public class CartService
{
    public readonly CartLINQ _cartLINQ;

    public CartService(CartLINQ cartLINQ)
    {
        _cartLINQ = cartLINQ;
    }

    public bool RemoveCartItem(int cartId)
    {
        try
        {
            return _cartLINQ.RemoveCartItem(cartId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return false;
        }
    }

    public bool InsertCart(Cart cart)
    {
        try
        {
            _cartLINQ.InsertCart(cart);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return false;
        }
    }

    public Cart CheckAddProductInCart(int productId, int? userId)
    {
        try
        {
            return _cartLINQ.GetCartByProductIdAndUserId(productId, userId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return new Cart();
        }
    }
    public int GetCartCount(int userId)
    {
        try
        {
            return _cartLINQ.GetCartCount(userId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return 0;
        }
    }

    public List<Cart> GetCartsByUserId(int? userId)
    {
        try
        {
            return _cartLINQ.GetCartsByUserId(userId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return new List<Cart>();
        }
    }

    public bool UpdateQuantity(int cartId, int quantity)
    {
        try
        {
            return _cartLINQ.UpdateQuantity(cartId, quantity);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return false;
        }
    }
}