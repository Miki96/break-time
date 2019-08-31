using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Manager
    {
        // singleton
        private static Manager instance = null;
        public static Manager getInstance()
        {
            if (instance == null)
                instance = new Manager();
            return instance;
        }




    }
}
