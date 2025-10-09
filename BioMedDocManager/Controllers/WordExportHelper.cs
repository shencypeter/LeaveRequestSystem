using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// 匯出Word工具(使用「開發人員」-「控制項」元件處理)
/// </summary>
public static class WordExportHelper
{
    /// <summary>
    /// 取得指定 <see cref="SdtElement"/> 的內容容器 (Content)，
    /// 依實際型態回傳 <see cref="SdtContentRun"/>、<see cref="SdtContentBlock"/>、<see cref="SdtContentCell"/> 或 <see cref="SdtContentRow"/>。
    /// </summary>
    /// <param name="sdt">
    /// 控制項元素 (Structured Document Tag, SDT)，
    /// 例如純文字控制項、重複項控制項、表格列控制項等。
    /// </param>
    /// <returns>
    /// 對應的內容容器 <see cref="OpenXmlElement"/>，
    /// 若無對應內容則傳回 <c>null</c>。
    /// </returns>
    private static OpenXmlElement GetSdtContent(SdtElement sdt) =>
    sdt.GetFirstChild<SdtContentRun>() as OpenXmlElement ??
    sdt.GetFirstChild<SdtContentBlock>() as OpenXmlElement ??
    sdt.GetFirstChild<SdtContentCell>() as OpenXmlElement ??
    sdt.GetFirstChild<SdtContentRow>() as OpenXmlElement;

    /// <summary>
    /// 只用 Tag 比對（請在內容控制的「標記(Tag)」填入鍵名）
    /// </summary>
    /// <param name="sdt">控制項</param>
    /// <param name="key">標籤名稱</param>
    /// <returns></returns>
    private static bool MatchByTag(SdtElement sdt, string key)
    {
        var p = sdt?.SdtProperties;
        var tag = p?.GetFirstChild<Tag>()?.Val?.Value;
        return !string.IsNullOrEmpty(tag)
            && string.Equals(tag, key, StringComparison.Ordinal);
    }

    /// <summary>
    /// 批次寫多個Tag
    /// </summary>
    /// <param name="scope">作用範圍</param>
    /// <param name="values">資料</param>
    /// <returns></returns>
    public static int SetManyByTag(OpenXmlElement scope, IDictionary<string, string> values)
    {
        int hits = 0;
        foreach (var kv in values)
            if (SetTextByTag(scope, kv.Key, kv.Value) > 0) hits++;
        return hits;
    }



    /// <summary>
    /// 依 Tag 設文字；Run/Block/Cell 正確放置，避免破壞結構
    /// </summary>
    /// <param name="scope">作用範圍</param>
    /// <param name="tag">標籤名稱</param>
    /// <param name="value">資料</param>
    /// <returns></returns>
    public static int SetTextByTag(OpenXmlElement scope, string tag, string value)
    {
        var sdts = scope.Descendants<SdtElement>()
                    .Where(x => MatchByTag(x, tag))
                    .ToList();
        if (sdts.Count == 0) return 0;

        int hits = 0;
        foreach (var sdt in sdts)
        {
            var content = GetSdtContent(sdt);
            if (content == null || content is SdtContentRow) continue;

            // 準備 Run（沿用第一個 Run 的樣式）
            var run = BuildRunLike(content, new Text(value ?? string.Empty)
            {
                Space = SpaceProcessingModeValues.Preserve
            });

            // 統一把 value 切行（保留空行）
            string[] lines = (value ?? string.Empty)
                .Replace("\r\n", "\n").Replace("\r", "\n")
                .Split(new[] { "\n" }, StringSplitOptions.None);

            if (content is SdtContentRun)
            {
                // 【行內 RichText】——只動「第一個 Run」的 Text/Break，保留 rPr 與周邊結構
                var firstRun = content.Elements<Run>().FirstOrDefault()
                               ?? new Run(); // 若沒有就新建一個乾淨的 Run
                if (firstRun.Parent == null) content.Append(firstRun);

                // 移除同層其餘 Run（避免樣板殘字）
                foreach (var extra in content.Elements<Run>().Skip(1).ToList())
                    extra.Remove();

                // 清掉這個 Run 內既有 Text/Break，但保留 RunProperties
                var rPr = firstRun.RunProperties?.CloneNode(true) as RunProperties;
                firstRun.RemoveAllChildren<Text>();
                firstRun.RemoveAllChildren<Break>();
                if (rPr != null) firstRun.RunProperties = rPr;

                // 依行數重新塞入 Text + Break（都在同一個 Run 裡）
                for (int i = 0; i < lines.Length; i++)
                {
                    firstRun.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
                    if (i < lines.Length - 1)
                        firstRun.AppendChild(new Break());
                }
            }
            else
            {
                // 【Block/Cell RichText】——不重建段落；依舊只動「第一個段落的第一個 Run」
                var para = content.Elements<Paragraph>().FirstOrDefault();
                if (para == null)
                {
                    // 若真的沒有段落，才新建一個；並盡量沿用祖先段落的 pPr
                    var pPr = sdt.Ancestors<Paragraph>().FirstOrDefault()?.ParagraphProperties?.CloneNode(true) as ParagraphProperties;
                    para = new Paragraph();
                    if (pPr != null) para.ParagraphProperties = pPr;
                    content.Append(para);
                }

                // 目標 Run：沿用第一個 Run（保留 rPr）
                var firstRun = para.Elements<Run>().FirstOrDefault() ?? new Run();
                if (firstRun.Parent == null) para.Append(firstRun);

                // 移除同段落其餘 Run（避免樣板殘字）
                foreach (var extra in para.Elements<Run>().Skip(1).ToList())
                    extra.Remove();

                // 清掉目標 Run 內既有 Text/Break，但保留 RunProperties 與段落的 pPr
                var rPr = firstRun.RunProperties?.CloneNode(true) as RunProperties;
                firstRun.RemoveAllChildren<Text>();
                firstRun.RemoveAllChildren<Break>();
                if (rPr != null) firstRun.RunProperties = rPr;

                // 依行數把文字都塞進「同一個 Run」，用 <w:br/> 斷行
                for (int i = 0; i < lines.Length; i++)
                {
                    firstRun.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
                    if (i < lines.Length - 1)
                        firstRun.AppendChild(new Break());
                }
            }

            // 扁平化：把內容搬出來，移除控制項外框
            FlattenSdt(sdt);
            hits++;
        }

        return hits;
    }

    /// <summary>
    /// 建立一個沿用第一個 run 樣式的新 Run
    /// </summary>
    /// <param name="content">控制項</param>
    /// <param name="text">文字</param>
    /// <returns></returns>
    private static Run BuildRunLike(OpenXmlElement content, Text text)
    {
        var firstRun = content.Descendants<Run>().FirstOrDefault();
        var r = new Run();
        if (firstRun?.RunProperties != null)
            r.RunProperties = (RunProperties)firstRun.RunProperties.CloneNode(true);
        r.Append(text);
        return r;
    }

    /// <summary>
    /// 填入表格重複區塊
    /// </summary>
    /// <param name="scope">作用範圍</param>
    /// <param name="rowTag">表格標籤名稱</param>
    /// <param name="rows">表格資料</param>
    public static void FillRepeatRowsByTag(OpenXmlElement scope, string rowTag, IEnumerable<IDictionary<string, string>> rows)
    {
        var rowSdtTemplate = scope.Descendants<SdtElement>()
                                  .FirstOrDefault(s => MatchByTag(s, rowTag));
        if (rowSdtTemplate == null) return;

        var parent = rowSdtTemplate.Parent;
        bool insertedAny = false;

        foreach (var row in rows ?? Enumerable.Empty<IDictionary<string, string>>())
        {
            // 1) 複製範本
            var newRow = (SdtElement)rowSdtTemplate.CloneNode(true);

            // 2) 先把這列插入到文件樹，放在範本之前
            parent.InsertBefore(newRow, rowSdtTemplate);

            // 3) 在這一列裡填值（會對 cell 內的純文字控制項逐一 SetTextByTag → 扁平化）
            SetManyByTag(newRow, row);

            // 4) 最後把「列級」的外層 SDT 扁平化，保留 <w:tr>
            FlattenSdt(newRow);

            insertedAny = true;
        }

        // 5) 移除範本列
        if (insertedAny) rowSdtTemplate.Remove();
    }

    /// <summary>
    /// 扁平化控制項
    /// </summary>
    /// <param name="sdt">元件</param>
    private static void FlattenSdt(SdtElement sdt)
    {
        var content = GetSdtContent(sdt);
        if (content != null)
        {
            foreach (var child in content.Elements().ToList())
            {
                // 直接把 <w:tr>（或其他元素）搬到 sdt 之前
                sdt.InsertBeforeSelf(child.CloneNode(true));
            }
        }
        sdt.Remove();
    }

    // 1) 只把 Tag/Alias 命中的 SDT 內部文字清空；不動外面的「密／敏」也不刪 run/paragraph
    public static void ClearSdtTextByTagOrAlias(WordprocessingDocument doc, IEnumerable<string> keys)
    {
        var keySet = new HashSet<string>(keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        void Process(OpenXmlElement root)
        {
            if (root == null) return;

            foreach (var sdt in root.Descendants<SdtElement>())
            {
                var tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                var alias = sdt.SdtProperties?.GetFirstChild<SdtAlias>()?.Val?.Value;

                if ((!string.IsNullOrEmpty(tag) && keySet.Contains(tag)) ||
                    (!string.IsNullOrEmpty(alias) && keySet.Contains(alias)))
                {
                    foreach (var t in sdt.Descendants<Text>()) t.Text = string.Empty; // 只清文字
                }
            }
        }

        Process(doc.MainDocumentPart.Document.Body);
        foreach (var hp in doc.MainDocumentPart.HeaderParts) Process(hp.Header);
        foreach (var fp in doc.MainDocumentPart.FooterParts) Process(fp.Footer);
    }

    // 2) 安全拆殼：把 SDT 的內容搬到同層，最後移除 SDT（涵蓋 SdtRow/SdtCell/SdtRun/SdtBlock）
    public static void StripAllContentControlsSafe(WordprocessingDocument doc)
    {
        void Strip(OpenXmlElement root)
        {
            if (root == null) return;

            var sdts = root.Descendants<SdtElement>().ToList();
            foreach (var sdt in sdts)
            {
                OpenXmlElement content = sdt switch
                {
                    SdtBlock sb => sb.SdtContentBlock,
                    SdtRun sr => sr.SdtContentRun,
                    SdtCell sc => sc.SdtContentCell,
                    SdtRow srw => srw.SdtContentRow,
                    _ => null
                };

                if (content != null && content.HasChildren)
                {
                    var children = content.ChildElements.ToList();
                    foreach (var child in children)
                    {
                        content.RemoveChild(child);
                        sdt.InsertBeforeSelf(child);   // 搬到原 SDT 同層級
                    }
                }
                sdt.Remove();
            }
        }

        Strip(doc.MainDocumentPart.Document.Body);
        foreach (var hp in doc.MainDocumentPart.HeaderParts) { Strip(hp.Header); hp.Header.Save(); }
        foreach (var fp in doc.MainDocumentPart.FooterParts) { Strip(fp.Footer); fp.Footer.Save(); }
    }

    // 3) 正常化：確保每個儲存格至少有一個段落，避免 Word 報「無法讀取的內容」
    public static void EnsureEachCellHasParagraph(WordprocessingDocument doc)
    {
        void Fix(OpenXmlElement root)
        {
            if (root == null) return;
            foreach (var tc in root.Descendants<TableCell>())
            {
                bool hasBlock = tc.Elements<Paragraph>().Any() || tc.Elements<Table>().Any();
                if (!hasBlock)
                {
                    tc.AppendChild(new Paragraph(new Run())); // 補一個空段落
                }
            }
        }

        Fix(doc.MainDocumentPart.Document.Body);
        foreach (var hp in doc.MainDocumentPart.HeaderParts) { Fix(hp.Header); hp.Header.Save(); }
        foreach (var fp in doc.MainDocumentPart.FooterParts) { Fix(fp.Footer); fp.Footer.Save(); }
        doc.MainDocumentPart.Document.Save();
    }



}
