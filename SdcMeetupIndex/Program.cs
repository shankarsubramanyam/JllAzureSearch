using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SdcMeetupIndex
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                SearchServiceClient serviceClient = new
                SearchServiceClient(ConfigurationManager.AppSettings["SearchServiceName"],
                new SearchCredentials(ConfigurationManager.AppSettings["SearchServiceApiKey"]));


                DeleteMoviesIndexIfExists(serviceClient);
                CreateMovieIndex(serviceClient);

                SearchIndexClient indexClient = serviceClient.Indexes.GetClient("movies");

                UploadDocuments(indexClient);

                Console.WriteLine("{0}", "Index completed...\n");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void DeleteMoviesIndexIfExists(SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists("movies"))
            {
                Console.WriteLine("{0}", "Deleting index...\n");
                serviceClient.Indexes.Delete("movies");
                Console.WriteLine("{0}", "movies index deleted\n");
            }
        }


        private static void CreateMovieIndex(SearchServiceClient serviceClient)
        {
            Console.WriteLine("{0}", "Creating movies index...\n");
            var definition = new Index()
            {
                Name = "movies",
                Fields = new[] 
                { 
                    new Field("ID",DataType.String) { IsKey = true,  IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false,IsRetrievable = true},
                    new Field("Title",DataType.String) { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("Year",DataType.Int32) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true },
                    new Field("Rating",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("NumVotes",DataType.Int32) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("imdbID",DataType.String) { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("Type",DataType.String) { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("Released",DataType.DateTimeOffset) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("Runtime",DataType.Int32) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("Genres",DataType.String) { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("Directors",DataType.String) { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("Metascore",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true },
                    new Field("imdbRating",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true },
                    new Field("imdbVotes",DataType.String) { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("tomatoMeter",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("tomatoRating",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("tomatoRotten",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("tomatoUserMeter",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("tomatoUserRating",DataType.Double) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("poster_path",DataType.String) { IsKey = false, IsSearchable = false, IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    new Field("release_date",DataType.DateTimeOffset) { IsKey = false, IsSearchable = false,  IsFilterable = true,  IsSortable = true,  IsFacetable = true, IsRetrievable = true},
                    
                },
                Suggesters = new[] 
                { 
                    new Suggester(){ Name ="Sug",SearchMode=SuggesterSearchMode.AnalyzingInfixMatching, SourceFields = new [] { "Genres" , "Directors","Title" } }
                }
            };

            serviceClient.Indexes.Create(definition);
            Console.WriteLine("{0}", "movies index created\n");
        }

        private static void UploadDocuments(SearchIndexClient indexClient)
        {
            Console.WriteLine("{0}", "Uploading movies documents...\n");
            try
            {
                string query = @"SELECT top 9500 ID,Title,Year,Rating,NumVotes,imdbID,Type,Released,Runtime,Genres,Directors,Metascore,imdbRating,imdbVotes,tomatoMeter,tomatoRating
                                    ,tomatoRotten,tomatoUserMeter,tomatoUserRating,poster_path,release_date FROM Movie";

                using (SqlConnection con = new SqlConnection(ConfigurationManager.AppSettings["SourceSqlConnectionString"]))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Connection.Open();
                        using (SqlDataReader row = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int count = 0;
                            List<Movie> movies = new List<Movie>();
                            while (row.Read())
                            {
                                Movie movie = new Movie()
                                              {
                                                  ID = row["ID"] is DBNull ? "" : row["ID"].ToString(),
                                                  Title = row["Title"] is DBNull ? "" : (string)row["Title"],
                                                  Year = row["Year"] is DBNull ? 0 : Convert.ToInt32(row["Year"]),
                                                  Rating = row["Rating"] is DBNull ? 0 : (double)row["Rating"],
                                                  NumVotes = row["NumVotes"] is DBNull ? 0 : Convert.ToInt32(row["NumVotes"]),
                                                  imdbID = row["imdbID"] is DBNull ? "" : (string)row["imdbID"],
                                                  Type = row["Type"] is DBNull ? "" : (string)row["Type"],
                                                  Released = row["Released"] is DBNull ? DateTime.Now : (DateTime)row["Released"],
                                                  Runtime = row["Runtime"] is DBNull ? 0 : Convert.ToInt32(row["Runtime"]),
                                                  Genres = row["Genres"] is DBNull ? "" : (string)row["Genres"],
                                                  Directors = row["Directors"] is DBNull ? "" : (string)row["Directors"],
                                                  Metascore = row["Metascore"] is DBNull ? 0 : (double)row["Metascore"],
                                                  imdbRating = row["imdbRating"] is DBNull ? 0 : (double)row["imdbRating"],
                                                  imdbVotes = row["imdbVotes"] is DBNull ? "" : (string)row["imdbVotes"],
                                                  tomatoMeter = row["tomatoMeter"] is DBNull ? 0 : (double)row["tomatoMeter"],
                                                  tomatoRating = row["tomatoRating"] is DBNull ? 0 : (double)row["tomatoRating"],
                                                  tomatoRotten = row["tomatoRotten"] is DBNull ? 0 : (double)row["tomatoRotten"],
                                                  tomatoUserMeter = row["tomatoUserMeter"] is DBNull ? 0 : (double)row["tomatoUserMeter"],
                                                  tomatoUserRating = row["tomatoUserRating"] is DBNull ? 0 : (double)row["tomatoUserRating"],
                                                  poster_path = row["poster_path"] is DBNull ? "" : "http://image.tmdb.org/t/p/w90" + (string)row["poster_path"],
                                                  release_date = row["release_date"] is DBNull ? DateTime.Now : (DateTime)row["release_date"],
                                              };

                                if (movies.Count > 990)
                                {
                                    indexClient.Documents.Index(IndexBatch.Create(movies.Select(doc => IndexAction.Create(doc))));
                                    movies.Clear();
                                }

                                movies.Add(movie);

                                count++;
                            }

                            if (movies.Count > 0)
                            {
                                indexClient.Documents.Index(IndexBatch.Create(movies.Select(doc => IndexAction.Create(doc))));
                                movies.Clear();
                            }
                        }
                    }
                }
            }
            catch (IndexBatchException e)
            {
                Console.WriteLine(
                    "Failed to index some of the documents: {0}",
                    String.Join(", ", e.IndexResponse.Results.Where(r => !r.Succeeded).Select(r => r.Key)));
            }

            Thread.Sleep(5000);

        }
        
        
    }


    public class Movie
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public double Rating { get; set; }
        public int NumVotes { get; set; }
        public string imdbID { get; set; }
        public string Type { get; set; }
        public DateTime Released { get; set; }
        public int Runtime { get; set; }
        public string Genres { get; set; }
        public string Directors { get; set; }
        public double Metascore { get; set; }
        public double imdbRating { get; set; }
        public string imdbVotes { get; set; }
        public double tomatoMeter { get; set; }
        public double tomatoRating { get; set; }
        public double tomatoRotten { get; set; }
        public double tomatoUserMeter { get; set; }
        public double tomatoUserRating { get; set; }
        public string poster_path { get; set; }
        public DateTime release_date { get; set; }

    }
}
