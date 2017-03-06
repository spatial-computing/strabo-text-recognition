using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Strabo.Test
{
    class TestString
    {
        public void test()
        {
            string a = "120-R";
            if (Regex.IsMatch(a, @"[R]"))
            {
                string v = "NO";
            }
        }
    }
}
