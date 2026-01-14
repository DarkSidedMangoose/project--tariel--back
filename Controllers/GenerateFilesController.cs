using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Http;
using ASP.MongoDb.API.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenerateFilesController : ControllerBase
    {
        private readonly IGenerateFilesRepository _generateFilesRepository;

        public GenerateFilesController(IGenerateFilesRepository generateFilesRepository)
        {
            _generateFilesRepository = generateFilesRepository;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _generateFilesRepository.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("getTemplatesName")]
        public async Task<IActionResult> GetTemplates()
        {
            var allData = await _generateFilesRepository.GetAllAsync();
            if (allData == null)
                return BadRequest("Connection error to the database");

            var templateList = allData.Select(t => t.templateName).ToList();
            var templateIds = allData.Select(t => t.id).ToList();

            return Ok(new { templateList, templateIds });
        }

        [HttpGet("getTemplateState")]
        public async Task<IActionResult> GetTemplateState([FromQuery] string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
                return BadRequest("Template id is missing");

            var templateData = await _generateFilesRepository.GetByIdAsync(templateId);
            return Ok(templateData);
        }

        [HttpPost("addNewTemplate")]
        public async Task<IActionResult> AddNewTemplate([FromBody] GenerateFiles response)
        {
            if (response == null)
                return BadRequest("Response from frontend is null");

            await _generateFilesRepository.CreateAsync(response);
            return Ok("Template added successfully");
        }

        [HttpPut("updateTemplate")]
        public async Task<IActionResult> UpdateTemplate([FromBody] GenerateFiles response)
        {
            if (response == null)
                return BadRequest("Response from frontend is null.");

            await _generateFilesRepository.UpdateAsync(response.id, response);

            var fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedFiles");
            Directory.CreateDirectory(fileDirectory);
            var filePath = Path.Combine(fileDirectory, "updated.docx");

            try
            {
                using (var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body(
                        new Paragraph(new Run(new Text($"Template updated: {response.templateName}")))
                    ));
                    mainPart.Document.Save();
                }

                return Ok("Template updated and Word file saved successfully.");
            }
            catch (IOException ioEx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"File access error: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Unexpected error: {ex.Message}");
            }
        }

        private Color ParseColor(string? colorString)
        {
            if (string.IsNullOrWhiteSpace(colorString))
                return Color.Black;

            colorString = colorString.Trim().Trim('"');

            if (colorString.StartsWith("rgb"))
            {
                var match = Regex.Match(colorString, @"rgb\((\d+),\s*(\d+),\s*(\d+)\)");
                if (match.Success)
                {
                    int r = int.Parse(match.Groups[1].Value);
                    int g = int.Parse(match.Groups[2].Value);
                    int b = int.Parse(match.Groups[3].Value);
                    return Color.FromArgb(r, g, b);
                }
            }

            try
            {
                return ColorTranslator.FromHtml(colorString);
            }
            catch
            {
                return Color.Black; // fallback
            }
        }

        [HttpPost("generateWordFile")]
        public IActionResult GenerateWordFile([FromBody] GenerateFiles response)
        {
            try
            {
                var fileName = string.IsNullOrWhiteSpace(response.templateName) ? "output.docx" : response.templateName + ".docx";
                var fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedFiles");
                Directory.CreateDirectory(fileDirectory);
                var filePath = Path.Combine(fileDirectory, fileName);

                using (var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body());

                    if (response.templateState != null)
                    {
                        foreach (var templateStateObj in response.templateState)
                        {
                            if (templateStateObj.children != null)
                            {
                                foreach (var child in templateStateObj.children)
                                {
                                    if(child.textArea[0].type == "questionary")
                                    {
                                       foreach(var questionaryChild in child.textArea)
                                        {
                                            foreach(var contextInner in questionaryChild.questionInnerValueChildren.context)
                                            {
                                                if(contextInner.questionAnswer == questionaryChild.questionInnerValueChildren.choosedAnswer)
                                                {
                                                    foreach(var choosedContextInner in contextInner.answeredQuestionInner)
                                                    {
                                                        var paragraph = BuildParagraph(choosedContextInner);
                                                        mainPart.Document.Body.Append(paragraph);
                                                    }
                                                }
                                            }
                                        }

                                    }
                                    else
                                    {

                                        var paragraph = BuildParagraph(child);
                                    mainPart.Document.Body.Append(paragraph);
                                    }
                                }
                            }
                        }
                    }

                    mainPart.Document.Save();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes,
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                            fileName);
            }
            catch (IOException ioEx)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"File access error: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Unexpected error: {ex.Message}");
            }
        }

        private Paragraph BuildParagraph(templateStructureChildren child)
        {
            var paragraph = new Paragraph();

            //if (textArea.type == "questionary")
            //{
            //    foreach (var questionInnerContext in textArea.questionInnerValueChildren.context)
            //    {
            //        if (textArea.questionInnerValueChildren.choosedAnswer == questionInnerContext.questionAnswer)
            //        {
            //            foreach (var choosedQuestionInner in questionInnerContext.answeredQuestionInner)
            //            {
            //                BuildParagraph(choosedQuestionInner);
            //            }
            //        }
            //    }


            //}


            var justification = new Justification
            {
                Val = child.justify?.ToLower() switch
                {
                    "center" => JustificationValues.Center,
                    "right" => JustificationValues.Right,
                    _ => JustificationValues.Left
                }
            };

            var pProps = new ParagraphProperties(justification);
            paragraph.AppendChild(pProps);

            if (child.textArea != null)
            {
                foreach (var textArea in child.textArea)
                {
                    var run = BuildRun(textArea);
                    paragraph.Append(run);
                }
            }

            return paragraph;
        }

        private Run BuildRun(templateStructureChildrenTextArea textArea)
        {
            var runProps = new RunProperties();
            
           
            if (textArea.className != null)
            {
                if (!string.IsNullOrEmpty(textArea.className.fontFamily))
                    runProps.RunFonts = new RunFonts { Ascii = textArea.className.fontFamily };

                runProps.FontSize = new FontSize { Val = ((textArea.className.fontSize ?? 16) * 2).ToString() };

                if (!string.IsNullOrEmpty(textArea.className.fontColor))
                    runProps.Color = new DocumentFormat.OpenXml.Wordprocessing.Color
                    {
                        Val = ConvertColorToHex(textArea.className.fontColor)
                    };

                if (textArea.className.fontStyle?.bold == true) runProps.Bold = new Bold();
                if (textArea.className.fontStyle?.italic == true) runProps.Italic = new Italic();
                if (textArea.className.fontStyle?.underLine == true) runProps.Underline = new Underline { Val = UnderlineValues.Single };
            }

            var run = new Run(runProps, new Text
            {
                Text = textArea.value ?? string.Empty,
                Space = SpaceProcessingModeValues.Preserve
            });

            return run;
        }

        private string ConvertColorToHex(string colorString)
        {
            try
            {
                var color = ParseColor(colorString);
                return $"{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            catch
            {
                return "000000"; // fallback to black
            }
        }

        
    }
}
