using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using static ASP.MongoDb.API.Entities.templateChildValue;

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
        public List<TableComponentState>? tableInner {get;set;}
        public string? questionName { get; set; }
        public questionaryQuestionChildren? questionInnerValueChildren { get; set; }
        public string? type { get; set; }
        public string? value { get; set; }
        public templateStructureChildrenTextAreaClassName? className { get; set; }
    }



    public class questionaryQuestionChildren { 
        public Int32 questionIndex { get; set; }
        public string choosedAnswer { get; set; }
        public  List<questionaryQuestionChildrenContext> context { get; set; }
        
    }

    public class questionaryQuestionChildrenContext
    {
        public string? questionAnswer { get; set; }
        public Int16 questionIdentifier { get; set; }
        public List<templateStructureChildren>? answeredQuestionInner { get; set; }
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
        public bool underline { get; set; }
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


        ////tableInner
        public class FontStyle
        {
            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public bool Underline { get; set; }
        }

        public class ClassName
        {
            public int FontSize { get; set; }
            public FontStyle FontStyle { get; set; }
            public string FontFamily { get; set; }
            public string Justify { get; set; }
            public string FontColor { get; set; }
        }

        public class TextArea
        {
            public string Uuid { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
            public ClassName ClassName { get; set; }
        }

        public class Child
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public List<string> Option { get; set; } = new List<string>();
        }

        public class AnswerInner
        {
            public string Name { get; set; }
            public string Justify { get; set; }
            public int Index { get; set; }
            public List<TextArea> TextArea { get; set; } = new List<TextArea>();
            public List<List<Child>> Children { get; set; } = new List<List<Child>>();
        }

        public class QuestionInner
        {
            public string QuestionAnswer { get; set; }
            public List<List<AnswerInner>> AnswerInner { get; set; } = new List<List<AnswerInner>>();
        }

        public class TableComponentState
        {
            public string Question { get; set; }
            public string Uuid { get; set; }
            public int AmountOfCols { get; set; }
            public int ChoosedAnswer { get; set; }
            public string ConnectedUuid { get; set; }
            public List<QuestionInner> QuestionInner { get; set; } = new List<QuestionInner>();
        }
    }
}
