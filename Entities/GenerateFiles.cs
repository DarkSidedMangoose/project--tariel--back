using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace ASP.MongoDb.API.Entities
{
    public class GenerateFiles
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        public string? templateName { get; set; }
        public List<templateStructure>? templateState { get; set; }
    }

    public class templateStructure
    {
        public string? name { get; set; }
        public bool? remove { get; set; }
        public List<templateStructureChildren>? children { get; set; }
    }

    public class templateStructureChildren
    {
        public string? name { get; set; }
        public string? justify { get; set; }
        public List<templateStructureChildrenTextArea>? textArea { get; set; }
        public int? index { get; set; }
        public List<List<templateStructureChildrenChildItem>>? children { get; set; }
    }

    public class templateStructureChildrenTextArea
    {
        public string? uuid { get; set; }
        public string? questionName { get; set; }
        public string? type { get; set; }
        public string? value { get; set; }
        public templateStructureChildrenTextAreaClassName? className { get; set; }
    }

    public class templateStructureChildrenTextAreaClassName
    {
        public int? fontSize { get; set; }
        public templateStructureChildrenTextAreaClassNameFontStyle? fontStyle { get; set; }
        public string? fontFamily { get; set; }
        public string? fontElement { get; set; }
        public string? justify { get; set; }
        public string? fontColor { get; set; }
    }

    public class templateStructureChildrenTextAreaClassNameFontStyle
    {
        public bool bold { get; set; }
        public bool italic { get; set; }
        public bool underLine { get; set; }
    }

    public class templateStructureChildrenChildItem
    {
        public string? name { get; set; }
        public string? type { get; set; }
        public List<string>? option { get; set; }
        public templateChildValue? value { get; set; }
    }

    public class templateChildValue
    {
        [BsonIgnoreIfNull]
        public string? stringValue { get; set; }

        [BsonIgnoreIfNull]
        public double? numberValue { get; set; }

        [BsonIgnoreIfNull]
        public templateStructureChildrenTextAreaClassNameFontStyle? objectValue { get; set; }

        public bool isValid()
        {
            int count = 0;
            if (stringValue != null) count++;
            if (numberValue != null) count++;
            if (objectValue != null) count++;
            return count == 1;
        }
    }
}
