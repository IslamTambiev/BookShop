using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BookShopUI.Repositories
{
    public class HomeRepository: IHomeRepository
	{
		private readonly ApplicationDbContext _db;

		public HomeRepository(ApplicationDbContext db)
        {
			_db = db;
		}
        public async Task<IEnumerable<Book>> GetBooks(string sTerm="", int genreId = 0)
        {
            sTerm = sTerm.ToLower();
			IEnumerable <Book> books = await (from book in _db.Books
                         join genre in _db.Genres
                         on book.GenreId equals genre.Id
                         where string.IsNullOrWhiteSpace(sTerm) || (book != null && book.BookName.ToLower().StartsWith(sTerm))
						 select new Book
                         {
                             Id = book.Id,
                             Image = book.Image,
                             AuthorName = book.AuthorName,
                             BookName = book.BookName,
                             GenreId = book.GenreId,
                             Price = book.Price,
                             GenreName = genre.GenreName
                         }).ToListAsync();
            if (genreId > 0)
            {
				books = books.Where(x => x.GenreId == genreId).ToList();
			}
			return books;
		}
    }
}
