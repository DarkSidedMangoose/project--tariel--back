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
            {
                return BadRequest("connection Error to the database");
            }
            else
            {

                var templateList = allData.Select(t => t.templateName).ToList();
                var templateIds = allData.Select(t => t.id).ToList();

                return Ok(new
                {
                    templateList = templateList,
                    templateIds = templateIds
                });
            }
        }
        [HttpGet("getTemplateState")]
        public async Task<IActionResult> GetTemplateState([FromQuery] string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
            {
                return BadRequest("cant access template id correctly");
            }

            var templateData = await _generateFilesRepository.GetByIdAsync(templateId);
            return Ok(templateData);
        }

        [HttpPost("addNewTemplate")]
        public async Task<IActionResult> AddNewTemplate([FromBody] GenerateFiles response)
        {
            if (response == null)
            {
                return BadRequest("response take from front is null");
            }
            else
            {
                await _generateFilesRepository.CreateAsync(response);
                return Ok("yes");

            }
        }

        [HttpPut("updateTemplate")]
        public async Task<IActionResult> UpdateTemplate([FromBody] GenerateFiles response)
        {
            if (response == null)
            {
                return BadRequest("Response from frontend is null.");
            }

            await _generateFilesRepository.UpdateAsync(response.id, response);

            string fileDirectory = @"C:\New folder\ASP.MongoDb.API";
            string filePath = Path.Combine(fileDirectory, "output.docx");

            try
            {
                // Ensure directory exists
                if (!Directory.Exists(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }

                // Check if file is locked
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            // File is accessible
                        }
                    }
                    catch (IOException)
                    {
                        return StatusCode(StatusCodes.Status423Locked, "The file is currently in use by another process.");
                    }
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
                var fileName = string.IsNullOrWhiteSpace(response.templateName) ? "output.docx" : response.templateName;
                var filePath = Path.Combine("C:\\New folder\\ASP.MongoDb.API", fileName);

                using (var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body());

                    foreach (var templateStateObj in response.templateState)
                    {
                        foreach (var child in templateStateObj.children)
                        {
                            var paragraph = BuildParagraph(child);
                            mainPart.Document.Body.Append(paragraph);
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
        }
        private Paragraph BuildParagraph(templateStructureChildren child)
        {
            var paragraph = new Paragraph();

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

            foreach (var textArea in child.textArea)
            {
                var run = BuildRun(textArea);
                paragraph.Append(run);
            }

            return paragraph;
        }
        private Run BuildRun(templateStructureChildrenTextArea textArea)
        {
            var runProps = new RunProperties
            {
                RunFonts = new RunFonts { Ascii = textArea.className.fontFamily },
                FontSize = new FontSize { Val = ((textArea.className.fontSize ?? 16) * 2).ToString() }
            };

            if (!string.IsNullOrEmpty(textArea.className.fontColor))
            {
                runProps.Color = new DocumentFormat.OpenXml.Wordprocessing.Color
                {
                    Val = ConvertColorToHex(textArea.className.fontColor)
                };
            }

            if (textArea.className.fontStyle.bold) runProps.Bold = new Bold();
            if (textArea.className.fontStyle.italic) runProps.Italic = new Italic();
            if (textArea.className.fontStyle.underLine) runProps.Underline = new Underline { Val = UnderlineValues.Single };

            var run = new Run(runProps, new Text
            {
                Text = textArea.value,
                Space = SpaceProcessingModeValues.Preserve
            });

            return run;
        }
        private string ConvertColorToHex(string rgb)
        {
            try
            {
                var color = System.Drawing.ColorTranslator.FromHtml(rgb);
                return $"{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            catch
            {
                return "000000"; // fallback to black
            }
        }

    }
}