using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace check
{
    class Program
    {
        static void Main(string[] args)
        {
            string msg = "<DOC CBH=\"44232132130000\" DT=\"20150512102432\" DOCID=\"52577123213133\" CHECKCODE=\"1231232131\" DIRECTION=\"0\"><EMKDBUILD operateType=\"6\"><APPLYIDS>,147717</APPLYIDS></EMKDBUILD></DOC>";
            string[] m = msg.Split('=').ToArray<string>();
            Console.WriteLine(msg);
            Console.WriteLine("-------------------------------------------------------------------------------");
            for (int i=0;i< m.Length; i++)
            {
                Console.WriteLine(m[i]);
            }

            long docid = Int64.Parse(msg.Substring(msg.IndexOf("DOCID") + 7, msg.IndexOf("CHECKCODE") - msg.IndexOf("DOCID") - 9));
            long DT = Int64.Parse(msg.Substring(msg.IndexOf("DT") + 6, msg.IndexOf("DOCID") - msg.IndexOf("DT") - 8));
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(docid);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(DT);
            Console.ReadKey();
        }
    }
}
