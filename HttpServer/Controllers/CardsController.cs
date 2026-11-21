using HttpServer.Controllers;
using HttpServer.Framework.Attributes;
using HttpServer.Framework.HttpResponses;
using MiniHttpServer.Models;
using MiniHttpServer.ORM;

namespace MiniHttpServer.Controllers
{
    [HttpController("cards")]
    internal class CardsController : ControllerBase
    {
        private static string connectionString = "Host=localhost;Port=5432;Database=myDataBase;Username=postgres;Password=piskoed228";
        private readonly IOrmContext ormContext = new OrmContext(connectionString);

        [Get("")]
        public IHttpResponseTypeResult GetAll()
        {
            return ShowCards(0);
        }

        [Get("filter")]
        public IHttpResponseTypeResult Filter(string alignmentId)
        {
            if (!int.TryParse(alignmentId, out int id) || id < 0)
            {
                return new JsonResult(new { Message = "Некоректноая принадлежность" }, 400);
            }

            return ShowCards(id);
        }

        [Get("view")]
        public IHttpResponseTypeResult GetCardById(string id)
        {
            if (!int.TryParse(id, out int cardId) || cardId <= 0)
            {
                Context.Response.StatusCode = 400;
                return Html("<h1>400 — Некорректный ID карточки</h1><a href=\"/cards\">Вернуться в каталог</a>");
            }

            var res = ormContext.GetAllAsync<Card>("cards").GetAwaiter().GetResult();
            var card = res.FirstOrDefault(c => c.Id == cardId);

            if (card == null)
            {
                Context.Response.StatusCode = 404;
                return Html("semestrovaya/404.html", new { });
            }

            var model = new
            {
                CardId = card.Id,
                CardName = card.Name,
                CardType = card.Type,
                CardAlignment = card.Alignment,
                CardAbility = card.Ability,
                CardDescription = card.Description,
                CardImage = card.ImageUrl
            };

            return Html("semestrovaya/cardInfo.html", model);
        }

        private IHttpResponseTypeResult ShowCards(int alignmentId)
        {
            var cards = ormContext.GetAllAsync<Card>("cards").GetAwaiter().GetResult();
            var alignments = ormContext.GetAllAsync<CardAlignment>("cardalignments").GetAwaiter().GetResult();

            var viewCards = cards.
                Where(c => alignmentId == 0 || c.CardAlignmentId == alignmentId).
                Select(c => new
                {
                    CardId = c.Id,
                    CardName = c.Name,
                    CardType = c.Type,
                    CardAlignment = c.Alignment,
                    CardAbility = c.Ability,
                    CardImage = c.ImageUrl
                }).ToList();

            var filters = alignments.OrderBy(c => c.Id).Select(c => new
            {
                c.Id,
                c.Name
            }).ToList();

            return Html("semestrovaya/cardsPage.html", new
            {
                Cards = viewCards,
                AlignmentFilters = filters,
            });
        }
    }
}