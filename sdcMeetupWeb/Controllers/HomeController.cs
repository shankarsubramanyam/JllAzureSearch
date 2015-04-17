using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace sdcMeetupWeb.Controllers
{

    public class HomeController : Controller
    {
        private CatalogSearch _catalogSearch = new CatalogSearch();
        public ActionResult Index()
        {
            if (CatalogSearch.errorMessage != null)
                ViewBag.errorMessage = "Please ensure that you have added your SearchServiceName and SearchServiceApiKey to the Web.config. Error: " + CatalogSearch.errorMessage;

            return View(); ;
        }

        [HttpGet]
        public ActionResult Search(string q = "", string directors = null, string genres = null, int? yearFrom = null, int? yearTo = null, double? ratingFrom = null, double? ratingTo = null, string sort = null)
        {
            dynamic result = null;

            if (string.IsNullOrWhiteSpace(q))
                q = "*";

            result = _catalogSearch.Search(q, sort, directors, genres, yearFrom, yearTo, ratingFrom, ratingTo);
            ViewBag.searchString = q;
            ViewBag.genres = genres;
            ViewBag.directors = directors;
            ViewBag.yearFrom = yearFrom;
            ViewBag.yearTo = yearTo;
            ViewBag.priceFrom = ratingFrom;
            ViewBag.priceTo = ratingTo;
            ViewBag.sort = sort;

            return View("Index", result);
        }

        [HttpGet]
        public ActionResult Suggest(string term)
        {
            var options = new List<string>();
            if (term.Length >= 3)
            {
                var result = _catalogSearch.Suggest(term);

                foreach (var option in result.value)
                {
                    options.Add((string)option["@search.text"] + " (" + (string)option["Title"] + ")");
                }
            }

            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = options
            };
        }
    }

}