using API.Models.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.Text;
using System.Text.Json;

namespace API.Services;

/// <summary>
/// Main report generation service following the Template Method pattern
/// Implements the complete report generation logic as described in the Java pattern
/// </summary>
public class ReportService : IReportService
{
    private readonly IStatisticService _statisticService;
    private readonly ICurrencyConverter _currencyConverter;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IStatisticService statisticService,
        ICurrencyConverter currencyConverter,
        ILogger<ReportService> logger)
    {
        _statisticService = statisticService;
        _currencyConverter = currencyConverter;
        _logger = logger;
    }

    /// <summary>
    /// Main report generation method following the Template Method pattern
    /// Implements the exact flow described in the Java pattern
    /// </summary>
    /// <param name="reportInfo">Report configuration</param>
    /// <returns>Report information with download details</returns>
    public async Task<ReportInfoDTO> GenerateReportAsync(CreateReportDTO reportInfo)
    {
        try
        {
            _logger.LogInformation("Starting report generation for type {Type} from {Start} to {End} in {Currency}",
                reportInfo.Type, reportInfo.StartDate, reportInfo.EndDate, reportInfo.Currency);

            // Phase 1: Validation Phase
            ValidateReportRequest(reportInfo);

            // Phase 2: Data Generation & Currency Conversion
            var reportData = await _statisticService.GenerateReportDataAsync(reportInfo);
            var convertedData = await HandleCurrencyConversion(reportData, reportInfo.Currency);

            // Phase 3: Template Selection & Parameters
            var reportParameters = CreateReportParameters(reportInfo, convertedData);

            // Phase 4: Report Compilation & Data Binding
            var reportId = Guid.NewGuid();
            var fileName = GenerateFileName(reportInfo, reportId);
            string filePath;
            string extension;

            switch (reportInfo.Format.ToLower())
            {
                case "pdf":
                    extension = "pdf";
                    filePath = await GeneratePdfReport(fileName, reportInfo.Type, convertedData, reportParameters);
                    break;
                case "csv":
                    extension = "csv";
                    filePath = await GenerateCsvReport(fileName, convertedData, reportParameters);
                    break;
                default:
                    throw new ArgumentException($"Unsupported report format: {reportInfo.Format}");
            }

            // Phase 5: PDF Export & Response
            var downloadUrl = GenerateDownloadUrl(filePath);

            var result = new ReportInfoDTO
            {
                ReportId = reportId.GetHashCode(),
                Status = "Generated",
                DownloadUrl = downloadUrl,
                Format = reportInfo.Format,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Report generated successfully with ID: {ReportID}, File: {FilePath}", 
                result.ReportId, filePath);

            return result;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid input validation for report generation");
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "File access error during report generation");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during report generation");
            throw;
        }
    }

    #region Phase 1: Validation Phase

    /// <summary>
    /// Validates report request parameters
    /// </summary>
    private void ValidateReportRequest(CreateReportDTO reportInfo)
    {
        // Date Validation: Ensures start date is not after end date
        if (reportInfo.StartDate > reportInfo.EndDate)
            throw new ArgumentException("Start date cannot be after end date");

        // Currency Validation: Only supports "VND" and "USD" currencies
        if (!string.IsNullOrEmpty(reportInfo.Currency) && 
            reportInfo.Currency.ToUpper() != "VND" && 
            reportInfo.Currency.ToUpper() != "USD")
            throw new ArgumentException("Only VND and USD currencies are supported");

        // Report Type Validation
        var supportedTypes = new[] { "cash-flow", "category-breakdown", "daily-summary", "weekly-summary", "monthly-summary", "yearly-summary", "transaction" };
        if (!string.IsNullOrEmpty(reportInfo.Type) && 
            !supportedTypes.Contains(reportInfo.Type.ToLower()))
            throw new ArgumentException($"Report type '{reportInfo.Type}' is not supported");

        // Format Validation
        var supportedFormats = new[] { "pdf", "csv" };
        if (!supportedFormats.Contains(reportInfo.Format.ToLower()))
            throw new ArgumentException($"Report format '{reportInfo.Format}' is not supported");
    }

    #endregion

    #region Phase 2: Currency Handling

    /// <summary>
    /// Handles currency conversion if USD is requested
    /// </summary>
    private async Task<object> HandleCurrencyConversion(object reportData, string targetCurrency)
    {
        // Default currency is VND, no conversion needed
        if (string.IsNullOrEmpty(targetCurrency) || targetCurrency.ToUpper() == "VND")
            return reportData;

        // USD conversion logic
        if (targetCurrency.ToUpper() == "USD")
        {
            // Fetch current exchange rate
            var exchangeRate = await _currencyConverter.FetchExchangeRateAsync();
            
            // Convert monetary values
            return _currencyConverter.ConvertToUSD(reportData, exchangeRate);
        }

        return reportData;
    }

    #endregion

    #region Phase 3: Template Selection & Parameters

    /// <summary>
    /// Creates parameter map for report generation
    /// </summary>
    private Dictionary<string, object> CreateReportParameters(CreateReportDTO model, object reportData)
    {
        var currencySymbol = model.Currency?.ToUpper() switch
        {
            "USD" => "$",
            "VND" => "₫",
            _ => "₫"
        };

        return new Dictionary<string, object>
        {
            ["startDate"] = model.StartDate,
            ["endDate"] = model.EndDate,
            ["currencySymbol"] = currencySymbol,
            ["currency"] = model.Currency?.ToUpper() ?? "VND",
            ["reportType"] = model.Type ?? "transaction",
            ["generatedAt"] = DateTime.Now,
            ["reportData"] = reportData
        };
    }

    /// <summary>
    /// Generates unique filename for report
    /// </summary>
    private string GenerateFileName(CreateReportDTO reportInfo, Guid reportId)
    {
        return $"report_{reportInfo.Type ?? "transaction"}_{reportId:N}_{DateTime.Now:yyyyMMdd_HHmmss}";
    }    /// <summary>
    /// Generates download URL for the report file
    /// </summary>
    private string GenerateDownloadUrl(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return $"/Reports/{fileName}";
    }

    #endregion

    #region Phase 4: Report Compilation & Data Binding

    /// <summary>
    /// Generates PDF report using QuestPDF (Strategy Pattern for different report types)
    /// </summary>
    private async Task<string> GeneratePdfReport(string fileName, string? reportType, object reportData, Dictionary<string, object> parameters)
    {
        var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        Directory.CreateDirectory(reportsDirectory);
        var filePath = Path.Combine(reportsDirectory, $"{fileName}.pdf");

        await Task.Run(() =>
        {
            Document
                .Create(container =>
                {                    
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // Template Method Pattern: Select template based on report type
                        ComposeReportTemplate(page, reportType?.ToLower() ?? "transaction", reportData, parameters);
                    });
                })
                .GeneratePdf(filePath);
        });

        return filePath;
    }    /// <summary>
    /// Strategy Pattern: Different template composition strategies for different report types
    /// </summary>
    private void ComposeReportTemplate(PageDescriptor page, string templateType, object data, Dictionary<string, object> parameters)
    {
        switch (templateType)
        {
            case "cash-flow":
                // Special Case - Cash Flow Reports use CashFlowSummaryDTO directly
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeCashFlowTemplate(container, data as CashFlowSummaryDTO, parameters));
                page.Footer().Element(container => ComposeStandardFooter(container));
                break;
                
            case "category-breakdown":
            case "daily-summary":
            case "weekly-summary":
            case "monthly-summary":
            case "yearly-summary":
                // General Reports - Handle both single objects and lists
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeGeneralTemplate(container, data, parameters));
                page.Footer().Element(container => ComposeStandardFooter(container));
                break;
                
            default:
                // Default transaction report
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeTransactionTemplate(container, data, parameters));
                page.Footer().Element(container => ComposeStandardFooter(container));
                break;
        }
    }

    /// <summary>
    /// Composes standard header for reports
    /// </summary>
    private void ComposeStandardHeader(IContainer container, Dictionary<string, object> parameters)
    {
        var reportType = parameters["reportType"].ToString() ?? "Financial";
        var generatedAt = (DateTime)parameters["generatedAt"];

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"{reportType.ToUpper().Replace("-", " ")} REPORT")
                    .FontSize(20)
                    .Bold()
                    .FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                
                column.Item().Text($"Generated on: {generatedAt:yyyy-MM-dd HH:mm}")
                    .FontSize(10)
                    .FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
            });
        });
    }

    /// <summary>
    /// Composes standard footer for reports
    /// </summary>
    private void ComposeStandardFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    /// <summary>
    /// Composes cash flow specific template
    /// </summary>
    private void ComposeCashFlowTemplate(IContainer container, CashFlowSummaryDTO? cashFlowData, Dictionary<string, object> parameters)
    {
        if (cashFlowData is null)
        {
            container.Text("No cash flow data available").FontSize(12);
            return;
        }

        var currencySymbol = parameters["currencySymbol"].ToString() ?? "₫";
        var startDate = (DateTime)parameters["startDate"];
        var endDate = (DateTime)parameters["endDate"];

        container.Column(column =>
        {
            column.Spacing(20);

            // Period information
            column.Item().Text($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}")
                .FontSize(12)
                .Bold();

            // Cash flow summary
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Description").Bold();
                    header.Cell().Element(CellStyle).Text("Amount").Bold();
                });

                // Data rows
                table.Cell().Element(CellStyle).Text("Total Income");
                table.Cell().Element(CellStyle).Text($"{cashFlowData.TotalIncome:N2} {currencySymbol}")
                    .FontColor(QuestPDF.Helpers.Colors.Green.Darken2);

                table.Cell().Element(CellStyle).Text("Total Expenses");
                table.Cell().Element(CellStyle).Text($"{cashFlowData.TotalExpenses:N2} {currencySymbol}")
                    .FontColor(QuestPDF.Helpers.Colors.Red.Darken2);

                table.Cell().Element(CellStyle).Text("Net Cash Flow").Bold();
                table.Cell().Element(CellStyle).Text($"{cashFlowData.NetCashFlow:N2} {currencySymbol}")
                    .FontColor(cashFlowData.NetCashFlow >= 0 ? QuestPDF.Helpers.Colors.Green.Darken2 : QuestPDF.Helpers.Colors.Red.Darken2)
                    .Bold();
            });
        });

        static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingVertical(5);
        }
    }

    /// <summary>
    /// Composes general template for other report types
    /// </summary>
    private void ComposeGeneralTemplate(IContainer container, object data, Dictionary<string, object> parameters)
    {
        var startDate = (DateTime)parameters["startDate"];
        var endDate = (DateTime)parameters["endDate"];
        var currency = parameters["currency"].ToString() ?? "VND";

        container.Column(column =>
        {
            column.Spacing(10);

            column.Item().Text($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}")
                .FontSize(12);

            column.Item().Text($"Currency: {currency}")
                .FontSize(10)
                .FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);

            // Convert data to JSON for display (simplified approach)
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            column.Item().Text(jsonData)
                .FontSize(8)
                .FontFamily("Courier New");
        });
    }

    /// <summary>
    /// Composes transaction template
    /// </summary>
    private void ComposeTransactionTemplate(IContainer container, object data, Dictionary<string, object> parameters)
    {
        // This would implement transaction-specific template logic
        // For now, using the general template
        ComposeGeneralTemplate(container, data, parameters);
    }

    /// <summary>
    /// Generates CSV report
    /// </summary>
    private async Task<string> GenerateCsvReport(string fileName, object reportData, Dictionary<string, object> parameters)
    {
        var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        Directory.CreateDirectory(reportsDirectory);
        var filePath = Path.Combine(reportsDirectory, $"{fileName}.csv");

        var startDate = (DateTime)parameters["startDate"];
        var endDate = (DateTime)parameters["endDate"];
        var currency = parameters["currency"].ToString() ?? "VND";

        await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        // Write report header
        await writer.WriteLineAsync($"Financial Report,{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        await writer.WriteLineAsync($"Currency,{currency}");
        await writer.WriteLineAsync($"Generated,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync();

        // Handle different data types
        switch (reportData)
        {
            case CashFlowSummaryDTO cashFlow:
                await writer.WriteLineAsync("Cash Flow Summary");
                await writer.WriteLineAsync($"Total Income,{cashFlow.TotalIncome:N2}");
                await writer.WriteLineAsync($"Total Expenses,{cashFlow.TotalExpenses:N2}");
                await writer.WriteLineAsync($"Net Cash Flow,{cashFlow.NetCashFlow:N2}");
                break;

            case IEnumerable<CategoryBreakdownDTO> categories:
                await writer.WriteLineAsync("Category,Total Income,Total Expense,Income %,Expense %");
                foreach (var category in categories)
                {
                    await writer.WriteLineAsync($"\"{EscapeCsvField(category.Category)}\"," +
                                              $"{category.TotalIncome:N2}," +
                                              $"{category.TotalExpense:N2}," +
                                              $"{category.IncomePercentage:N2}," +
                                              $"{category.ExpensePercentage:N2}");
                }
                break;

            default:
                // Handle other data types using reflection or JSON serialization
                var jsonData = JsonSerializer.Serialize(reportData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await writer.WriteLineAsync("Data (JSON format)");
                await writer.WriteLineAsync($"\"{EscapeCsvField(jsonData)}\"");
                break;
        }

        return filePath;
    }

    /// <summary>
    /// Escapes CSV field content
    /// </summary>
    private string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return field.Replace("\"", "\"\"");
        }

        return field;
    }

    #endregion
}
