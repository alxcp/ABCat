using HtmlAgilityPack;

namespace ABCat.Shared
{
    public static class HtmlExtensions
    {
        public static void RemoveNodeById(this HtmlDocument document, string id)
        {
            var element = document.GetElementbyId(id);
            element?.ParentNode.RemoveChild(element);
        }

        public static void RemoveNodesByIds(this HtmlDocument document, params string[] ids)
        {
            foreach (var id in ids)
            {
                RemoveNodeById(document, id);
            }
        }
    }
}