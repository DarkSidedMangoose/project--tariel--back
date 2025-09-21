using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Http;
using ASP.MongoDb.API.Entities;
using Microsoft.AspNetCore.Mvc;
using IronWord;
using IronWord.Models;
using IronWord.Models.Enums;
using SixLabors.Fonts;
using Font = IronWord.Models.Font;
using System.Drawing;
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;

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

                // Create and save Word document
                var doc = new WordDocument();
                doc.AddText("Hello from IronWord!");
                doc.SaveAs(filePath);

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
                WordDocument doc = new WordDocument();

                foreach (var templateStateObj in response.templateState)
                {
                    foreach (var templateStateObjChildren in templateStateObj.children)
                    {
                        Paragraph paragraph = new Paragraph();
                        // Set alignment
                        switch (templateStateObjChildren.justify?.ToLower())
                        {
                            case "left":
                                paragraph.Alignment = IronWord.Models.Enums.TextAlignment.Left;
                                break;
                            case "center":
                                paragraph.Alignment = IronWord.Models.Enums.TextAlignment.Center;
                                break;
                            case "right":
                                paragraph.Alignment = IronWord.Models.Enums.TextAlignment.Right;
                                break;
                            default:
                                paragraph.Alignment = IronWord.Models.Enums.TextAlignment.Left;
                                break;
                        }

                        foreach (var textArea in templateStateObjChildren.textArea)
                        {
                            TextContent textRun = new TextContent
                            {
                                Text = textArea.value,
                                Style = new TextStyle
                                {
                                    TextFont = new Font
                                    {
                                        FontFamily = textArea.className.fontFamily,
                                        FontSize = (float)(textArea.className.fontSize ?? 16)
                                    },
                                    Color = ParseColor(textArea.className.fontColor),
                                    IsBold = textArea.className.fontStyle.bold,
                                    IsItalic = textArea.className.fontStyle.italic,
                                    Underline = textArea.className.fontStyle.underLine ? new Underline() : null,
                                    // Add background color (highlight or shading)
                                    
                                }
                            };

                            paragraph.AddText(textRun);
                        }

                        doc.AddParagraph(paragraph);
                    }
                }


                // Save and return the file
                var fileName = string.IsNullOrWhiteSpace(response.templateName) ? "output.docx" : response.templateName;
                var filePath = Path.Combine("C:\\New folder\\ASP.MongoDb.API", fileName);
                doc.SaveAs(filePath);

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

    

    }
}