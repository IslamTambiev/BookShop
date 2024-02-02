using Microsoft.AspNetCore.Identity;
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
                    cartItem = new CartDetail
                    {
                        BookId = bookId,
                        Quantity = qty,
                        ShoppingCartId = cart.Id
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
        private string GetUserId()
        {
            var principal = _httpcontextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
    }
}
