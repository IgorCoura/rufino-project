using System.Text.Json.Nodes;

namespace PeopleManagement.Domain.Utils
{
    public static class JsonExtesion
    {
        public static JsonObject MergeJsonObjects(this JsonObject targetObject, JsonObject sourceObject)
        {
            foreach (var propSource in sourceObject)
            {
                if (targetObject[propSource.Key] is JsonObject targetChild && propSource.Value is JsonObject sourceChild)
                {
                    // Recursivamente une objetos filhos
                    MergeJsonObjects(targetChild, sourceChild);
                }
                else if (targetObject.ContainsKey(propSource.Key) == false)
                {
                    // Adiciona propriedade nova
                    targetObject[propSource.Key] = JsonNode.Parse(propSource.Value!.ToJsonString());
                }
            }

            return targetObject;
        }

        public static JsonObject MergeListJsonObjects(this List<JsonObject> objects)
        {
            var result = new JsonObject();

            foreach(var obj in objects)
            {
                result = MergeJsonObjects(result, obj);
            }
            return result;
        }

        public static JsonObject ToJsonObject(this DateOnly? date)
        {
            if(date == null)
            {
                return new JsonObject
                {
                    ["date"] = null
                };
            }
            return new JsonObject
            {
                ["date"] = $"{date}"
            };
        }



        public static JsonObject Clone(this JsonObject original)
        {
            return JsonNode.Parse(original.ToJsonString())!.AsObject();
        }
    }
}
