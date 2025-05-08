using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using API.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeminiController : ControllerBase
    {
        private readonly GeminiService _geminiService;

        public GeminiController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskGemini([FromBody] string userPrompt)
        {
            try
            {
                var response = await _geminiService.AskGeminiAsync(userPrompt);
                return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("status code"))
                {
                    var statusCodeStr = ex.Message.Split("status code ")[1].Split(':')[0];
                    if (int.TryParse(statusCodeStr, out int statusCode))
                    {
                        return StatusCode(statusCode, ex.Message);
                    }
                }
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost("extract-ocr")]
        public async Task<IActionResult> ExtractOcr([FromBody] string ocrtext)
        {
            try
            {
                var response = await _geminiService.ExtractTransactionInfoAsync(ocrtext);
                return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("status code"))
                {
                    var statusCodeStr = ex.Message.Split("status code ")[1].Split(':')[0];
                    if (int.TryParse(statusCodeStr, out int statusCode))
                    {
                        return StatusCode(statusCode, ex.Message);
                    }
                }
                return StatusCode(500, ex.Message);
            }

        }
    }
}
