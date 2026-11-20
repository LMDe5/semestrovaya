using ConsoleApp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace MiniTemplateEngine
{
    [TestClass]
    public sealed class HtmlTemplateRenderTests
    {
        private readonly HtmlTemplateRenderer renderer = new HtmlTemplateRenderer();

        [TestMethod]
        public void RenderFromString_NullTemplateOrModel_ReturnsNullText()
        {
            Assert.AreEqual("null", renderer.RenderFromString(null!, new { CardName = "Мерлин" }));
            Assert.AreEqual("null", renderer.RenderFromString("${CardName}", null!));
        }

        [TestMethod]
        public void RenderFromString_ReplacesCardProperties()
        {
            string template = "<h1>${CardName}</h1><p>${CardAbility}</p>";
            var model = new { CardName = "Мерлин", CardAbility = "Прозрение" };

            Assert.AreEqual("<h1>Мерлин</h1><p>Прозрение</p>", renderer.RenderFromString(template, model));
        }

        [TestMethod]
        public void RenderFromString_UnknownOrNullProperty_LeavesPlaceholder()
        {
            string template = "${CardName}: ${CardAbility} / ${CardAlignment}";
            var model = new { CardName = "Мерлин", CardAbility = (string?)null };

            Assert.AreEqual("Мерлин: ${CardAbility} / ${CardAlignment}",
                renderer.RenderFromString(template, model));
        }

        [TestMethod]
        [DataRow(true, "<p>Нет карточек</p>")]
        [DataRow(false, "<p>Карточки найдены</p>")]
        public void RenderFromString_IfElse_ChoosesCorrectBranch(bool isEmpty, string expected)
        {
            string template = "$if(IsEmpty) <p>Нет карточек</p> $else <p>Карточки найдены</p> $endif";

            Assert.AreEqual(expected, renderer.RenderFromString(template, new { IsEmpty = isEmpty }));
        }

        [TestMethod]
        public void RenderFromString_IfFalseWithoutElse_ReturnsEmptyString()
        {
            string template = "$if(IsEmpty) <p>Нет карточек</p> $endif";

            Assert.AreEqual("", renderer.RenderFromString(template, new { IsEmpty = false }));
        }

        [TestMethod]
        public void RenderFromString_Foreach_RendersCardList()
        {
            string template = "$foreach(var card in Cards) <h2>${CardName}</h2> $endfor";
            var model = new { Cards = new[] { new { CardName = "Мерлин" }, new { CardName = "Убийца" } } };

            Assert.AreEqual("<h2>Мерлин</h2><h2>Убийца</h2>", renderer.RenderFromString(template, model));
        }

        [TestMethod]
        public void RenderFromString_Foreach_WithEmptyOrMissingList_ReturnsEmptyString()
        {
            string template = "$foreach(var card in Cards) <h2>${CardName}</h2> $endfor";

            Assert.AreEqual("", renderer.RenderFromString(template, new { Cards = Array.Empty<object>() }));
            Assert.AreEqual("", renderer.RenderFromString(template, new { CardName = "Мерлин" }));
        }

        [TestMethod]
        public void RenderFromFile_ReadsTemplateFromTemporaryFile()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "<h1>${CardName}</h1>");

                string result = renderer.RenderFromFile(path, new { CardName = "Мерлин" });

                Assert.AreEqual("<h1>Мерлин</h1>", result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void RenderToFile_WritesToSeparateFile_AndKeepsTemplate()
        {
            string directory = Path.Combine(Path.GetTempPath(), "grail-template-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                string input = Path.Combine(directory, "template.html");
                string output = Path.Combine(directory, "result.html");
                string template = "<h1>${CardName}</h1>";
                File.WriteAllText(input, template);

                string result = renderer.RenderToFile(input, output, new { CardName = "Мерлин" });

                Assert.AreEqual("<h1>Мерлин</h1>", result);
                Assert.AreEqual(result, File.ReadAllText(output));
                Assert.AreEqual(template, File.ReadAllText(input));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
