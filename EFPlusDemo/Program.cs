using AutoGen;
using EFPlusDemo.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFPlusDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            BLLGenerator bLLGenerator = new BLLGenerator();
            bLLGenerator.GenerateCode();
        }
    }
}
