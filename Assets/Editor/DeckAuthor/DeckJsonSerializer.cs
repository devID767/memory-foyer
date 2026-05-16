using System.Collections.Generic;
using System.Text;
using MemoryFoyer.Infrastructure.ScriptableObjects;

namespace MemoryFoyer.Editor.DeckAuthor
{
    public static class DeckJsonSerializer
    {
        public static string Serialize(IReadOnlyList<DeckAsset> sortedAssets)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[\n");
            for (int d = 0; d < sortedAssets.Count; d++)
            {
                DeckAsset deck = sortedAssets[d];
                sb.Append("  {\n");
                sb.Append("    \"deckId\": ").Append(JsonString(deck.DeckId)).Append(",\n");
                sb.Append("    \"displayName\": ").Append(JsonString(deck.DisplayName)).Append(",\n");
                sb.Append("    \"description\": ").Append(JsonString(deck.Description)).Append(",\n");
                sb.Append("    \"newCardsPerDay\": ").Append(deck.NewCardsPerDay).Append(",\n");
                sb.Append("    \"cardIds\": [");
                for (int c = 0; c < deck.Cards.Count; c++)
                {
                    if (c > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(JsonString(deck.Cards[c].CardId));
                }
                sb.Append("]\n");
                sb.Append("  }");
                if (d < sortedAssets.Count - 1)
                {
                    sb.Append(",");
                }
                sb.Append("\n");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string JsonString(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
