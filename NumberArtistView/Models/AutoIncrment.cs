using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberArtistView.Models
{

    public static class AutoIncrment
    {
        private static int id = 1;
        public static int GenerateId()
        {
            return id++;
        }
    }

}
