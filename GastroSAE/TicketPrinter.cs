using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Text;

namespace GastroSAE
{
    public sealed class TicketLineItem
    {
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public decimal Importe { get; set; }
    }

    public sealed class TicketDocumentData
    {
        public string Negocio { get; set; } = string.Empty;
        public string RfcNegocio { get; set; } = string.Empty;
        public string TiendaNumero { get; set; } = string.Empty;
        public List<string> DomicilioFiscalLineas { get; set; } = new();
        public string CveDoc { get; set; } = string.Empty;
        public string FolioTexto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Hora { get; set; } = string.Empty;
        public string AtendidoPor { get; set; } = string.Empty;
        public string Mesa { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public decimal Efectivo { get; set; }
        public decimal Cambio { get; set; }
        public string FormaPagoTexto { get; set; } = string.Empty;
        public List<TicketLineItem> Lineas { get; set; } = new();
    }

    public static class TicketPrinter
    {
        public static bool TryPrint(TicketDocumentData ticket, string printerName, int widthMm, out string? error)
        {
            error = null;
            try
            {
                using var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                if (!pd.PrinterSettings.IsValid)
                {
                    error = $"La impresora '{printerName}' no está disponible.";
                    return false;
                }

                var lines = BuildTicketLines(ticket, widthMm);
                var printableMm = CalibratedPrintableMm(widthMm);
                var widthHundredths = MmToHundredths(printableMm);
                var lineHeightPx = widthMm <= 58 ? 17 : widthMm <= 63 ? 18 : widthMm <= 70 ? 19 : 20;
                var heightHundredths = Math.Max(700, (lines.Count + 6) * lineHeightPx);
                pd.DefaultPageSettings.PaperSize = new PaperSize($"Ticket {widthMm}mm", widthHundredths, heightHundredths);
                pd.DefaultPageSettings.Margins = new Margins(2, 2, 3, 3);
                pd.PrintController = new StandardPrintController();

                pd.PrintPage += (s, e) =>
                {
                    e.Graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                    try { e.Graphics.TextContrast = 12; } catch { }
                    using var fontNormal = new Font("Arial", widthMm <= 58 ? 7.2f : widthMm <= 63 ? 7.7f : widthMm <= 70 ? 8.0f : 8.4f, FontStyle.Regular, GraphicsUnit.Point);
                    using var fontBold = new Font("Arial", widthMm <= 58 ? 8.0f : widthMm <= 63 ? 8.5f : widthMm <= 70 ? 8.9f : 9.3f, FontStyle.Bold, GraphicsUnit.Point);
                    using var fontTitle = new Font("Arial", widthMm <= 58 ? 7.4f : widthMm <= 63 ? 7.9f : widthMm <= 70 ? 8.3f : 8.7f, FontStyle.Bold, GraphicsUnit.Point);
                    float y = e.MarginBounds.Top;
                    float lineH = fontNormal.GetHeight(e.Graphics) + (widthMm <= 58 ? 0f : 1f);
                    int businessLineCount = 0;
                    while (businessLineCount < lines.Count && !string.IsNullOrWhiteSpace(lines[businessLineCount]))
                        businessLineCount++;

                    for (int i = 0; i < lines.Count; i++)
                    {
                        var line = lines[i] ?? string.Empty;

                        if (i < businessLineCount)
                        {
                            y += DrawCenteredFittedLine(e.Graphics, line.Trim(), fontTitle, e.MarginBounds.Left, y, e.MarginBounds.Width);
                            y += 1f;
                            continue;
                        }

                        e.Graphics.DrawString(line, fontNormal, Brushes.Black, e.MarginBounds.Left, y);
                        y += lineH + 1f;
                    }
                    e.HasMorePages = false;
                };

                pd.Print();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static List<string> BuildTicketLines(TicketDocumentData t, int widthMm)
        {
            var w = WidthChars(widthMm);
            var sb = new List<string>();
            foreach (var ln in WrapAndCenter((t.Negocio ?? string.Empty).Trim().ToUpperInvariant(), TitleWidthChars(widthMm)))
                sb.Add(ln);
            sb.Add(string.Empty);
            sb.Add("Domicilio fiscal:");
            foreach (var line in t.DomicilioFiscalLineas ?? new List<string>())
                foreach (var wrap in Wrap(line, w)) sb.Add(wrap);
            if (!string.IsNullOrWhiteSpace(t.RfcNegocio))
                sb.Add($"RFC: {t.RfcNegocio.Trim()}");
            sb.Add(new string('-', w));
            sb.Add(LeftRight("Tienda num.", string.IsNullOrWhiteSpace(t.TiendaNumero) ? "1" : t.TiendaNumero, w));
            sb.Add(LeftRight("Nota no.", t.FolioTexto, w));
            sb.Add(LeftRight("Fecha", t.Fecha.ToString("dd/MM/yyyy"), w));
            sb.Add(LeftRight("Hora", t.Hora, w));
            sb.Add(LeftRight("Atendido por", string.IsNullOrWhiteSpace(t.AtendidoPor) ? "ADMINISTRADOR" : t.AtendidoPor, w));
            if (!string.IsNullOrWhiteSpace(t.Mesa))
                sb.Add(LeftRight("Mesa", t.Mesa, w));
            sb.Add(new string('-', w));
            foreach (var header in ColsHeader(widthMm, w)) sb.Add(header);
            sb.Add(new string('-', w));

            int qtyW = widthMm <= 58 ? 5 : 6;
            int descW = Math.Max(widthMm <= 58 ? 16 : 18, w - qtyW - 1);
            int detailW = Math.Max(widthMm <= 58 ? 16 : 18, w - (qtyW + 1));
            foreach (var item in t.Lineas)
            {
                var qty = item.Cantidad.ToString("0.000").PadLeft(qtyW);
                var descLines = Wrap(item.Descripcion, descW);
                if (descLines.Count == 0) descLines.Add(string.Empty);
                sb.Add(qty + " " + descLines[0]);
                for (int i = 1; i < descLines.Count; i++)
                    sb.Add(new string(' ', qtyW + 1) + descLines[i]);

                if (widthMm <= 58)
                {
                    sb.Add(new string(' ', qtyW + 1) + LeftRight("PU:" + Money(item.PrecioUnitario), "IMP:" + Money(item.Importe), detailW));
                }
                else
                {
                    sb.Add(new string(' ', qtyW + 1) + LeftRight("P.Unit: " + Money(item.PrecioUnitario), "Imp.: " + Money(item.Importe), detailW));
                }

                sb.Add(string.Empty);
            }

            sb.Add(new string('-', w));
            sb.Add(LeftRight("SubTotal", Money(t.Subtotal), w));
            sb.Add(LeftRight("Descuento", Money(t.Descuento), w));
            sb.Add(LeftRight("Impuesto", Money(t.Impuesto), w));
            sb.Add(LeftRight("Total", Money(t.Total), w));
            var pagoLbl = GetPagoLabel(t.FormaPagoTexto);
            sb.Add(LeftRight(pagoLbl, Money(t.Efectivo <= 0 ? t.Total : t.Efectivo), w));
            if (t.Cambio > 0)
                sb.Add(LeftRight("Cambio", Money(t.Cambio), w));
            sb.Add(string.Empty);
            foreach (var line in Wrap(NumeroALetrasPesos(t.Total), w)) sb.Add(line);
            sb.Add("GRACIAS POR SU COMPRA!!");
            sb.Add(string.Empty);
            sb.Add(string.Empty);
            return sb;
        }

        private static IEnumerable<string> ColsHeader(int widthMm, int width)
        {
            if (widthMm <= 58)
            {
                yield return "Cant. Descripción";
                yield return new string(' ', 6) + LeftRight("PU", "IMP", Math.Max(8, width - 6));
            }
            else
            {
                yield return LeftRight("Cant.  Descripción del producto", "Importe", width);
                yield return new string(' ', 7) + "Precio unitario";
            }
        }

        private static float DrawCenteredFittedLine(Graphics graphics, string text, Font baseFont, float x, float y, float width)
        {
            if (string.IsNullOrWhiteSpace(text))
                return baseFont.GetHeight(graphics);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoClip
            };

            Font? workingFont = null;
            try
            {
                float size = baseFont.Size;
                while (size > 6.2f)
                {
                    workingFont?.Dispose();
                    workingFont = new Font(baseFont.FontFamily, size, baseFont.Style, GraphicsUnit.Point);
                    var measured = graphics.MeasureString(text, workingFont, new SizeF(width, 1000f), sf);
                    if (measured.Width <= width)
                        break;
                    size -= 0.2f;
                }

                workingFont ??= new Font(baseFont.FontFamily, 6.2f, baseFont.Style, GraphicsUnit.Point);
                var finalSize = graphics.MeasureString(text, workingFont, new SizeF(width, 1000f), sf);
                graphics.DrawString(text, workingFont, Brushes.Black, new RectangleF(x, y, width, finalSize.Height + 2f), sf);
                return finalSize.Height;
            }
            finally
            {
                workingFont?.Dispose();
            }
        }

        private static int TitleWidthChars(int mm) => Math.Max(16, WidthChars(mm) - (mm <= 58 ? 6 : mm <= 63 ? 5 : mm <= 70 ? 4 : 4));

        private static string GetPagoLabel(string? formaPago)
        {
            return (formaPago ?? string.Empty).Trim() switch
            {
                "01" => "Efectivo",
                "02" => "Cheque",
                "03" => "Transferencia",
                _ => "Pago"
            };
        }

        private static string Money(decimal amount) => "$" + amount.ToString("0.00");

        private static IEnumerable<string> WrapAndCenter(string text, int width)
        {
            foreach (var line in Wrap(text, width))
                yield return line.PadLeft((width + line.Length) / 2).PadRight(width);
        }

        private static string LeftRight(string left, string right, int width)
        {
            left ??= string.Empty;
            right ??= string.Empty;
            if (left.Length + right.Length + 1 > width)
                left = left[..Math.Max(0, width - right.Length - 1)];
            return left + new string(' ', Math.Max(1, width - left.Length - right.Length)) + right;
        }

        private static List<string> Wrap(string? text, int width)
        {
            var s = (text ?? string.Empty).Trim();
            var lines = new List<string>();
            if (string.IsNullOrEmpty(s))
            {
                lines.Add(string.Empty);
                return lines;
            }

            while (s.Length > width)
            {
                int cut = s.LastIndexOf(' ', Math.Min(width, s.Length - 1), Math.Min(width, s.Length));
                if (cut <= 0) cut = width;
                lines.Add(s[..cut].TrimEnd());
                s = s[cut..].TrimStart();
            }
            if (s.Length > 0) lines.Add(s);
            return lines;
        }

        public static int WidthChars(int mm) => mm switch
        {
            <= 58 => 30,
            <= 63 => 34,
            <= 70 => 39,
            _ => 43
        };

        private static int CalibratedPrintableMm(int mm) => mm switch
        {
            <= 58 => 53,
            <= 63 => 59,
            <= 70 => 67,
            _ => 76
        };

        private static int MmToHundredths(int mm) => (int)Math.Round(mm / 25.4 * 100.0);

        public static string NumeroALetrasPesos(decimal importe)
        {
            long enteros = (long)Math.Floor(importe);
            int centavos = (int)Math.Round((importe - enteros) * 100m, 0);
            return $"{NumeroALetras(enteros)} PESOS {centavos:00}/100 M.N.";
        }

        private static string NumeroALetras(long valor)
        {
            if (valor == 0) return "CERO";
            if (valor < 0) return "MENOS " + NumeroALetras(Math.Abs(valor));
            if (valor <= 15) return new[] { "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE" }[valor];
            if (valor < 20) return "DIECI" + NumeroALetras(valor - 10).ToLowerInvariant();
            if (valor == 20) return "VEINTE";
            if (valor < 30) return "VEINTI" + NumeroALetras(valor - 20).ToLowerInvariant();
            if (valor < 100)
            {
                string[] decenas = { "", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
                var d = valor / 10; var r = valor % 10;
                return r == 0 ? decenas[d] : decenas[d] + " Y " + NumeroALetras(r);
            }
            if (valor == 100) return "CIEN";
            if (valor < 200) return "CIENTO " + NumeroALetras(valor - 100);
            if (valor < 300) return "DOSCIENTOS " + NumeroALetras(valor - 200);
            if (valor < 400) return "TRESCIENTOS " + NumeroALetras(valor - 300);
            if (valor < 500) return "CUATROCIENTOS " + NumeroALetras(valor - 400);
            if (valor < 600) return "QUINIENTOS " + NumeroALetras(valor - 500);
            if (valor < 700) return "SEISCIENTOS " + NumeroALetras(valor - 600);
            if (valor < 800) return "SETECIENTOS " + NumeroALetras(valor - 700);
            if (valor < 900) return "OCHOCIENTOS " + NumeroALetras(valor - 800);
            if (valor < 1000) return "NOVECIENTOS " + NumeroALetras(valor - 900);
            if (valor == 1000) return "MIL";
            if (valor < 2000) return "MIL " + NumeroALetras(valor - 1000);
            if (valor < 1000000)
            {
                var miles = valor / 1000; var resto = valor % 1000;
                var pref = miles == 1 ? "MIL" : NumeroALetras(miles) + " MIL";
                return resto == 0 ? pref : pref + " " + NumeroALetras(resto);
            }
            if (valor == 1000000) return "UN MILLON";
            if (valor < 2000000) return "UN MILLON " + NumeroALetras(valor - 1000000);
            if (valor < 1000000000000)
            {
                var millones = valor / 1000000; var resto = valor % 1000000;
                var pref = NumeroALetras(millones) + " MILLONES";
                return resto == 0 ? pref : pref + " " + NumeroALetras(resto);
            }
            return valor.ToString();
        }
    }
}
