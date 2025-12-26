using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeDModelFiles
{
    public class AutoCounter : IEnumerator
    {
        private int _current;

        public AutoCounter()
        {
            Reset();
        }

        public int GetNext()
        {
            if(!MoveNext())
            {
               throw new InvalidOperationException("No more items.");
            }
            return _current;
        }

        public object Current { get { return _current; } }

        public bool MoveNext()
        {
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }
    }
}
