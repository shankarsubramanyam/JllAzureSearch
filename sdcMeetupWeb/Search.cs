using CatalogCommon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace sdcMeetupWeb
{

    public class CatalogSearch
    {
        private static readonly Uri _serviceUri;
        private static HttpClient _httpClient;
        public static string errorMessage;

        static CatalogSearch()
        {
            try
            {
                _serviceUri = new Uri("https://" + ConfigurationManager.AppSettings["SearchServiceName"] + ".search.windows.net");
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("api-key", ConfigurationManager.AppSettings["SearchServiceApiKey"]);
            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }

        public dynamic Search(string searchText, string sort, string directors, string genres, int? yearFrom, int? yearTo, double? ratingFrom, double? ratingTo)
        {
            string search = "&search=" + Uri.EscapeDataString(searchText);
            string facets = "&facet=Directors&facet=Genres&facet=Year,values:1970|1980|1990|2000|2010|2020&facet=Rating,values:2|4|6|8";
            string paging = "&$top=50";
            string filter = BuildFilter(directors, genres, yearFrom, yearTo, ratingFrom, ratingTo);
            string orderby = BuildSort(sort);

            Uri uri = new Uri(_serviceUri, "/indexes/movies/docs?$count=true" + search + facets + paging + filter + orderby);
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Get, uri);
            AzureSearchHelper.EnsureSuccessfulSearchResponse(response);

            return AzureSearchHelper.DeserializeJson<dynamic>(response.Content.ReadAsStringAsync().Result);
        }

        public dynamic Suggest(string searchText)
        {
            // we still need a default filter to exclude discontinued products from the suggestions
            Uri uri = new Uri(_serviceUri, "/indexes/movies/docs/suggest?$filter=Title ne null&$select=Title&suggesterName=Sug&search=" + Uri.EscapeDataString(searchText));
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Get, uri);
            AzureSearchHelper.EnsureSuccessfulSearchResponse(response);

            return AzureSearchHelper.DeserializeJson<dynamic>(response.Content.ReadAsStringAsync().Result);
        }

        private string BuildSort(string sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                return string.Empty;
            }

            if (sort == "Rating" || sort == "Year")
            {
                return "&$orderby=" + sort;
            }

            throw new Exception("Invalid sort order");
        }

        private string BuildFilter(string directors, string genres, int? yearFrom, int? yearTo, double? ratingFrom, double? ratingTo)
        {
            string filter = "&$filter= Title ne null ";

            if (!string.IsNullOrWhiteSpace(directors))
            {
                filter += " and Directors eq '" + EscapeODataString(directors) + "'";
            }

            if (!string.IsNullOrWhiteSpace(genres))
            {
                filter += " and Genres eq '" + EscapeODataString(genres) + "'";
            }

            if (yearFrom.HasValue)
            {
                filter += " and Year ge " + yearFrom.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (yearTo.HasValue && yearTo > 0)
            {
                filter += " and Year le " + yearTo.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (ratingFrom.HasValue)
            {
                filter += " and Rating ge " + ratingFrom.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (ratingTo.HasValue && ratingTo > 0)
            {
                filter += " and Rating le " + ratingTo.Value.ToString(CultureInfo.InvariantCulture);
            }

            return filter;
        }

        private string EscapeODataString(string s)
        {
            return Uri.EscapeDataString(s).Replace("\'", "\'\'");
        }
    }
 
}