using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LundsLångLagerSQL
{
    internal static class Utilities
    {
        /// <summary>
        /// (Josef) Converts the palletSize value to the corresponding string
        /// </summary>
        /// <param name="palletSize"></param>
        /// <returns></returns>
        public static string GetPalletTypeFromInt(int palletSize)
        {
            if (palletSize == 100)
                return "Whole pallet";
            else if (palletSize == 50)
                return "Halfpallet";
            else
                return "[INVALID PALLET TYPE]";
        }

        /// <summary>
        /// (Josef) Converts datetime values to strings that can be used to cast datetime values in sql.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string GetSQLDateTimeString(DateTime dt)
        {
            return "'" + $"{dt.Year:D4}" + "-" + $"{dt.Month:D2}" + "-" + $"{dt.Day:D2}" + "T" + $"{dt.Hour:D2}" + ":" + $"{dt.Minute:D2}" + ":" + $"{dt.Second:D2}" + "." + $"{dt.Millisecond:D3}" + "'";
        }
    }
}
