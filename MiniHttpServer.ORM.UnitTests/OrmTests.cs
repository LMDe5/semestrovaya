using MiniHttpServer.Models;

namespace MiniHttpServer.ORM.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class OrmTests
    {
        [TestMethod]
        public void CreateInsertSql_IncludesCardFieldsAndForeignKeys()
        {
            var properties = typeof(Card).GetProperties()
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            string sql = OrmContext.CreateInsertSql("cards", properties);

            StringAssert.StartsWith(sql, "INSERT INTO cards (");
            StringAssert.Contains(sql, "@Name");
            StringAssert.Contains(sql, "@CardTypeId");
            StringAssert.Contains(sql, "@CardAlignmentId");
        }

        [TestMethod]
        [TestCategory("PostgreSQL")]
        public async Task Insert_Read_Update_Delete_Card()
        {
            string? connectionString = Environment.GetEnvironmentVariable("GRAIL_TEST_DB_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Assert.Inconclusive("Задайте GRAIL_TEST_DB_CONNECTION для тестовой PostgreSQL с применённым init.sql.");
                return;
            }

            var orm = new OrmContext(connectionString);
            var types = await orm.GetAllAsync<CardType>("cardtypes");
            var alignments = await orm.GetAllAsync<CardAlignment>("cardalignments");
            var type = types.FirstOrDefault(t => t.Name == "Персонаж");
            var alignment = alignments.FirstOrDefault(a => a.Name == "Нейтрал");
            Assert.IsNotNull(type, "В тестовой БД нет типа «Персонаж».");
            Assert.IsNotNull(alignment, "В тестовой БД нет принадлежности «Нейтрал».");

            string name = "TestCard" + Guid.NewGuid().ToString("N")[..12];
            var card = new Card
            {
                Name = name,
                Type = type!.Name,
                Alignment = alignment!.Name,
                CardTypeId = type.Id,
                CardAlignmentId = alignment.Id,
                Ability = "Без особой способности",
                Description = "Тестовая карточка",
                ImageUrl = "/semestrovaya/images/default.png"
            };

            int insertedId = 0;
            try
            {
                Assert.AreEqual(1, await orm.InsertAsync("cards", card));

                var cards = await orm.GetAllAsync<Card>("cards");
                var found = cards.SingleOrDefault(c => c.Name == name);
                Assert.IsNotNull(found);
                insertedId = found!.Id;
                Assert.AreEqual(type.Id, found.CardTypeId);
                Assert.AreEqual(alignment.Id, found.CardAlignmentId);
                Assert.AreEqual("Нейтрал", found.Alignment);

                found.Ability = "Обновлённая способность";
                Assert.AreEqual(1, await orm.UpdateAsync("cards", insertedId, found));
                cards = await orm.GetAllAsync<Card>("cards");
                Assert.AreEqual("Обновлённая способность", cards.Single(c => c.Id == insertedId).Ability);

                Assert.AreEqual(1, await orm.DeleteAsync("cards", insertedId));
                cards = await orm.GetAllAsync<Card>("cards");
                Assert.IsFalse(cards.Any(c => c.Id == insertedId));
                insertedId = 0;
            }
            finally
            {
                if (insertedId != 0)
                {
                    await orm.DeleteAsync("cards", insertedId);
                }
            }
        }
    }
}
