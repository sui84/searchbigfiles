﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.SqlClient;

namespace Common.Helpers
{
    public class LinqHelper
    {
        public List<string> SearchString(List<string> lines, List<string> strs)
        {
            List<string> result = new List<string>();
            foreach (string str in strs)
            {
                // wrong way:
                // string conStr = string.Format("%{0}%", str);
                //var q = from line in lines
                //            where SqlMethods.Like(line, conStr)
                //            select line;
                // correct way:
                result.AddRange(lines.Where(o => o.Contains(str)).Distinct());
            }
            return result;
        }
        
    }
}
