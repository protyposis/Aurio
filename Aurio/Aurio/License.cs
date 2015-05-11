using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Aurio {
    public static class License {
        public static string Info {
            get { return ReadEmbeddedResourceFileText("Aurio.licenseinfo.txt"); }
        }

        private static string ReadEmbeddedResourceFileText(string filename) {
            string text = String.Empty;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    text = reader.ReadToEnd();
                }
            }
            return text;
        }
    }
}
