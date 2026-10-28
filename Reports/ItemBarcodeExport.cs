using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using tuipos.Data;
using tuipos.UI;

namespace tuipos.Reports;

public record ExportedFile(string Path, int Count);

// The catalog as a PDF with a scannable barcode for each item. It's meant
// for an office printer, so it skips the spool: spooled files are plain
// text for the Okidata.
public static class ItemBarcodeExport
{
    public const string Id = "ITMBAR";
    public const string Title = "Item Barcodes";

    // One bar width is a point (about 14 mils), which most scanners read.
    private const int BarHeight = 36;
    private const float BarcodeColumnWidth = 180;

    public static ExportedFile Export(PosDbContext db, string userId, string folder)
    {
        var items = db.Items.AsNoTracking().OrderBy(i => i.Name).ToList();
        var created = DateTime.Now;

        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"{Id}-{created:yyyyMMdd-HHmmss}.pdf");

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Element(header => Header(header, userId, created));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(BarcodeColumnWidth);
                    });

                    // Repeated at the top of every page.
                    table.Header(header =>
                    {
                        header.Cell().Element(HeadingCell).Text("Item").Bold();
                        header.Cell().Element(HeadingCell).Text("Barcode").Bold();
                    });

                    foreach (var item in items)
                    {
                        var barcode = Barcode.Code128(item.Barcode, BarHeight);

                        table.Cell().Element(DetailCell).AlignMiddle().Text(item.Name);
                        table.Cell().Element(DetailCell).Column(column =>
                        {
                            column.Item().Width(barcode.Width).Height(BarHeight).Svg(barcode.Svg);
                            column.Item().Width(barcode.Width).PaddingTop(2).AlignCenter().Text(item.Barcode).FontSize(9);
                        });
                    }

                    if (items.Count == 0)
                    {
                        table.Cell().ColumnSpan(2).Element(DetailCell).Text("(No items)");
                    }
                });
            });
        })
        .WithMetadata(new DocumentMetadata
        {
            Title = Title,
            Author = userId,
            Creator = GreenScreen.SystemName
        })
        .GeneratePdf(path);

        return new ExportedFile(path, items.Count);
    }

    // The same heading as the text reports: report ID, title and page, then
    // system, user and timestamp.
    private static void Header(IContainer container, string userId, DateTime created)
    {
        container.PaddingBottom(12).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(Id);
                row.RelativeItem().AlignCenter().Text(Title).Bold().FontSize(12);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(GreenScreen.SystemName);
                row.RelativeItem().AlignRight().Text($"User {userId}   {GreenScreen.Timestamp(created)}");
            });
        });
    }

    private static IContainer HeadingCell(IContainer container)
    {
        return container.BorderBottom(1).PaddingVertical(4);
    }

    // ShowEntire keeps a barcode from being split across two pages.
    private static IContainer DetailCell(IContainer container)
    {
        return container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(8).ShowEntire();
    }
}
