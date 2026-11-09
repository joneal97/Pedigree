//Copyright (c) November 6, 2026 Jeremiah Burke O'Neal
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Pedigree
{
    internal static class SchoolPosterPdf
    {
        private static string F(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Safe(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormKD);
            var b = new StringBuilder();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (c == '\\' || c == '(' || c == ')') b.Append('\\');
                if (c is >= ' ' and <= '~') b.Append(c);
                else if (c is '–' or '—') b.Append('-');
                else if (c is '’' or '‘') b.Append('\'');
                else if (c is '“' or '”') b.Append('"');
                else if (c is '\n' or '\r' or '\t') b.Append(' ');
                else if (c > 127) b.Append('?');
            }
            return b.ToString();
        }
        private static void Rect(StringBuilder b, double x, double y, double w, double h, double r, double g, double bl)
        { b.AppendLine($"{F(r)} {F(g)} {F(bl)} rg {F(x)} {F(y)} {F(w)} {F(h)} re f"); }
        private static void Line(StringBuilder b, double x1, double y1, double x2, double y2, double r, double g, double bl)
        { b.AppendLine($"{F(r)} {F(g)} {F(bl)} RG 2 w {F(x1)} {F(y1)} m {F(x2)} {F(y2)} l S"); }
        private static void Text(StringBuilder b, double x, double y, string txt, int size = 12, bool bold = false)
        {
            b.AppendLine($"0.17 0.26 0.40 rg BT /{(bold ? "F2" : "F1")} {size} Tf 1 0 0 1 {F(x)} {F(y)} Tm ({Safe(txt)}) Tj ET");
        }
        private static string Clip(string input, int max) => input.Length <= max ? input : input[..Math.Max(0, max - 3)] + "...";
        private static void Card(StringBuilder b, double x, double y, double w, double h, string title, string subtitle, double r, double g, double bl)
        {
            Rect(b, x, y, w, h, r, g, bl);
            Rect(b, x + 3, y + 3, w - 6, h - 6, 1, 1, 1);
            Text(b, x + 12, y + h - 27, Clip(title, Math.Max(12, (int)(w / 9))), 15, true);
            Text(b, x + 12, y + h - 49, Clip(subtitle, Math.Max(12, (int)(w / 7))), 10);
        }
        public static void Export(string path, string name, string tg, string details,
            IReadOnlyList<string> parents, IReadOnlyList<string> children, IReadOnlyList<string> coparents)
        {
            // Letter, landscape: 792 x 612 points.
            var b = new StringBuilder();
            Rect(b, 0, 0, 792, 612, 0.97, 0.985, 1);
            Rect(b, 0, 548, 792, 64, 0.83, 0.90, 0.99);
            Text(b, 35, 570, "OUR FAMILY TREE", 28, true);
            Text(b, 525, 575, "Family History Project", 12);
            Text(b, 35, 531, "Parents", 15, true);
            var visibleParents = parents.Take(3).ToList();
            for (int i = 0; i < visibleParents.Count; i++)
                Card(b, 35 + i * 247, 425, 230, 90, visibleParents[i], "Parent", 0.82, 0.91, 0.84);
            if (parents.Count == 0) Card(b, 35, 425, 230, 90, "Not recorded", "Parent information", 0.91, 0.94, 0.96);
            Line(b, 396, 425, 396, 396, 0.38, 0.57, 0.48);
            Card(b, 239, 292, 314, 104, name, string.IsNullOrWhiteSpace(tg) ? "Family member" : "Family ID: " + tg, 0.98, 0.89, 0.64);
            if (coparents.Count > 0) Text(b, 35, 274, Clip("Other parent of child: " + string.Join(", ", coparents), 108), 10);
            Line(b, 396, 292, 396, 257, 0.38, 0.57, 0.48);
            Text(b, 35, 246, "Children", 15, true);
            var visibleChildren = children.Take(6).ToList();
            for (int i = 0; i < visibleChildren.Count; i++)
                Card(b, 35 + (i % 3) * 247, 152 - (i / 3) * 75, 230, 68, visibleChildren[i], "Child", 0.83, 0.90, 0.99);
            if (children.Count == 0) Card(b, 35, 152, 230, 68, "Not recorded", "Children information", 0.91, 0.94, 0.96);
            Rect(b, 26, 13, 740, 51, 0.95, 0.95, 0.90);
            Text(b, 35, 47, "FAMILY NOTES", 10, true);
            Text(b, 35, 29, Clip(string.IsNullOrWhiteSpace(details) ? "No original notes recorded." : details, 117), 9);
            if (parents.Count > 3 || children.Count > 6)
                Text(b, 475, 7, "Additional relatives are in the JSON family tree.", 8);
            // PDF content streams start with literal graphics operators. No network calls or NuGet packages.
            byte[] content = Encoding.ASCII.GetBytes(b.ToString());
            var objects = new List<byte[]>();
            void Add(string str) => objects.Add(Encoding.ASCII.GetBytes(str));
            Add("<< /Type /Catalog /Pages 2 0 R >>");
            Add("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 792 612] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>");
            Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
            using (var ms = new MemoryStream())
            {
                byte[] header = Encoding.ASCII.GetBytes("%PDF-1.4\n"); ms.Write(header);
                var offsets = new List<long> { 0 };
                for (int i = 0; i < 6; i++)
                {
                    offsets.Add(ms.Position);
                    byte[] prefix = Encoding.ASCII.GetBytes($"{i + 1} 0 obj\n"); ms.Write(prefix);
                    if (i == 5)
                    {
                        byte[] streamHeader = Encoding.ASCII.GetBytes($"<< /Length {content.Length} >>\nstream\n"); ms.Write(streamHeader);
                        ms.Write(content);
                        ms.Write(Encoding.ASCII.GetBytes("\nendstream\n"));
                    }
                    else ms.Write(objects[i]);
                    ms.Write(Encoding.ASCII.GetBytes("\nendobj\n"));
                }
                long xref = ms.Position;
                ms.Write(Encoding.ASCII.GetBytes("xref\n0 7\n0000000000 65535 f \n"));
                foreach (long offset in offsets.Skip(1)) ms.Write(Encoding.ASCII.GetBytes($"{offset:0000000000} 00000 n \n"));
                ms.Write(Encoding.ASCII.GetBytes($"trailer\n<< /Size 7 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"));
                File.WriteAllBytes(path, ms.ToArray());
            }
        }
    }
}
