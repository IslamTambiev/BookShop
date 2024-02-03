﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookShopUI.Repositories
{
    public class CartRepository: ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpcontextAccessor;

        public CartRepository(ApplicationDbContext db, IHttpContextAccessor httpcontextAccessor, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _httpcontextAccessor = httpcontextAccessor;
            _userManager = userManager;
        }
        public async Task<int> AddItem(int bookId, int qty)
        {
            string userId = GetUserId();
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("user is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                {
                    cart = new ShoppingCart { UserId = userId };
                    _db.ShoppingCarts.Add(cart);
                }
                _db.SaveChanges();
                // cart detail section
                var cartItem = await _db.CartDetails
                    .FirstOrDefaultAsync(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem is not null)
                    cartItem.Quantity += qty;
                else
                {
                    var book = _db.Books.Find(bookId);
                    cartItem = new CartDetail
                    {
                        BookId = bookId,
                        Quantity = qty,
                        ShoppingCartId = cart.Id,
                        UnitPrice = book.Price
                    };
                    _db.CartDetails.Add(cartItem);
                }
                _db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }
        public async Task<int> RemoveItem(int bookId)
        {
            // using var transaction = await _db.Database.BeginTransactionAsync();
            string userId = GetUserId();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("user is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                    throw new Exception("Invalid cart");
                var cartItem = await _db.CartDetails
                    .FirstOrDefaultAsync(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem is null)
                    throw new Exception("No items in cart");
                else if(cartItem.Quantity==1)
                    _db.CartDetails.Remove(cartItem);
                else
                    cartItem.Quantity -= 1;
                await _db.SaveChangesAsync();
                // await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }
        public async Task<ShoppingCart> GetUserCart()
        {
            string userId = GetUserId();
            if (userId == null)
                throw new Exception("Invalid userId");
            var shoppingCart = await _db.ShoppingCarts
                .Include(x => x.CartDetails)
                .ThenInclude(x => x.Book)
                .ThenInclude(x => x.Genre)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();
            return shoppingCart;
        }
        public async Task<ShoppingCart> GetCart(string userId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart;
        }
        public async Task<int> GetCartItemCount(string userId="")
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = GetUserId();
            }
            var data = await (from cart in _db.ShoppingCarts
                              join CartDetail in _db.CartDetails
                              on cart.Id equals CartDetail.ShoppingCartId
                              where cart.UserId == userId
                              select new { CartDetail.Id })
                              .ToListAsync();
            return data.Count;
        }
        public async Task<bool> DoCheckout()
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("user is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                    throw new Exception("Invalid cart");
                var cartDetails = _db.CartDetails
                    .Where(a => a.ShoppingCartId == cart.Id)
                    .ToList();
                if (cartDetails.Count == 0)
                    throw new Exception("No items in cart");
                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    OrderStatusId = 1
                };
                _db.Orders.Add(order);
                _db.SaveChanges();
                foreach (var item in cartDetails)
                {
                    var orderDetail = new OrderDetail
                    {
                        BookId = item.BookId,
                        OrderId = order.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _db.OrderDetails.Add(orderDetail);
                }
                _db.SaveChanges();

                // removing cartdetails
                _db.CartDetails.RemoveRange(cartDetails);
                _db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private string GetUserId()
        {
            var principal = _httpcontextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
    }
}
