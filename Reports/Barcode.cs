using System.Globalization;
using System.Text;
using ZXing.OneD;

namespace tuipos.Reports;

// A barcode drawn as SVG, so it stays sharp at any size in a PDF. Width is
// in bar widths (modules), including the blank margin on each side.
public record BarcodeSvg(string Svg, int Width);

public static class Barcode
{
    // Scanners need a blank margin at least 10 bar widths wide on each side.
    private const int QuietZone = 10;

    // Code 128 holds any barcode, where UPC and EAN need a valid check digit
    // (the seeded barcodes don't have one). One SVG unit is one bar width.
    public static BarcodeSvg Code128(string value, int height)
    {
        bool[] bars = new Code128Writer().encode(value);
        int width = bars.Length + QuietZone * 2;

        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");

        // One rectangle per run of black modules.
        for (int start = 0; start < bars.Length; start++)
        {
            if (!bars[start])
            {
                continue;
            }

            int end = start;

            while (end + 1 < bars.Length && bars[end + 1])
            {
                end++;
            }

            svg.Append(CultureInfo.InvariantCulture,
                $"<rect x=\"{QuietZone + start}\" y=\"0\" width=\"{end - start + 1}\" height=\"{height}\" fill=\"#000\"/>");

            start = end;
        }

        svg.Append("</svg>");

        return new BarcodeSvg(svg.ToString(), width);
    }
}
