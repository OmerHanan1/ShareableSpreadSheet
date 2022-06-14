using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShareableSpreadSheet
{
    internal class SimulatorTester
    {
        public static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                throw new Exception("need to enter 5 arguments");
            }

            int rows = 0;
            try
            {
                rows = int.Parse(args[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("First argument is not a number!");
                Console.WriteLine(ex.Message);
            }

            int cols = 0;
            try
            {
                cols = int.Parse(args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Second argument is not a number!");
                Console.WriteLine(ex.Message);
            }

            int nThreads = 0;
            try
            {
                nThreads = int.Parse(args[2]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Third argument is not a number!");
                Console.WriteLine(ex.Message);
            }

            int nOperations = 0;
            try
            {
                nOperations = int.Parse(args[3]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("4th argument is not a number!");
                Console.WriteLine(ex.Message);
            }

            int mssleep = 0;
            try
            {
                mssleep = int.Parse(args[4]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("5th argument is not a number!");
                Console.WriteLine(ex.Message);
            }

            SharableSpreadSheet spreadSheet = new SharableSpreadSheet(rows, cols);
            Simulator(rows, cols, nThreads, nOperations, mssleep);

            void Simulator(int rows, int cols, int nThreads, int nOperations, int mssleep)
            {
                int count = 1;

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        spreadSheet.setCell(i, j, $"({i},{j})");
                        count++;
                    }
                }

                List<Thread> threads = new List<Thread>();
                for (int i = 0; i < nThreads; i++)
                {
                    var th = new Thread(DoWork);
                    threads.Add(th);
                    threads[i].Start();
                }

                foreach (Thread thread in threads)
                    thread.Join();
            }


            void DoWork()
            {

                Console.Write("User[" + Thread.CurrentThread.ManagedThreadId + "]:");
                for (int i = 0; i < 14; i++)
                {
                    Random rnd = new Random();
                    int r = rnd.Next(0, 14);

                    switch (r)
                    {
                        case 0:
                            string s = spreadSheet.getCell(0, 0);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "]" + " string " + s + " is in cell [0,0]");

                            break;
                        case 1:
                            spreadSheet.setCell(0, 0, "Eden");
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "]" + " string 'Eden' inserted to cell [0,0]");


                            break;
                        case 2:
                            string s1 = spreadSheet.getCell(0, 0);
                            Tuple<int, int> t = spreadSheet.searchString(s1);

                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "]" + " string " + s1 + " founded in cell [0,0]");

                            break;
                        case 3:
                            spreadSheet.exchangeRows(2, 3);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] row 2 exchange with row 3");
                            break;
                        case 4:
                            spreadSheet.exchangeCols(1, 4);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] col 1 exchange with col 4");
                            break;
                        case 5:
                            Random rnd1 = new Random();
                            int c = rnd.Next(0, cols);
                            string s4 = spreadSheet.getCell(0, c);

                            int Row = spreadSheet.searchInRow(0, s4);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] string " + s4 + " was found in Row " + Row.ToString());

                            break;
                        case 6:
                            Random rnd2 = new Random();
                            int row = rnd.Next(0, rows);
                            string s5 = spreadSheet.getCell(0, row);

                            int Col = spreadSheet.searchInCol(0, s5);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] string " + s5 + " was found in Col " + Col.ToString());

                            break;
                        case 7:
                            string s6 = spreadSheet.getCell(0, 0);
                            spreadSheet.searchInRange(0, 3, 0, 3, s6);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + s6 + " was found in a range col(0,3) and row(0,3) in cell [0,0]");
                            break;
                        case 8:
                            spreadSheet.addRow(2);
                            rows++;

                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "add another row after row 2");
                            break;
                        case 9:
                            spreadSheet.addCol(0);
                            cols++;

                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "add another colum after column 0");

                            break;
                        case 10:
                            string s7 = spreadSheet.getCell(0, 2);
                            spreadSheet.findAll(s7, true);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "the string " + s7 + " was found in cell (0,2)");
                            break;
                        case 11:
                            string s8 = spreadSheet.getCell(0, 3);
                            spreadSheet.setAll(s8, "omer", true);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "the string " + s8 + " was replaced with the string 'omer'");
                            break;
                        case 12:
                            spreadSheet.getSize();

                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] the current size of columns:" + cols.ToString() + " and current size of rows:" + rows.ToString());
                            break;
                        case 13:
                            spreadSheet.setConcurrentSearchLimit(nThreads);
                            Console.WriteLine("User[" + Thread.CurrentThread.ManagedThreadId + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] update the current size to " + nThreads.ToString());
                            break;

                    }
                    Thread.Sleep(mssleep);


                }
            }



        }
    }
}
