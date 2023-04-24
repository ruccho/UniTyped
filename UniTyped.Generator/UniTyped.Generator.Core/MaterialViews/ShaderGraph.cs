using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UniTyped.Generator.MaterialViews;

public class ShaderGraphParser : ShaderParser
{
    public override bool Match(string shaderFullPath)
    {
        return Path.GetExtension(shaderFullPath) == ".shadergraph";
    }

    public override bool Process(string shaderFullPath, IList<ShaderProperty> result)
    {
        var shaderContent = File.ReadAllText(shaderFullPath, Encoding.UTF8);

        var sg = new ShaderGraph(shaderContent);

        var graphData = sg.Objects.FirstOrDefault(o => o.Type == "UnityEditor.ShaderGraph.GraphData");
        if (graphData == null) return false;

        foreach (var e in graphData.Document["m_Properties"])
        {
            string referencedId = (string)e["m_Id"];
            var referencedObj = sg.Objects.FirstOrDefault(o => o.Id == referencedId);
            if (referencedObj == null) continue;

            var root = referencedObj.Document;

            // is exposed
            if (!(bool)root["m_GeneratePropertyBlock"]) continue;

            var defaultReferenceName = (string)root["m_DefaultReferenceName"];
            var overrideReferenceName = (string)root["m_OverrideReferenceName"];

            string? referenceName = string.IsNullOrEmpty(overrideReferenceName)
                ? defaultReferenceName
                : overrideReferenceName;

            if (string.IsNullOrEmpty(referenceName)) continue;


            PropertyProvider? provider = referencedObj.Type switch
            {
                "UnityEditor.ShaderGraph.Internal.ColorShaderProperty" => SimplePropertyProvider.Color,
                "UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty" => SimplePropertyProvider.Float,
                "UnityEditor.ShaderGraph.Internal.Vector2ShaderProperty" or
                    "UnityEditor.ShaderGraph.Internal.Vector4ShaderProperty" => SimplePropertyProvider.Vector,
                "UnityEditor.ShaderGraph.Internal.Texture2DShaderProperty" or
                    "UnityEditor.ShaderGraph.Internal.Texture3DShaderProperty" or
                    "UnityEditor.ShaderGraph.VirtualTextureShaderProperty" or
                    "UnityEditor.ShaderGraph.Internal.CubemapShaderProperty" => TexturePropertyProvider.Instance,
                _ => null
            };

            if (provider == null) continue;
            
            result.Add(new ShaderProperty(provider, referenceName));
            
        }

        return true;
    }
}

public class ShaderGraph
{
    private readonly List<SGObject> objects = new();
    private readonly Dictionary<string, SGObject> objectIdDict = new();

    public IReadOnlyList<SGObject> Objects => objects;

    public ShaderGraph(string text)
    {
        /*
         * *.shadergraph files are consist of multiple json objects like:
         * 
         *  {
         *      "m_Type": "UnityEditor.ShaderGraph.GraphData",
         *      "m_ObjectId": "9e66f3df1fdb431a843a218972a2bfba",
         *      ...
         *  }
         *  
         *  {
         *      "m_SGVersion": 0,
         *      "m_Type": "UnityEditor.ShaderGraph.CategoryData",
         *      "m_ObjectId": "14e728f6032146a1a59d01e2315f82d8",
         *      ...
         *  }
         *  
         *  ...
         *  
         * We need to split them into individual json objects before parsing.
         * 
         */

        var span = text.AsMemory();
        int cursor = 0;
        while (true)
        {
            //FIXME: split with better ways than this!
            int end = text.IndexOf("}\n\n{", cursor);
            bool last = false;
            if (end < 0)
            {
                last = true;
                end = text.Length - 1;
            }

            var slice = span.Slice(cursor, end - cursor + 1);

            var doc = JObject.Parse(new string(slice.ToArray()));
            
            var obj = new SGObject(doc);

            objects.Add(obj);
            objectIdDict[obj.Id] = obj;

            cursor = end + 1;

            if (last) break;
        }
    }

    public class SGObject
    {
        public int Version { get; }
        public string Type { get; }
        public string Id { get; }

        public JObject Document { get; }

        public SGObject(JObject doc)
        {
            this.Document = doc;

            Version = (int)doc["m_SGVersion"];
            Type = (string)doc["m_Type"];
            Id = (string)doc["m_ObjectId"];
        }
    }
}