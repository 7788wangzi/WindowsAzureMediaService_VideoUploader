using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WAMS_UPLOAD
{
    public class ValidityCheck
    {
        //!*'();:@&=+$,/?%#[]". 
        //lenght limit of 260 chracters.
        //The value of the Name property cannot have any of the following percent-encoding-reserved characters: !*'();:@&=+$,/?%#[]". Also, there can only be one '.' for the file name extension.
        //input file name without extension test-demo+a23.abc
        /// <summary>
        /// Formate the file name
        /// </summary>
        /// <param name="stringOfFileName">File name without extension, example, test-demo+a23%abc</param>
        /// <returns>test-demo_a23_abc</returns>
        public static string FormatFileName(string stringOfFileName)
        {
            StringBuilder FileName = new StringBuilder();
            FileName.Append(stringOfFileName);
            string[] invalidStrings = new string[] { "!", "*", "'", "(", ")", ";", ":",
                "@","&","=","+","$",",","/","?","%","#","[","]","\"","." };
            foreach (string str in invalidStrings)
            {
                FileName = FileName.Replace(str, "_");
            }
            return FileName.ToString().Length>260?FileName.ToString().Substring(0,260):FileName.ToString();
        }        
    }
}
