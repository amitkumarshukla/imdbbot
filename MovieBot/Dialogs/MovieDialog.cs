using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace MovieBot.Dialogs
{
    public enum FilmType { None, Movie, Series, Episode, Game }
    public enum YesNo { None, Yes, No }

    [Serializable]
    public class MovieDialog
    {
        public FilmType FilmType { get; set; }
        public string Search { get; set; }
        [Optional()]
        [Template(TemplateUsage.NoPreference, "1")]
        public int? Page { get; set; }
        public bool HasMorePages { get; set; }

        public static IForm<MovieDialog> BuildForm()
        {
            OnCompletionAsyncDelegate<MovieDialog> processSearch = async (context, state) =>
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://www.omdbapi.com/");

                var jsonResult = await client.GetStringAsync($"?s={HttpUtility.HtmlEncode(state.Search)}&r=json&type={state.FilmType}");

                var movies = JObject.Parse(jsonResult);

                var message = context.MakeMessage();
                message.Text = $"These are {movies["Search"].Count()} results for **{state.Search}**:";
                if (message.Attachments == null)
                {
                    message.Attachments = new List<Attachment>();
                }

                foreach (var movie in movies["Search"])
                {
                    var images = new List<CardImage>();
                    
                    images.Add(new CardImage() { Url = movie.Value<string>("Poster") == "N/A" ? "http://vignette3.wikia.nocookie.net/shokugekinosoma/images/6/60/No_Image_Available.png/revision/latest?cb=20150708082716" : movie.Value<string>("Poster") });

                    var buttons = new List<CardAction>();
                    buttons.Add(new CardAction()
                    {
                        Value = "http://www.imdb.com//title/" + movie.Value<string>("imdbID") + "/?ref_=fn_tt_tt_1",
                        Type = "openUrl",
                        Title = "IMDB Page"
                    });

                    var card = new HeroCard()
                    {
                        Title = $"{movie.Value<string>("Title")} ({movie.Value<string>("Year")})",
                        Images = images,
                        Buttons = buttons
                    };

                    message.Attachments.Add(card.ToAttachment());
                }

                await context.PostAsync(message);
            };

            return new FormBuilder<MovieDialog>()
                .Message("Welcome to the IMDB bot. Now, you can start to search a movie, serie, episode or game.")                
                .OnCompletion(processSearch)
                .Build();
        }
    }
}