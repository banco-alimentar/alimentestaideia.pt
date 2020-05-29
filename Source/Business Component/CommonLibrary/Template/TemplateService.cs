using System;
using System.Collections.Generic;
using Link.PT.Telegramas.CommonLibrary.Template.DocumentServices;

namespace Link.PT.Telegramas.CommonLibrary.Template
{
    public class TemplateService
    {
        public static string ApplyDocxTemplate(string template, Dictionary<string, string> propertiesDict, string destFile)
        {
            IDocumentService documentService = new XDocDocumentService();
            try
            {
                documentService.LoadTemplate(template);

                documentService.Start();

                documentService.Apply(propertiesDict);

                documentService.End();

                //documentService.SaveAs(destFile);
                return documentService.TmpFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                documentService.Destroy();
            }
        }
    }
}
