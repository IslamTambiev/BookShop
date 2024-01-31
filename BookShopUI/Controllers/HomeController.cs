using BookShopUI.Models;
using BookShopUI.Models.DIOs;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BookShopUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeRepository _homeRepository;

        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepository)
        {
			_homeRepository = homeRepository;
			_logger = logger;
        }

        public async Task<IActionResult> Index(string sterm="", int genreId=0)
        {
            IEnumerable<Book> books = await _homeRepository.GetBooks(sterm, genreId);
			IEnumerable<Genre> genres = await _homeRepository.GetGenres();
            BookDisplayModel bookModel = new BookDisplayModel
            {
                Books = books,
                Genres = genres,
                STerm = sterm,
                GenreId = genreId
            };
            return View(bookModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
