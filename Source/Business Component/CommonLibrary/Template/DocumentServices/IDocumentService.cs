using System.Collections.Generic;
using Link.PT.Telegramas.CommonLibrary.Template.Entities;

namespace Link.PT.Telegramas.CommonLibrary.Template.DocumentServices
{
    internal interface IDocumentService
    {
        string TmpFile();

        /// <summary>
        /// Loads the template.
        /// </summary>
        /// <param name="templateFilename">The template filename.</param>
        void LoadTemplate(string templateFilename);

        /// <summary>
        /// Applies the specified template data.
        /// </summary>
        /// <param name="templateData">The template data.</param>
        void Apply(IDictionary<string, string> templateData);

        /// <summary>
        /// Applies the image.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageStream">The image stream.</param>
        //void ApplyImage(string name, Stream imageStream);

        /// <summary>
        /// Appends the specified dest file.
        /// </summary>
        /// <param name="destFile">The dest file.</param>
        /// <param name="srcFile">The SRC file.</param>
        void Append(string destFile, string srcFile);

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <returns></returns>
        string Save();

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <param name="destFile">The dest file.</param>
        void SaveAs(string destFile);

        /// <summary>
        /// Starts templating.
        /// </summary>
        /// 
        void Start();
        /// <summary>
        /// Ends templating.
        /// </summary>
        void End();

        /// <summary>
        /// Destroys this instance.
        /// </summary>
        void Destroy();
    }
}