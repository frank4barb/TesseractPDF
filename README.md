Ecco il testo formattato con i tag del README.md e i link alle librerie esterne:

# PDF Text Extractor
=====================

Questo progetto è un'applicazione console.NET che estrae il contenuto testuale da file PDF utilizzando la libreria [Tesseract OCR](https://github.com/tesseract-ocr/tesseract).

## Funzionalità

*   Estrazione del testo da file PDF utilizzando [Tesseract OCR](https://github.com/tesseract-ocr/tesseract)
*   Supporto per la connessione a database di tipo SQL Server, Sybase e Oracle
*   Possibilità di configurare la query di recupero dei file PDF dal database
*   Supporto per la rimozione di intestazione e piè di pagina dai documenti PDF utilizzando marcatori di inizio riga
*   Possibilità di salvare il testo estratto in un file di testo

## Librerie utilizzate

*   [Tesseract](https://github.com/tesseract-ocr/tesseract) (per l'OCR)
*   [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) (per l'elaborazione delle immagini)
*   [PdfiumViewer](https://github.com/pvginkel/PdfiumViewer) (per la lettura dei file PDF)
*   [AdoNetCore.AseClient](https://github.com/DataAction/AdoNetCore.AseClient) (per la connessione a database Sybase)
*   [Oracle.ManagedDataAccess.Client](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) (per la connessione a database Oracle)

## Configurazione

Per utilizzare il programma, è necessario configurare le seguenti variabili di ambiente:

*   `#tsPDF_dbType`: tipo di database (SQL Server, Sybase o Oracle)
*   `#tsPDF_connectionString`: stringa di connessione al database
*   `#tsPDF_select`: query di recupero dei file PDF dal database
*   `#tsPDF_docTY1`, `#tsPDF_docTYstart1`, `#tsPDF_docTYend1`,...: configurazione per la rimozione di intestazione e piè di pagina dai documenti PDF

## Esempio di utilizzo

Per utilizzare il programma, è sufficiente eseguirlo e seguire le istruzioni visualizzate a schermo. Il programma recupera i file PDF dal database, estrae il testo utilizzando Tesseract OCR e lo salva in un file di testo.

## Nota

Il programma è stato testato con file PDF di tipo A4 e potrebbe non funzionare correttamente con file di formato diverso. Inoltre, la qualità dell'estrazione del testo dipende dalla qualità del file PDF e dalla configurazione di Tesseract OCR.
