using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Link.PT.Telegramas.CommonLibrary.Template.Entities;
using Novacode;
using Paragraph = Novacode.Paragraph;
using System.Configuration;
using System.Web;
//using Microsoft.WindowsAzure.ServiceRuntime;

namespace Link.PT.Telegramas.CommonLibrary.Template.DocumentServices
{
    internal class XDocDocumentService : IDocumentService       
    {
        private static long m_Id = 0;
        private string m_DocumentFile;
        private string m_TemplateFile;
        private string m_TmpFile;

        public void LoadTemplate(string templateFilename)
        {
            m_TemplateFile = templateFilename;
        }

        public void Start()
        {
            m_TmpFile = getTemporaryFile("docx");
            File.Copy(m_TemplateFile, m_TmpFile);
            File.SetAttributes(m_TmpFile, FileAttributes.Normal);
        }

        public void Apply(IDictionary<string, string> templateData)
        {
            DocX document = DocX.Load(m_TmpFile);

            foreach (KeyValuePair<string, string> pair in templateData)
            {
                document.ReplaceText(pair.Key, pair.Value, false, RegexOptions.IgnoreCase);
            }

            document.Save();
            
            WordprocessingDocument doc = WordprocessingDocument.Open(m_TmpFile, true);
            doc.ExtendedFilePropertiesPart.Properties.DocumentSecurity = new DocumentFormat.OpenXml.ExtendedProperties.DocumentSecurity("8");
            doc.ExtendedFilePropertiesPart.Properties.Save();

            DocumentProtection dp = doc.MainDocumentPart.DocumentSettingsPart.Settings.ChildElements.First<DocumentProtection>();
            if (dp != null)
            {
                dp.Remove();
            }

            dp = new DocumentProtection();
            dp.Edit = DocumentProtectionValues.Comments;
            dp.Enforcement = DocumentFormat.OpenXml.Wordprocessing.BooleanValues.One;

            doc.MainDocumentPart.DocumentSettingsPart.Settings.AppendChild(dp);

            doc.MainDocumentPart.DocumentSettingsPart.Settings.Save();

            doc.Close();
              
        }

       /*
        public void ApplyImage(string name, Stream imageStream)
        {
            DocX document = DocX.Load(m_TmpFile);

            // Add an Image from a file.
            Novacode.Image img = document.AddImage(@imageStream);

            Picture barcodeImg = new Picture(img.Id, "barcode" + m_ImgId++, "bar code image");
           
            // Resize the Picture.


            foreach (Paragraph p in document.Paragraphs)
            {
                MatchCollection mc = Regex.Matches(p.Text, Regex.Escape(name));
                foreach (Match m in mc.Cast<Match>().Reverse())
                {
                    int i = m.Index;
                    p.ReplaceText(name, " ", false);
                    p.InsertPicture(barcodeImg, i);
                }
            }

            // Close the document.
            document.Close(true);
        }
        */

        public void Append(string destFile, string srcFile)
        {
            using (WordprocessingDocument myDoc = WordprocessingDocument.Open(destFile, true))
            {
                MainDocumentPart mainPart = myDoc.MainDocumentPart;

                string chunkId = "template" + m_Id++;

                AlternativeFormatImportPart chunk = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.WordprocessingML, chunkId);
                chunk.FeedData(File.Open(srcFile, FileMode.Open));
                AltChunk altChunk = new AltChunk();
                altChunk.Id = chunkId;
                mainPart.Document.LastChild.InsertAfterSelf(altChunk);
                mainPart.Document.Save();
            }
        }

        public void Append(string file)
        {
            if (m_DocumentFile == null)
            {
                m_DocumentFile = getTemporaryFile("docx");
                File.Copy(file, m_DocumentFile);
            }
            else
            {
                using (WordprocessingDocument myDoc = WordprocessingDocument.Open(m_DocumentFile, true))
                {
                    
                    MainDocumentPart mainPart = myDoc.MainDocumentPart;

                    string chunkId = "template" + m_Id++;

                    AlternativeFormatImportPart chunk = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.WordprocessingML, chunkId);
                    chunk.FeedData(File.Open(file, FileMode.Open));
                    AltChunk altChunk = new AltChunk();
                    altChunk.Id = chunkId;
                    mainPart.Document.LastChild.InsertAfterSelf(altChunk);
                    mainPart.Document.Save();
                }
            }
        }
        public void End()
        {
            Append(m_TmpFile);
            //File.Delete(m_TmpFile);
        }

        public string Save()
        {
            string newfile = getTemporaryFile("docx");
            File.Copy(m_DocumentFile, newfile, true);

            return newfile;
        }

        public void SaveAs(string destFile)
        {
            var directory = Path.GetDirectoryName(destFile);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.Copy(m_DocumentFile, destFile, true);
        }

        public void Destroy()
        {
            if (m_DocumentFile != null)
            {
                File.Delete(m_DocumentFile);
                m_DocumentFile = null;
            }
        }

        public static bool CheckDocument(byte[] document)
        {
            try {
                Stream documentStream = new MemoryStream(document);

                WordprocessingDocument wordDocument = WordprocessingDocument.Open(documentStream, false);

                wordDocument.Close();
            } catch (Exception)
            {
                return false;
            }
            return true;
        }

        /* Private */
        #region
        private string getTemporaryFile(string extn)
        {
            string response = string.Empty;

            try
            {
                if (!extn.StartsWith("."))
                    extn = "." + extn;

                if (String.IsNullOrEmpty(HttpRuntime.AppDomainAppVirtualPath))
                    response = ConfigurationManager.AppSettings["DestinationDirectory"] + Guid.NewGuid() + extn;
                else
                    // TODO: remove this dependency from Azure
                    /*if (RoleEnvironment.IsAvailable)
                    {
                        // Retrieve an object that points to the local storage resource
                        LocalResource localResource = RoleEnvironment.GetLocalResource("PdfGeneration");

                        //Define the file name and path
                        string[] paths = { localResource.RootPath, Guid.NewGuid() + extn };
                        response = Path.Combine(paths);
                    }
                    else
                    {*/
                        response = Path.GetTempPath() + Guid.NewGuid() + extn;
                    //}
                //response = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + extn;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return response;
        }
        #endregion  
    }
}
