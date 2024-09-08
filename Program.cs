using System.Data.SqlClient;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using PdfiumViewer;
using System.Text;
using TesseractPDF;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using AdoNetCore.AseClient;

class Program
{

    static bool logImage = true;
    static string projDirectory;


    private static IDbConnection getConnection(string dbType, string connectionString)
    {
        switch (dbType)
        {
            case "SqlServer": return (IDbConnection)new SqlConnection(connectionString);
            case "Sybase": return (IDbConnection)new AseConnection(connectionString);
            case "Oracle": return (IDbConnection)new OracleConnection(connectionString);
            default: return null;
        }
    }
    private static IDbCommand getCommand(string dbType, string query, IDbConnection connection)
    {
        switch (dbType)
        {
            case "SqlServer": return (IDbCommand)new SqlCommand(query, (SqlConnection)connection);
            case "Sybase": return (IDbCommand)new AseCommand(query, (AseConnection)connection);
            case "Oracle": return (IDbCommand)new OracleCommand(query, (OracleConnection)connection);
            default: return null;
        }
    }

    static void Main()
    {
        // Forza DefaultDir in base a dove viene avviato il programma
        projDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../.."));
        Directory.SetCurrentDirectory(projDirectory);

        // I dbType supportati sono
        // SqlServer
        // Sybase
        // Oracle
        string dbType = Context.Instance.GetString("#tsPDF_dbType");
        string connectionString = Context.Instance.GetString("#tsPDF_connectionString");

        // la select deve contenere le colonne:
        // ID_DOC       : codice univoco documento PDF
        // TYPE_DOC     : tipo documento PDF
        // DESC1        : descrizione testuale 1
        // DESC2        : descrizione testuale 2
        // DESC3        : descrizione testuale 3
        // BLOB_PDF     : contenuto binario del documento PDF
        string query = Context.Instance.GetString("#tsPDF_select");  //"select xxx as ID_DOC, xxx as TYPE_DOC, ... from TABLE where .....";

        //Lettura delle configurazioni di limitazione dei documenti
        //
        // Per eliminare intestazione e piè di pagina dal documento si usano dei marcatori di inizio riga.
        //  - Viene applicato il primo abbinamento che funziona.
        //  - Per l'esclusione del testo, alla riga individuata, viene sommato il numero di righe successivo
        //  - Le righe vuote ad inizio e fine pagina vengono eliminate.
        //  
        // #tsPDF_docTY1 XXXXXXXX                                               <- tipo documento da abbinare a TYPE_DOC
        // #tsPDF_docTYstart1 xxxxx**3||yyyyyy**-1||.....                       <- lista marcatori che escludono intestazione: <marcatore a inizio riga>**<numeo di righe da sommare>||....
        // #tsPDF_docTYend1 xxxxx**3||yyyyyy**-1||.....                         <- lista marcatori che escludono piè di pagina: <marcatore a inizio riga>**<numeo di righe da sommare>||....
        List<string> pdfTypes = new List<string>(); List<string[]> pdfTyStart = new List<string[]>(); List<string[]> pdfTyEnds = new List<string[]>();
        for (int i = 1; i < 100; i++)
        {
            string strType = Context.Instance.GetString($"#tsPDF_docTY{i}");
            string strStart = Context.Instance.GetString($"#tsPDF_docTYstart{i}");
            string strEnd = Context.Instance.GetString($"#tsPDF_docTYend{i}");
            if (string.IsNullOrEmpty(strType)) break;
            pdfTypes.Add(strType); pdfTyStart.Add(strStart.Split("||")); pdfTyEnds.Add(strEnd.Split("||"));
        }



        // Inizializza TesseractEngine una sola volta
        using (var engine = new TesseractEngine(projDirectory + @"/tessdata", "ita", EngineMode.Default))
        {
            // Esegui la query e processa i PDF
            string textFileName = projDirectory + "/temp/pdfText.txt";
            int maxRecords = 100, numRec = 0;
            using (IDbConnection connection = (IDbConnection)getConnection(dbType, connectionString))
            {
                connection.Open();
                using (IDbCommand command = (IDbCommand)getCommand(dbType, query, connection))
                {
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        command.CommandTimeout = 1;  // 1 seconds timeout
                        if (File.Exists(textFileName)) File.Delete(textFileName);
                        while (reader.Read() && numRec < maxRecords)
                        {
                            string pdfType = (string)reader["TYPE_DOC"];
                            //save desc
                            SaveToFile(textFileName, "***************************************************");
                            SaveToFile(textFileName, (string)reader["ID_DOC"] + "  [" + pdfType + "]");
                            SaveToFile(textFileName, (string)reader["DESC1"]);
                            SaveToFile(textFileName, (string)reader["DESC2"]);
                            SaveToFile(textFileName, (string)reader["DESC3"]);
                            SaveToFile(textFileName, "---------------------------------------------------");
                            //save text
                            byte[] pdfBytes = (byte[])reader["BLOB_PDF"];
                            if (pdfBytes[pdfBytes.Length - 1] == 0) pdfBytes = pdfBytes.SkipLast(1).ToArray();
                            string text = ExtractTextFromPdf(pdfType, pdfBytes, engine, pdfTypes, pdfTyStart, pdfTyEnds);
                            SaveToFile(textFileName, text);
                            numRec++;
                        }
                    }
                }
            }
        }
    }

    private static string ExtractTextFromPdf(string pdfType, byte[] pdfBytes, TesseractEngine engine, List<string> pdfTypes, List<string[]> pdfTyStart, List<string[]> pdfTyEnds)
    {
        string extractedText = "";

        // Crea un PDF document da PdfiumViewer usando un MemoryStream
        using (var pdfStream = new MemoryStream(pdfBytes))
        using (var document = PdfDocument.Load(pdfStream))
        {
            // Itera su ogni pagina del documento PDF
            for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                // Renderizza la pagina in un'immagine Bitmap
                using (System.Drawing.Image image = document.Render(pageIndex, 600, 600, PdfRenderFlags.CorrectFromDpi))  //using (System.Drawing.Image image = document.Render(pageIndex, 600, 600, PdfRenderFlags.Annotations))
                {
                    // Converti l'immagine Bitmap in un array di byte[] PNG
                    using (var memoryStream = new MemoryStream())
                    {
                        image.Save((Stream)memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                        if (logImage)
                        {
                            using (var fs = new FileStream(projDirectory + @"/temp/testFile1.png", FileMode.Create, FileAccess.Write))
                            {
                                fs.Write(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
                            }
                        }

                        //if (applicaFiltro) outputMemoryStream = filtroNitidezza(memoryStream);
                        memoryStream.Position = 0; // Resetta la posizione del MemoryStream

                        //---------------------------------------------------------------
                        //TESSERACT
                        //---------------------------------------------------------------
                        //Elenco dei PSM
                        //0: Solo rilevamento dell’orientamento e dello script(OSD).
                        //1: Segmentazione automatica della pagina con OSD.
                        //2: Segmentazione automatica, ma senza OSD o OCR(non implementato).
                        //3: Segmentazione automatica completa, ma senza OSD(predefinito).
                        //4: Assume una singola colonna di testo di dimensioni variabili.
                        //5: Assume un blocco uniforme di testo allineato verticalmente.
                        //6: Assume un blocco uniforme di testo.
                        //7: Tratta l’immagine come una singola linea di testo.
                        //8: Tratta l’immagine come una singola parola.
                        //9: Tratta l’immagine come una singola parola in un cerchio.
                        //10: Tratta l’immagine come un singolo carattere.
                        //11: Testo sparso. Trova il maggior numero possibile di testi senza un ordine particolare.
                        //12: Testo sparso con OSD.
                        //13: Linea grezza. Tratta l’immagine come una singola linea di testo, bypassando hack specifici di Tesseract.
                        //
                        using (var imgPix = Pix.LoadFromMemory(memoryStream.ToArray()))
                        using (var page = engine.Process(imgPix, " --psm 6"))  //using (var page = engine.Process(imgPix, PageSegMode.SingleBlock))  //using (var page = engine.Process(imgPix))
                        {
                            // get PDF text lines
                            List<string> lines = new List<string>(); 
                            using (var iter = page.GetIterator())
                            {
                                iter.Begin();
                                do
                                {
                                    if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine))
                                    {
                                        string line = iter.GetText(PageIteratorLevel.TextLine); lines.Add(line);
                                    }
                                } while (iter.Next(PageIteratorLevel.TextLine));
                            }
                            //extract Text
                            int index = Array.FindIndex<string>(pdfTypes.ToArray(), ty => ty.Equals(pdfType, StringComparison.Ordinal));
                            if (index != -1) extractedText += extractDoc(lines, pdfTyStart.ToArray()[index], pdfTyEnds.ToArray()[index]);
                            else extractedText += page.GetText() + "\n^^^^^^^^^^^^^^^\n"; 

                            //extractedText += page.GetText() + "\n*********************\n";
                            //extractedText += confidenceScoreText(page) + "\n*********************\n";
                        }
                        //-----------------------------------------------------------------
                    }
                }
            }
        }
        return extractedText;
    }

    private static string extractDoc(List<string> lines, string[] start, string[] end)
    {
        int startLine = 0, endLine = 0;
        foreach (var str in start)
        {
            string searchStr = str.Split("**")[0].Trim();
            int searchJump = 0; if (str.Split("**").Length > 1) searchJump = int.Parse(str.Split("**")[1]);
            int index = Array.FindIndex<string>(lines.ToArray(), line => line.StartsWith(searchStr, StringComparison.Ordinal));
            if (index != -1) { startLine = index + searchJump; break; }
        }
        foreach (var str in end)
        {
            string searchStr = str.Split("**")[0].Trim();
            int searchJump = 0; if (str.Split("**").Length > 1) searchJump = int.Parse(str.Split("**")[1]);
            int index = Array.FindLastIndex<string>(lines.ToArray(), line => line.StartsWith(searchStr, StringComparison.Ordinal));
            if (index != -1) { endLine = index + searchJump; break; }
        }
        if (endLine == 0) endLine = lines.Count() - 1;
        while (lines[startLine].TrimEnd(new char[] { ' ', '\t', '\n' }) == "") startLine++;  // scarto prime linee vuote
        while (lines[endLine].TrimEnd(new char[] { ' ', '\t', '\n' }) == "") endLine--; // scarto ultime linee vuote
        //---
        return string.Join("", lines.GetRange(startLine, endLine - startLine + 1)).TrimEnd('\n') + "\n";
    }

    private static void SaveToFile(string filePath, string content)
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine(content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Si è verificato un errore: {ex.Message}");
        }
    }


    //Tesseract può fornire un punteggio di affidabilità(confidence score) per il testo riconosciuto, sia a livello di parola che di carattere.
    //Questo punteggio indica quanto Tesseract è sicuro della sua interpretazione.
    //---
    //Confidence Score: Ogni volta che Tesseract riconosce un carattere o una parola, assegna un punteggio che varia da 0 a 100. Un punteggio più alto indica una maggiore certezza nella corretta interpretazione del testo.
    //Utilizzare ResultIterator.Confidence() per ottenere il punteggio di fiducia per ogni parola o carattere.
    static string confidenceScoreText(Page page)
    {
        StringBuilder text = new StringBuilder();
        using (var iter = page.GetIterator())
        {
            iter.Begin();
            do
            {
                if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine)) text.Append('\n');
                if (iter.IsAtBeginningOf(PageIteratorLevel.Word))
                {
                    string word = iter.GetText(PageIteratorLevel.Word);
                    float confidence = iter.GetConfidence(PageIteratorLevel.Word);
                    Console.WriteLine($"Confidence: {confidence}, Word: {word}");
                    text.Append(word).Append(' ');
                }
            } while (iter.Next(PageIteratorLevel.Word));
        }
        return text.ToString();
    }


    //filtro nitidezza immagine (non usato)
    static Byte[] filtroNitidezza(Stream memoryStream)
    {
        memoryStream.Position = 0; // Resetta la posizione del MemoryStream

        // Carica l'immagine in ImageSharp per ulteriori processi
        using (SixLabors.ImageSharp.Image imageSix = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream))
        {
            // Applica un filtro di sharpening per migliorare la nitidezza
            imageSix.Mutate(x => x
                .Resize(imageSix.Width, imageSix.Height)  // Mantiene la dimensione attuale
                .GaussianSharpen(1.5f));  // Applica sharpening, il fattore può essere regolato

            // Converti l'immagine elaborata in byte[]
            using (var outputMemoryStream = new MemoryStream())
            {
                imageSix.SaveAsPng(outputMemoryStream);

                if (logImage)
                {
                    using (var fs = new FileStream(projDirectory + @"/temp/testFile2.png", FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(outputMemoryStream.ToArray(), 0, outputMemoryStream.ToArray().Length);
                    }
                }
                outputMemoryStream.Position = 0; // Resetta la posizione del MemoryStream

                return outputMemoryStream.ToArray();
            }
        }

    }


    //-----------------------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------------------

}


