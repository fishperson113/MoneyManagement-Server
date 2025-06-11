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
        // Monthly report validation: Ensure start and end dates are in the same year
        if (reportInfo.Type?.ToLower() == "monthly-summary" && reportInfo.StartDate.Year != reportInfo.EndDate.Year)
            throw new ArgumentException("Monthly summary reports must be within the same year");

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
                // Category breakdown reports use CategoryBreakdownDTO collection
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeCategoryBreakdownTemplate(container, data, parameters));
                page.Footer().Element(container => ComposeStandardFooter(container));
                break;
                
            case "daily-summary":
                // Daily summary report - using our new template
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeDailySummaryTemplate(container, data, parameters));
                page.Footer().Element(container => ComposeStandardFooter(container));
                break;
            case "weekly-summary":
                // Weekly summary report - using our new template
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeWeeklySummaryTemplate(container, data, parameters));
                page.Footer().Element(container => ComposeStandardFooter(container));
                break;
            case "monthly-summary":
                // Monthly summary report - using our new template
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeMonthlyTemplate(container, data, parameters));
                page.Footer().Element(container => ComposeStandardFooter(container));
                break;
            // Update this case in the ComposeReportTemplate method's switch statement
            case "yearly-summary":
                // Yearly summary report using our specialized template
                page.Header().Element(container => ComposeStandardHeader(container, parameters));
                page.Content().Element(container => ComposeYearlySummaryTemplate(container, data, parameters));
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
    }    /// <summary>
    /// Composes cash flow specific template matching the professional design with enhanced charts and layout
    /// </summary>
    private void ComposeCashFlowTemplate(IContainer container, CashFlowSummaryDTO? cashFlowData, Dictionary<string, object> parameters)    {
        if (cashFlowData is null)
        {
            container.Text("No cash flow data available").FontSize(12);
            return;
        }

        var currencySymbol = parameters["currencySymbol"].ToString() ?? "₫";
        var currency = parameters["currency"].ToString() ?? "VND";
        var startDate = (DateTime)parameters["startDate"];
        var endDate = (DateTime)parameters["endDate"];        
        container.Column(column =>
        {
            column.Spacing(15); // Reduced from 25

            // Header Section with professional dark blue-gray background - more compact
            column.Item().Background("#34495E").Padding(20).Column(headerColumn => // Reduced from 30
            {
                headerColumn.Item().Text("CASH FLOW SUMMARY")
                    .FontSize(24) // Reduced from 32
                    .Bold()
                    .FontColor(QuestPDF.Helpers.Colors.White)
                    .AlignCenter();
                
                headerColumn.Item().PaddingTop(6).Text($"Reporting Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}") // Reduced from 10
                    .FontSize(12) // Reduced from 16
                    .FontColor("#BDC3C7")
                    .AlignCenter();
            });

            // Financial Overview Section
            column.Item().PaddingTop(20).Text("Financial Overview") // Reduced from 30
                .FontSize(16) // Reduced from 20
                .Bold()
                .FontColor("#2C3E50");

            // Summary Cards Row - More compact design
            column.Item().PaddingTop(12).Row(row => // Reduced from 20
            {
                // Total Income Card - Green styling
                row.RelativeItem().Background("#D5F4E6")
                    .Border(2).BorderColor("#27AE60")
                    .Padding(18).Column(incomeColumn => // Reduced from 25
                    {
                        incomeColumn.Item().Text("Total Income")
                            .FontSize(14) // Reduced from 16
                            .FontColor("#27AE60")
                            .Bold();
                        incomeColumn.Item().PaddingTop(8).Text($"{cashFlowData.TotalIncome:N0} {currency}") // Reduced from 12
                            .FontSize(20) // Reduced from 24
                            .FontColor("#1E8449")
                            .Bold();
                    });

                row.ConstantItem(30); // Reduced from 40

                // Total Expenses Card - Red styling
                row.RelativeItem().Background("#FADBD8")
                    .Border(2).BorderColor("#E74C3C")
                    .Padding(18).Column(expenseColumn => // Reduced from 25
                    {
                        expenseColumn.Item().Text("Total Expenses")
                            .FontSize(14) // Reduced from 16
                            .FontColor("#E74C3C")
                            .Bold();
                        expenseColumn.Item().PaddingTop(8).Text($"{cashFlowData.TotalExpenses:N0} {currency}") // Reduced from 12
                            .FontSize(20) // Reduced from 24
                            .FontColor("#C0392B")
                            .Bold();
                    });
            });

            // Visual Analysis Section
            column.Item().PaddingTop(25).Text("Visual Analysis") // Reduced from 40
                .FontSize(16) // Reduced from 20
                .Bold()
                .FontColor("#2C3E50");

            // Income vs Expenses Comparison Chart Title
            column.Item().PaddingTop(12).Text("Income vs Expenses Comparison") // Reduced from 20
                .FontSize(14) // Reduced from 18
                .Bold()
                .FontColor("#34495E")
                .AlignCenter();

            // Compact Chart Area
            column.Item().PaddingTop(15).Background("#F8F9FA") // Reduced height from 300 to 200, padding from 25 to 15
                .Border(2).BorderColor("#E9ECEF")
                .Padding(25).Column(chartColumn => // Reduced from 40
                {
                    // Chart container with compact proportions
                    chartColumn.Item().Height(130).Row(chartRow => // Reduced from 200
                    {
                        chartRow.ConstantItem(50); // Reduced from 60
                        
                        // Main chart area
                        chartRow.RelativeItem().Column(chartContent =>
                        {
                            // Calculate bar heights
                            var maxValue = Math.Max(cashFlowData.TotalIncome, cashFlowData.TotalExpenses);
                            var scaleFactor = maxValue > 0 ? 100.0 / (double)maxValue : 0; // Reduced from 180
                            var incomeHeight = (float)((double)cashFlowData.TotalIncome * scaleFactor);
                            var expenseHeight = (float)((double)cashFlowData.TotalExpenses * scaleFactor);

                            // Chart bars container
                            chartContent.Item().Height(100).Row(barsRow => // Reduced from 180
                            {
                                barsRow.ConstantItem(60); // Reduced from 80
                                
                                // Bars area
                                barsRow.RelativeItem().Row(actualBarsRow =>
                                {
                                    // Income Bar with professional styling
                                    actualBarsRow.RelativeItem().Column(incomeBarColumn =>
                                    {
                                        incomeBarColumn.Item().AlignBottom().Height(incomeHeight)
                                            .Background("#27AE60")
                                            .Border(1).BorderColor("#229954");
                                    });

                                    actualBarsRow.ConstantItem(60); // Reduced from 80

                                    // Expense Bar with professional styling
                                    actualBarsRow.RelativeItem().Column(expenseBarColumn =>
                                    {
                                        expenseBarColumn.Item().AlignBottom().Height(expenseHeight)
                                            .Background("#E74C3C")
                                            .Border(1).BorderColor("#DC2E2E");
                                    });
                                });
                                
                                barsRow.ConstantItem(60); // Reduced from 80
                            });
                            
                            // X-axis labels with compact spacing
                            chartContent.Item().PaddingTop(10).Row(labelsRow => // Reduced from 15
                            {
                                labelsRow.ConstantItem(60); // Reduced from 80
                                
                                labelsRow.RelativeItem().Row(actualLabelsRow =>
                                {
                                    actualLabelsRow.RelativeItem().Text("Income")
                                        .FontSize(12) // Reduced from 14
                                        .AlignCenter()
                                        .FontColor("#27AE60")
                                        .Bold();

                                    actualLabelsRow.ConstantItem(60); // Reduced from 80

                                    actualLabelsRow.RelativeItem().Text("Expenses")
                                        .FontSize(12) // Reduced from 14
                                        .AlignCenter()
                                        .FontColor("#E74C3C")
                                        .Bold();
                                });
                                
                                labelsRow.ConstantItem(60); // Reduced from 80
                            });
                        });
                        
                        chartRow.ConstantItem(50); // Reduced from 60
                    });

                    // Compact Chart Legend
                    chartColumn.Item().PaddingTop(15).Row(legendRow => // Reduced from 25
                    {
                        legendRow.RelativeItem(); // Left spacer
                        
                        // Income legend
                        legendRow.ConstantItem(20).Height(14).Background("#27AE60").Border(1).BorderColor("#229954"); // Reduced from 25x18
                        legendRow.ConstantItem(8); // Reduced from 10
                        legendRow.ConstantItem(60).Text("Income").FontSize(12).FontColor("#2C3E50").Bold(); // Reduced from 14
                        
                        legendRow.ConstantItem(30); // Reduced from 40
                        
                        // Expense legend
                        legendRow.ConstantItem(20).Height(14).Background("#E74C3C").Border(1).BorderColor("#DC2E2E"); // Reduced from 25x18
                        legendRow.ConstantItem(8); // Reduced from 10
                        legendRow.ConstantItem(60).Text("Expenses").FontSize(12).FontColor("#2C3E50").Bold(); // Reduced from 14
                        
                        legendRow.RelativeItem(); // Right spacer
                    });
                });

            // Net Cash Flow Analysis with compact styling
            var isPositive = cashFlowData.NetCashFlow >= 0;
            var netCashFlowColor = isPositive ? "#27AE60" : "#E74C3C";
            var netCashFlowBgColor = isPositive ? "#D5F4E6" : "#FADBD8";
            
            column.Item().PaddingTop(20).Background(netCashFlowBgColor) // Reduced from 35
                .Border(3).BorderColor(netCashFlowColor)
                .Padding(20).Column(netCashFlowColumn => // Reduced from 30
                {
                    netCashFlowColumn.Item().Text(isPositive ? "Positive Cash Flow" : "Negative Cash Flow")
                        .FontSize(16) // Reduced from 18
                        .Bold()
                        .FontColor(netCashFlowColor)
                        .AlignCenter();
                    
                    var statusDescription = isPositive 
                        ? "Your income exceeds your expenses during this period."
                        : "Your expenses exceed your income during this period.";
                    
                    netCashFlowColumn.Item().PaddingTop(8).Text(statusDescription) // Reduced from 12
                        .FontSize(12) // Reduced from 14
                        .FontColor("#5D6D7E")
                        .AlignCenter();
                });

            // Footer note with compact styling
            column.Item().Text("Generated by Money Management System") // Reduced from 25
                .FontSize(10) // Reduced from 11
                .FontColor("#95A5A6")
                .AlignRight();
        });
    }

    /// <summary>
    /// Composes category breakdown template matching the professional table design
    /// </summary>
    private void ComposeCategoryBreakdownTemplate(IContainer container, object data, Dictionary<string, object> parameters)
    {
        var startDate = (DateTime)parameters["startDate"];
        var endDate = (DateTime)parameters["endDate"];
        var currency = parameters["currency"].ToString() ?? "VND";
        var generatedAt = (DateTime)parameters["generatedAt"];

        container.Column(column =>
        {
            column.Spacing(20);

            // Header Section with professional dark blue-gray background
            column.Item().Background("#34495E").Padding(25).Column(headerColumn =>
            {
                headerColumn.Item().Text("CATEGORY BREAKDOWN")
                    .FontSize(28)
                    .Bold()
                    .FontColor(QuestPDF.Helpers.Colors.White)
                    .AlignCenter();
                
                headerColumn.Item().PaddingTop(8).Text($"Reporting Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}")
                    .FontSize(14)
                    .FontColor("#BDC3C7")
                    .AlignCenter();
            });

            // Data Table Section
            column.Item().PaddingTop(25);

            // Handle different data types
            switch (data)
            {
                case IEnumerable<CategoryBreakdownDTO> categories:
                    var categoryList = categories.ToList();
                    if (categoryList.Any())
                    {
                        column.Item().Table(table =>
                        {
                            // Define table columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f); // Category
                                columns.RelativeColumn(2f);   // Total Income
                                columns.RelativeColumn(2f);   // Total Expense
                                columns.RelativeColumn(1.5f); // Income %
                                columns.RelativeColumn(1.5f); // Expense %
                                columns.RelativeColumn(1f);   // Currency
                            });

                            // Table Header with professional styling
                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Category")
                                    .FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                header.Cell().Element(HeaderCellStyle).Text("Total Income")
                                    .FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                header.Cell().Element(HeaderCellStyle).Text("Total Expense")
                                    .FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                header.Cell().Element(HeaderCellStyle).Text("Income %")
                                    .FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                header.Cell().Element(HeaderCellStyle).Text("Expense %")
                                    .FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                header.Cell().Element(HeaderCellStyle).Text("Currency")
                                    .FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                            });

                            // Data Rows
                            foreach (var category in categoryList)
                            {
                                table.Cell().Element(DataCellStyle).Text(category.Category)
                                    .FontSize(12).FontColor("#2C3E50");
                                
                                table.Cell().Element(DataCellStyle).Text(category.TotalIncome.ToString("N0"))
                                    .FontSize(12).FontColor(category.TotalIncome > 0 ? "#27AE60" : "#2C3E50");
                                
                                table.Cell().Element(DataCellStyle).Text(category.TotalExpense.ToString("N0"))
                                    .FontSize(12).FontColor(category.TotalExpense > 0 ? "#E74C3C" : "#2C3E50");
                                
                                table.Cell().Element(DataCellStyle).Text($"{category.IncomePercentage:F1}%")
                                    .FontSize(12).FontColor("#2C3E50");
                                
                                table.Cell().Element(DataCellStyle).Text($"{category.ExpensePercentage:F1}%")
                                    .FontSize(12).FontColor("#2C3E50");
                                
                                table.Cell().Element(DataCellStyle).Text(currency)
                                    .FontSize(12).FontColor("#2C3E50");
                            }
                        });
                    }
                    else
                    {
                        column.Item().Text("No category data available for the selected period.")
                            .FontSize(14)
                            .FontColor("#7F8C8D")
                            .AlignCenter();
                    }
                    break;

                default:
                    column.Item().Text("Invalid data format for category breakdown report.")
                        .FontSize(14)
                        .FontColor("#E74C3C")
                        .AlignCenter();
                    break;
            }

            // Footer with generation timestamp
            column.Item().PaddingTop(30).Row(footerRow =>
            {
                footerRow.RelativeItem(); // Left spacer
                footerRow.ConstantItem(300).Text($"{generatedAt:dd/MM/yyyy HH:mm}")
                    .FontSize(10)
                    .FontColor("#95A5A6")
                    .AlignRight();
            });

            column.Item().Text("Generated by Money Management System")
                .FontSize(10)
                .FontColor("#95A5A6")
                .AlignRight();
        });

        // Helper methods for table styling
        static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background("#34495E")
                .Border(1)
                .BorderColor("#2C3E50")
                .Padding(12);
        }

        static IContainer DataCellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor("#E9ECEF")
                .Padding(10);
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
    }    /// <summary>
    /// Composes weekly summary report template matching the exact design from the image
    /// </summary>
    private void ComposeWeeklySummaryTemplate(IContainer container, object data, Dictionary<string, object> parameters)
    {
        _logger.LogInformation("ComposeWeeklySummaryTemplate received data of type: {DataType}", data?.GetType().Name ?? "null");

        // Get date parameters
        DateTime startDate = (DateTime)parameters["startDate"];
        DateTime endDate = (DateTime)parameters["endDate"];

        // Extract data from DTO if available
        decimal totalIncome = 0;
        decimal totalExpenses = 0;
        decimal netCashFlow = 0;

        // Dictionary to store weekly data with week number as key
        Dictionary<string, WeeklyDetailDTO> weeklyDetailsMap = new();

        // Extract data from different possible types
        if (data is WeeklySummaryDTO weeklySummary)
        {
            totalIncome = weeklySummary.TotalIncome;
            totalExpenses = weeklySummary.TotalExpenses;
            netCashFlow = weeklySummary.NetCashFlow;

            // Convert list to dictionary for easier lookup
            if (weeklySummary.WeeklyDetails != null)
            {
                foreach (var detail in weeklySummary.WeeklyDetails)
                {
                    weeklyDetailsMap[detail.WeekNumber] = detail;
                }
            }
        }
        else
        {
            // Handle anonymous object from StatisticService or other data types
            var dataType = data?.GetType();
            if (dataType != null)
            {
                // Use reflection to extract properties from anonymous object
                var totalIncomeProperty = dataType.GetProperty("TotalIncome");
                var totalExpensesProperty = dataType.GetProperty("TotalExpenses");
                var netCashFlowProperty = dataType.GetProperty("NetCashFlow");
                var weeklyDetailsProperty = dataType.GetProperty("WeeklyDetails");

                if (totalIncomeProperty != null)
                    totalIncome = (decimal)(totalIncomeProperty.GetValue(data) ?? 0);
                if (totalExpensesProperty != null)
                    totalExpenses = (decimal)(totalExpensesProperty.GetValue(data) ?? 0);
                if (netCashFlowProperty != null)
                    netCashFlow = (decimal)(netCashFlowProperty.GetValue(data) ?? 0);

                // Extract weekly details if available
                if (weeklyDetailsProperty != null)
                {
                    var weeklyDetails = weeklyDetailsProperty.GetValue(data) as IEnumerable<WeeklyDetailDTO>;
                    if (weeklyDetails != null)
                    {
                        foreach (var detail in weeklyDetails)
                        {
                            weeklyDetailsMap[detail.WeekNumber] = detail;
                        }
                    }
                }
            }
            else
            {
                container.Text("No weekly summary data available").FontSize(12);
                return;
            }
        }

        // Generate weeks for the date range
        List<WeeklyDetailDTO> allWeeks = GenerateWeeksInRange(startDate, endDate, weeklyDetailsMap);

        var currencySymbol = parameters["currencySymbol"].ToString() ?? "₫";
        var currency = parameters["currency"].ToString() ?? "USD";
        var generatedAt = (DateTime)parameters["generatedAt"];

        container.Column(column =>
        {
            column.Spacing(15); // Reduced from 25

            // Header Section with professional dark blue-gray background - more compact
            column.Item().Background("#34495E").Padding(20).Column(headerColumn => // Reduced from 30
            {
                headerColumn.Item().Text("WEEKLY SUMMARY")
                    .FontSize(24) // Reduced from 32
                    .Bold()
                    .FontColor(QuestPDF.Helpers.Colors.White)
                    .AlignCenter();

                headerColumn.Item().PaddingTop(6).Text($"Reporting Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}") // Reduced from 10
                    .FontSize(12) // Reduced from 16
                    .FontColor("#BDC3C7")
                    .AlignCenter();
            });

            // Financial Overview Section
            column.Item().PaddingTop(20).Text("Financial Overview") // Reduced from 30
                .FontSize(16) // Reduced from 20
                .Bold()
                .FontColor("#2C3E50");

            // Summary Cards Row - More compact design
            column.Item().PaddingTop(12).Row(row => // Reduced from 20
            {
                // Total Income Card - Green styling
                row.RelativeItem().Background("#D5F4E6")
                    .Border(2).BorderColor("#27AE60")
                    .Padding(18).Column(incomeColumn => // Reduced from 25
                    {
                        incomeColumn.Item().Text("Total Income")
                            .FontSize(14) // Reduced from 16
                            .FontColor("#27AE60")
                            .Bold();
                        incomeColumn.Item().PaddingTop(8).Text($"{totalIncome:N0} {currency}") // Reduced from 12
                            .FontSize(20) // Reduced from 24
                            .FontColor("#1E8449")
                            .Bold();
                    });

                row.ConstantItem(30); // Reduced from 40

                // Total Expenses Card - Red styling
                row.RelativeItem().Background("#FADBD8")
                    .Border(2).BorderColor("#E74C3C")
                    .Padding(18).Column(expenseColumn => // Reduced from 25
                    {
                        expenseColumn.Item().Text("Total Expenses")
                            .FontSize(14) // Reduced from 16
                            .FontColor("#E74C3C")
                            .Bold();
                        expenseColumn.Item().PaddingTop(8).Text($"{totalExpenses:N0} {currency}") // Reduced from 12
                            .FontSize(20) // Reduced from 24
                            .FontColor("#C0392B")
                            .Bold();
                    });
            });

            // Weekly Breakdown Section
            column.Item().PaddingTop(30).Text("Weekly Breakdown")
                .FontSize(18)
                .Bold()
                .FontColor("#4A5568");

            // Weekly Breakdown Table matching the image design
            column.Item().PaddingTop(15).Table(table =>
            {
                // Define table columns
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);    // Week Number
                    columns.RelativeColumn(3);    // Income
                    columns.RelativeColumn(3);    // Expense
                });

                // Table Header with dark blue-gray background
                table.Header(header =>
                {
                    header.Cell().Background("#4A5568")
                        .Padding(12).Text("Week Number")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();

                    header.Cell().Background("#4A5568")
                        .Padding(12).Text("Income")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();

                    header.Cell().Background("#4A5568")
                        .Padding(12).Text("Expense")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();
                });

                // Table Rows with alternating light gray background
                bool isAlternateRow = false;
                foreach (var detail in allWeeks)
                {
                    var background = isAlternateRow ? "#F7FAFC" : "#FFFFFF";

                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(detail.WeekNumber)
                        .FontColor("#2D3748")
                        .FontSize(12);

                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(detail.Income == 0 ? "0.00" : detail.Income.ToString("F2"))
                        .FontColor(detail.Income == 0 ? "#A0AEC0" : "#48BB78")
                        .FontSize(12);

                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(detail.Expense == 0 ? "0.00" : detail.Expense.ToString("F2"))
                        .FontColor(detail.Expense == 0 ? "#A0AEC0" : "#F56565")
                        .FontSize(12);

                    isAlternateRow = !isAlternateRow;
                }
            });

            // Cash Flow Status at bottom
            var isPositive = netCashFlow >= 0;
            var cashFlowText = isPositive ? "Positive Cash Flow" : "Negative Cash Flow";

            column.Item().PaddingTop(25).Text(cashFlowText)
                .FontSize(16)
                .Bold()
                .FontColor("#4A5568")
                .AlignLeft();

            // Footer with generation timestamp and system info
            column.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem(); // Left spacer

                row.RelativeItem().AlignRight().Column(rightCol =>
                {
                    rightCol.Item().Text($"{generatedAt:dd/MM/yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor("#A0AEC0");

                    rightCol.Item().Text("Generated by Money Management System")
                        .FontSize(10)
                        .FontColor("#A0AEC0");
                });
            });
        });
    }
    /// <summary>
    /// Composes daily summary report template matching the provided design
    /// </summary>
    private void ComposeDailySummaryTemplate(IContainer container, object data, Dictionary<string, object> parameters)
    {
        _logger.LogInformation("ComposeDailySummaryTemplate received data of type: {DataType}", data?.GetType().Name ?? "null");

        // Get date parameters
        DateTime startDate = (DateTime)parameters["startDate"];
        DateTime endDate = (DateTime)parameters["endDate"];
        var date = startDate; // For daily reports, use the start date as the target date

        // Extract data from DTO if available
        decimal totalIncome = 0;
        decimal totalExpenses = 0;
        string dayOfWeek = string.Empty;
        string month = string.Empty;

        // Extract data from different possible types
        if (data is DailySummaryDTO dailySummary)
        {
            totalIncome = dailySummary.TotalIncome;
            totalExpenses = dailySummary.TotalExpenses;
            dayOfWeek = dailySummary.DayOfWeek;
            month = dailySummary.Month;
            date = dailySummary.Date;
        }
        else
        {
            // Handle anonymous object from StatisticService or other data types
            var dataType = data?.GetType();
            if (dataType != null)
            {
                // Use reflection to extract properties from anonymous object
                var totalIncomeProperty = dataType.GetProperty("TotalIncome");
                var totalExpensesProperty = dataType.GetProperty("TotalExpenses");
                var dateProperty = dataType.GetProperty("Date");
                var dayOfWeekProperty = dataType.GetProperty("DayOfWeek");
                var monthProperty = dataType.GetProperty("Month");

                if (totalIncomeProperty != null)
                    totalIncome = (decimal)(totalIncomeProperty.GetValue(data) ?? 0);
                if (totalExpensesProperty != null)
                    totalExpenses = (decimal)(totalExpensesProperty.GetValue(data) ?? 0);
                if (dateProperty != null && dateProperty.GetValue(data) is DateTime dt)
                    date = dt;
                if (dayOfWeekProperty != null)
                    dayOfWeek = dayOfWeekProperty.GetValue(data)?.ToString() ?? date.DayOfWeek.ToString();
                if (monthProperty != null)
                    month = monthProperty.GetValue(data)?.ToString() ?? date.ToString("MMMM");
            }
            else
            {
                container.Text("No daily summary data available").FontSize(12);
                return;
            }
        }

        // If day of week and month are empty, set them from the date
        if (string.IsNullOrEmpty(dayOfWeek))
            dayOfWeek = date.DayOfWeek.ToString();
        if (string.IsNullOrEmpty(month))
            month = date.ToString("MMMM");

        var currencySymbol = parameters["currencySymbol"].ToString() ?? "₫";
        var currency = parameters["currency"].ToString() ?? "VND";
        var generatedAt = (DateTime)parameters["generatedAt"];

        // Calculate net balance for status message
        var netBalance = totalIncome - totalExpenses;
        var isPositive = netBalance >= 0;

        container.Column(column =>
        {
            column.Spacing(15); // Reduced from 25

            // Header Section with professional dark blue-gray background - more compact
            column.Item().Background("#34495E").Padding(20).Column(headerColumn => // Reduced from 30
            {
                headerColumn.Item().Text("DAILY SUMMARY")
                    .FontSize(24) // Reduced from 32
                    .Bold()
                    .FontColor(QuestPDF.Helpers.Colors.White)
                    .AlignCenter();

                headerColumn.Item().PaddingTop(6).Text($"Reporting Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}") // Reduced from 10
                    .FontSize(12) // Reduced from 16
                    .FontColor("#BDC3C7")
                    .AlignCenter();
            });

            // Financial Overview Section
            column.Item().PaddingTop(20).Text("Financial Overview") // Reduced from 30
                .FontSize(16) // Reduced from 20
                .Bold()
                .FontColor("#2C3E50");

            // Summary Cards Row - More compact design
            column.Item().PaddingTop(12).Row(row => // Reduced from 20
            {
                // Total Income Card - Green styling
                row.RelativeItem().Background("#D5F4E6")
                    .Border(2).BorderColor("#27AE60")
                    .Padding(18).Column(incomeColumn => // Reduced from 25
                    {
                        incomeColumn.Item().Text($"Total Income of {date:MM/dd/yyyy}")
                            .FontSize(14) // Reduced from 16
                            .FontColor("#27AE60")
                            .Bold();
                        incomeColumn.Item().PaddingTop(8).Text($"{totalIncome:N0} {currency}") // Reduced from 12
                            .FontSize(20) // Reduced from 24
                            .FontColor("#1E8449")
                            .Bold();
                    });

                row.ConstantItem(30); // Reduced from 40

                // Total Expenses Card - Red styling
                row.RelativeItem().Background("#FADBD8")
                    .Border(2).BorderColor("#E74C3C")
                    .Padding(18).Column(expenseColumn => // Reduced from 25
                    {
                        expenseColumn.Item().Text($"Total Expenses of {date:MM/dd/yyyy}")
                            .FontSize(14) // Reduced from 16
                            .FontColor("#E74C3C")
                            .Bold();
                        expenseColumn.Item().PaddingTop(8).Text($"{totalExpenses:N0} {currency}") // Reduced from 12
                            .FontSize(20) // Reduced from 24
                            .FontColor("#C0392B")
                            .Bold();
                    });
            });

            // Large spacing block before footer
            column.Item().PaddingTop(300);

            var cashFlowText = isPositive ? "Positive Cash Flow" : "Negative Cash Flow";

            column.Item().PaddingTop(25).Text(cashFlowText)
                .FontSize(16)
                .Bold()
                .FontColor("#4A5568")
                .AlignLeft();

            // Footer with generation timestamp and system info
            column.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem(); // Left spacer

                row.RelativeItem().AlignRight().Column(rightCol =>
                {
                    rightCol.Item().Text($"{generatedAt:dd/MM/yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor("#A0AEC0");

                    rightCol.Item().Text("Generated by Money Management System")
                        .FontSize(10)
                        .FontColor("#A0AEC0");
                });
            });
        });
    }
    private List<WeeklyDetailDTO> GenerateWeeksInRange(DateTime startDate, DateTime endDate,
    Dictionary<string, WeeklyDetailDTO> existingWeeks)
    {
        var result = new List<WeeklyDetailDTO>();

        // Find the first day of the first week
        DateTime firstDay = startDate.Date;
        // Go back to the first day of the week (Monday)
        int daysToSubtract = ((int)firstDay.DayOfWeek == 0 ? 7 : (int)firstDay.DayOfWeek) - 1;
        firstDay = firstDay.AddDays(-daysToSubtract);

        // Find the last day of the last week
        DateTime lastDay = endDate.Date;
        // Go forward to the last day of the week (Sunday)
        int daysToAdd = 7 - ((int)lastDay.DayOfWeek == 0 ? 7 : (int)lastDay.DayOfWeek);
        lastDay = lastDay.AddDays(daysToAdd);

        // Generate weeks
        int weekNumber = 1;
        for (DateTime weekStart = firstDay; weekStart <= lastDay; weekStart = weekStart.AddDays(7))
        {
            DateTime weekEnd = weekStart.AddDays(6);

            // Create week identifier, format as "W#"
            string weekId = weekNumber.ToString();

            // Look for existing data or create empty week
            WeeklyDetailDTO weekData;
            if (!existingWeeks.TryGetValue(weekId, out weekData))
            {
                weekData = new WeeklyDetailDTO
                {
                    WeekNumber = weekId,
                    Income = 0,
                    Expense = 0
                };
            }

            result.Add(weekData);
            weekNumber++;
        }

        return result;
    }
    /// <summary>
    /// Composes monthly summary report template showing all 12 months of the year
    /// </summary>
    private void ComposeMonthlyTemplate(IContainer container, object data, Dictionary<string, object> parameters)
    {
        _logger.LogInformation("ComposeMonthlyTemplate received data of type: {DataType}", data?.GetType().Name ?? "null");

        // Get date parameters
        DateTime startDate = (DateTime)parameters["startDate"];
        DateTime endDate = (DateTime)parameters["endDate"];
        int year = startDate.Year; // Use the year from start date

        // Extract data from DTO or anonymous object
        decimal totalIncome = 0;
        decimal totalExpenses = 0;
        decimal netCashFlow = 0;

        // Dictionary to store monthly data with month number as key (1-12)
        var monthlyData = new Dictionary<int, (decimal Income, decimal Expense)>();

        // Initialize all months with zero values
        for (int i = 1; i <= 12; i++)
        {
            monthlyData[i] = (0m, 0m);
        }

        // Extract data using reflection for flexibility
        var dataType = data?.GetType();
        if (dataType != null)
        {
            // Extract summary values
            var totalIncomeProperty = dataType.GetProperty("TotalIncome");
            var totalExpensesProperty = dataType.GetProperty("TotalExpenses");
            var netCashFlowProperty = dataType.GetProperty("NetCashFlow");

            if (totalIncomeProperty != null)
                totalIncome = (decimal)(totalIncomeProperty.GetValue(data) ?? 0m);
            if (totalExpensesProperty != null)
                totalExpenses = (decimal)(totalExpensesProperty.GetValue(data) ?? 0m);
            if (netCashFlowProperty != null)
                netCashFlow = (decimal)(netCashFlowProperty.GetValue(data) ?? 0m);

            // Try to extract monthly breakdown data
            var monthlyBreakdownProperty = dataType.GetProperty("MonthlyBreakdown");
            if (monthlyBreakdownProperty != null)
            {
                var monthlyBreakdown = monthlyBreakdownProperty.GetValue(data) as IEnumerable<object>;
                if (monthlyBreakdown != null)
                {
                    foreach (var monthData in monthlyBreakdown)
                    {
                        var monthType = monthData.GetType();
                        var monthProperty = monthType.GetProperty("Month");
                        var incomeProperty = monthType.GetProperty("Income");
                        var expenseProperty = monthType.GetProperty("Expenses");

                        if (monthProperty != null && incomeProperty != null && expenseProperty != null)
                        {
                            int month = Convert.ToInt32(monthProperty.GetValue(monthData));
                            decimal income = (decimal)(incomeProperty.GetValue(monthData) ?? 0m);
                            decimal expense = (decimal)(expenseProperty.GetValue(monthData) ?? 0m);

                            if (month >= 1 && month <= 12)
                            {
                                monthlyData[month] = (income, expense);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            container.Text("No monthly summary data available").FontSize(12);
            return;
        }

        var currencySymbol = parameters["currencySymbol"].ToString() ?? "₫";
        var currency = parameters["currency"].ToString() ?? "VND";
        var generatedAt = (DateTime)parameters["generatedAt"];

        container.Column(column =>
        {
            column.Spacing(15);

            // Header Section with professional dark blue-gray background
            column.Item().Background("#34495E").Padding(20).Column(headerColumn =>
            {
                headerColumn.Item().Text("MONTHLY SUMMARY")
                    .FontSize(24)
                    .Bold()
                    .FontColor(QuestPDF.Helpers.Colors.White)
                    .AlignCenter();

                headerColumn.Item().PaddingTop(6).Text($"Year: {year}")
                    .FontSize(12)
                    .FontColor("#BDC3C7")
                    .AlignCenter();
            });

            // Financial Overview Section
            column.Item().PaddingTop(20).Text("Financial Overview")
                .FontSize(16)
                .Bold()
                .FontColor("#2C3E50");

            // Summary Cards Row - Green and Red styling
            column.Item().PaddingTop(12).Row(row =>
            {
                // Total Income Card - Green styling
                row.RelativeItem().Background("#D5F4E6")
                    .Border(2).BorderColor("#27AE60")
                    .Padding(18).Column(incomeColumn =>
                    {
                        incomeColumn.Item().Text("Total Income")
                            .FontSize(14)
                            .FontColor("#27AE60")
                            .Bold();
                        incomeColumn.Item().PaddingTop(8).Text($"{totalIncome:N0} {currency}")
                            .FontSize(20)
                            .FontColor("#1E8449")
                            .Bold();
                    });

                row.ConstantItem(30);

                // Total Expenses Card - Red styling
                row.RelativeItem().Background("#FADBD8")
                    .Border(2).BorderColor("#E74C3C")
                    .Padding(18).Column(expenseColumn =>
                    {
                        expenseColumn.Item().Text("Total Expenses")
                            .FontSize(14)
                            .FontColor("#E74C3C")
                            .Bold();
                        expenseColumn.Item().PaddingTop(8).Text($"{totalExpenses:N0} {currency}")
                            .FontSize(20)
                            .FontColor("#C0392B")
                            .Bold();
                    });
            });

            // Monthly Breakdown Section
            column.Item().PaddingTop(30).Text("Monthly Breakdown")
                .FontSize(18)
                .Bold()
                .FontColor("#4A5568");

            // Monthly Breakdown Table
            column.Item().PaddingTop(15).Table(table =>
            {
                // Define table columns
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);    // Month
                    columns.RelativeColumn(3);    // Income
                    columns.RelativeColumn(3);    // Expense
                });

                // Table Header with dark blue background
                table.Header(header =>
                {
                    header.Cell().Background("#2C3E50")
                        .Padding(12).Text("Month")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();

                    header.Cell().Background("#2C3E50")
                        .Padding(12).Text("Income")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();

                    header.Cell().Background("#2C3E50")
                        .Padding(12).Text("Expense")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();
                });

                // Month names
                string[] monthNames = {
                    "January", "February", "March", "April", "May", "June",
                    "July", "August", "September", "October", "November", "December"
                };

                // Table Rows for all 12 months with alternating background
                bool isAlternateRow = false;
                for (int i = 0; i < 12; i++)
                {
                    int monthNumber = i + 1;
                    var (income, expense) = monthlyData[monthNumber];
                    var background = isAlternateRow ? "#F7FAFC" : "#FFFFFF";

                    // Month name cell
                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(monthNames[i])
                        .FontColor("#2D3748")
                        .FontSize(12);

                    // Income cell - Green color for non-zero values
                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(income == 0 ? "0" : income.ToString("N0"))
                        .FontColor(income == 0 ? "#A0AEC0" : "#48BB78")
                        .FontSize(12);

                    // Expense cell - Red color for non-zero values
                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(expense == 0 ? "0" : expense.ToString("N0"))
                        .FontColor(expense == 0 ? "#A0AEC0" : "#F56565")
                        .FontSize(12);

                    isAlternateRow = !isAlternateRow;
                }
            });

            // Net Cash Flow Status
            var isPositive = netCashFlow >= 0;
            var cashFlowText = isPositive ? "Positive Cash Flow" : "Negative Cash Flow";

            column.Item().PaddingTop(25).Text(cashFlowText)
                .FontSize(16)
                .Bold()
                .FontColor(isPositive ? "#27AE60" : "#E74C3C");

            // Footer with generation timestamp and system info
            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem(); // Left spacer

                row.RelativeItem().AlignRight().Column(rightCol =>
                {
                    rightCol.Item().Text($"{generatedAt:dd/MM/yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor("#95A5A6");

                    rightCol.Item().Text("Generated by Money Management System")
                        .FontSize(10)
                        .FontColor("#95A5A6");
                });
            });
        });
    }
    /// <summary>
    /// Composes yearly summary report template showing data for years in the date range
    /// </summary>
    private void ComposeYearlySummaryTemplate(IContainer container, object data, Dictionary<string, object> parameters)
    {
        _logger.LogInformation("ComposeYearlySummaryTemplate received data of type: {DataType}", data?.GetType().Name ?? "null");

        // Get date parameters
        DateTime startDate = (DateTime)parameters["startDate"];
        DateTime endDate = (DateTime)parameters["endDate"];

        // Extract data from DTO or anonymous object
        decimal totalIncome = 0;
        decimal totalExpenses = 0;
        decimal netCashFlow = 0;

        // Dictionary to store yearly data with year as key
        var yearlyData = new Dictionary<int, (decimal Income, decimal Expense)>();

        // Extract data using reflection for flexibility
        var dataType = data?.GetType();
        if (dataType != null)
        {
            // Extract summary values
            var totalIncomeProperty = dataType.GetProperty("TotalIncome");
            var totalExpensesProperty = dataType.GetProperty("TotalExpenses");
            var netCashFlowProperty = dataType.GetProperty("NetCashFlow");
            var yearlyDetailsProperty = dataType.GetProperty("YearlyDetails");

            if (totalIncomeProperty != null)
                totalIncome = (decimal)(totalIncomeProperty.GetValue(data) ?? 0m);
            if (totalExpensesProperty != null)
                totalExpenses = (decimal)(totalExpensesProperty.GetValue(data) ?? 0m);
            if (netCashFlowProperty != null)
                netCashFlow = (decimal)(netCashFlowProperty.GetValue(data) ?? 0m);

            // Extract yearly details if available
            if (yearlyDetailsProperty != null)
            {
                var yearlyDetails = yearlyDetailsProperty.GetValue(data) as IEnumerable<object>;
                if (yearlyDetails != null)
                {
                    foreach (var yearData in yearlyDetails)
                    {
                        var yearType = yearData.GetType();
                        var yearProperty = yearType.GetProperty("Year");
                        var incomeProperty = yearType.GetProperty("Income");
                        var expenseProperty = yearType.GetProperty("Expense");

                        if (yearProperty != null && incomeProperty != null && expenseProperty != null)
                        {
                            // Parse year string to int
                            if (int.TryParse(yearProperty.GetValue(yearData)?.ToString(), out int yearValue))
                            {
                                decimal income = (decimal)(incomeProperty.GetValue(yearData) ?? 0m);
                                decimal expense = (decimal)(expenseProperty.GetValue(yearData) ?? 0m);
                                yearlyData[yearValue] = (income, expense);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            container.Text("No yearly summary data available").FontSize(12);
            return;
        }

        // Make sure we have entries for all years in the range
        for (int year = startDate.Year; year <= endDate.Year; year++)
        {
            if (!yearlyData.ContainsKey(year))
            {
                yearlyData[year] = (0m, 0m);
            }
        }

        var currencySymbol = parameters["currencySymbol"].ToString() ?? "₫";
        var currency = parameters["currency"].ToString() ?? "VND";
        var generatedAt = (DateTime)parameters["generatedAt"];

        container.Column(column =>
        {
            column.Spacing(15);

            // Header Section with professional dark blue-gray background
            column.Item().Background("#34495E").Padding(20).Column(headerColumn =>
            {
                headerColumn.Item().Text("YEARLY SUMMARY")
                    .FontSize(24)
                    .Bold()
                    .FontColor(QuestPDF.Helpers.Colors.White)
                    .AlignCenter();

                // Show date range in header
                string periodText = startDate.Year == endDate.Year
                    ? $"Year: {startDate.Year}"
                    : $"Period: {startDate.Year} - {endDate.Year}";

                headerColumn.Item().PaddingTop(6).Text(periodText)
                    .FontSize(12)
                    .FontColor("#BDC3C7")
                    .AlignCenter();
            });

            // Financial Overview Section
            column.Item().PaddingTop(20).Text("Financial Overview")
                .FontSize(16)
                .Bold()
                .FontColor("#2C3E50");

            // Summary Cards Row - Green and Red styling
            column.Item().PaddingTop(12).Row(row =>
            {
                // Total Income Card - Green styling
                row.RelativeItem().Background("#D5F4E6")
                    .Border(2).BorderColor("#27AE60")
                    .Padding(18).Column(incomeColumn =>
                    {
                        incomeColumn.Item().Text("Total Income")
                            .FontSize(14)
                            .FontColor("#27AE60")
                            .Bold();
                        incomeColumn.Item().PaddingTop(8).Text($"{totalIncome:N0} {currency}")
                            .FontSize(20)
                            .FontColor("#1E8449")
                            .Bold();
                    });

                row.ConstantItem(30);

                // Total Expenses Card - Red styling
                row.RelativeItem().Background("#FADBD8")
                    .Border(2).BorderColor("#E74C3C")
                    .Padding(18).Column(expenseColumn =>
                    {
                        expenseColumn.Item().Text("Total Expenses")
                            .FontSize(14)
                            .FontColor("#E74C3C")
                            .Bold();
                        expenseColumn.Item().PaddingTop(8).Text($"{totalExpenses:N0} {currency}")
                            .FontSize(20)
                            .FontColor("#C0392B")
                            .Bold();
                    });
            });

            // Yearly Breakdown Section
            column.Item().PaddingTop(30).Text("Yearly Breakdown")
                .FontSize(18)
                .Bold()
                .FontColor("#4A5568");

            // Yearly Breakdown Table
            column.Item().PaddingTop(15).Table(table =>
            {
                // Define table columns
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);    // Year
                    columns.RelativeColumn(3);    // Income
                    columns.RelativeColumn(3);    // Expense
                });

                // Table Header with dark blue background
                table.Header(header =>
                {
                    header.Cell().Background("#2C3E50")
                        .Padding(12).Text("Year")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();

                    header.Cell().Background("#2C3E50")
                        .Padding(12).Text("Income")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();

                    header.Cell().Background("#2C3E50")
                        .Padding(12).Text("Expense")
                        .FontColor(QuestPDF.Helpers.Colors.White)
                        .FontSize(14)
                        .Bold();
                });

                // Table Rows for each year in the range with alternating background
                bool isAlternateRow = false;
                // Sort years in ascending order
                var sortedYears = yearlyData.Keys.OrderBy(y => y).ToList();

                foreach (var year in sortedYears)
                {
                    var (income, expense) = yearlyData[year];
                    var background = isAlternateRow ? "#F7FAFC" : "#FFFFFF";

                    // Year cell
                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(year.ToString())
                        .FontColor("#2D3748")
                        .FontSize(12);

                    // Income cell - Green color for non-zero values
                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(income == 0 ? "0" : income.ToString("N0"))
                        .FontColor(income == 0 ? "#A0AEC0" : "#48BB78")
                        .FontSize(12);

                    // Expense cell - Red color for non-zero values
                    table.Cell().Background(background)
                        .Border(1).BorderColor("#E2E8F0")
                        .Padding(12).Text(expense == 0 ? "0" : expense.ToString("N0"))
                        .FontColor(expense == 0 ? "#A0AEC0" : "#F56565")
                        .FontSize(12);

                    isAlternateRow = !isAlternateRow;
                }
            });

            // Net Cash Flow Status
            var isPositive = netCashFlow >= 0;
            var cashFlowText = isPositive ? "Positive Cash Flow" : "Negative Cash Flow";

            column.Item().PaddingTop(25).Text(cashFlowText)
                .FontSize(16)
                .Bold()
                .FontColor(isPositive ? "#27AE60" : "#E74C3C");

            // Footer with generation timestamp and system info
            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem(); // Left spacer

                row.RelativeItem().AlignRight().Column(rightCol =>
                {
                    rightCol.Item().Text($"{generatedAt:dd/MM/yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor("#95A5A6");

                    rightCol.Item().Text("Generated by Money Management System")
                        .FontSize(10)
                        .FontColor("#95A5A6");
                });
            });
        });
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
