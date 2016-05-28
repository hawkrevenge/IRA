using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCode
{
    class CEQHandler
    {
        public static double Jacquard(string Wt, string Wq)
        {
            if (Wt == "0" || Wq == "0")
                return 1;

            List<String> allvalues = new List<String>();
            string[] wvalues = Wt.Split(',');
            string[] qvalues = Wq.Split(',');

            double amount = 0;
            double total = 0;

            for (int i=0; i<qvalues.Length-1; i++)
            {
                string value = qvalues[i];
                allvalues.Add(value);
                total += Convert.ToInt32(value.Split(' ')[0]);
            }

            for (int i = 0; i < wvalues.Length - 1; i++)
            {
                string value = wvalues[i];
                if (allvalues.Contains(value))
                {
                    amount += Convert.ToInt32(value.Split(' ')[0]);
                }
                else
                {
                    allvalues.Add(value);
                    total += Convert.ToInt32(value.Split(' ')[0]);
                }
            }

            return (amount / total);
        }
    }
}
