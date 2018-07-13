using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMAP.General
{
    class RandomGenerator
    {
        private static Random m_rnd = new Random(0);
        public static void Init(int iSeed)
        {
            m_rnd = new Random(iSeed);
        }
        public static void Init()
        {
            m_rnd = new Random();
        }
        public static int Next(int iMax)
        {
            return m_rnd.Next(iMax);
        }
        public static double NextDouble()
        {
            return m_rnd.NextDouble();
        }
    }
}
