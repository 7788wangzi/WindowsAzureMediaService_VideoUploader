using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAMS_UPLOAD
{
    public class GetTimeStamp
    {
        public static string ToString(string fileName)
        {
            string timestamp = string.Format("{0:d/M/yyyy HH:mm:ss}", DateTime.Now);
            string formatFileName = fileName+"_" + timestamp.Replace(@"/", "_").Replace(" ", "_").Replace(":", "_");
            return formatFileName;
        }

    }
}
