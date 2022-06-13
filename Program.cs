using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ShareableSpreadSheet;



public static class Globals
{
    public static int Rows;
    public static int Columns;
}

public class ThreadWork
{

    public static void DoWork(int nOperations, int mssleep, SharableSpreadSheet spreadSheet, int ID, int rows, int cols, int nThreads)
    {
        Console.Write("User[" + ID.ToString() + "]:");
        for (int i = 0; i < 14; i++)
        {
            Random rnd = new Random();
            int r = rnd.Next(1, 14);

            switch (r)
            {
                case 0:
                    string s = spreadSheet.getCell(0, 0);

                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "]" + " string " + s + " is in cell [0,0]");

                    break;
                case 1:
                    spreadSheet.setCell(0, 0, "Eden");

                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "]" + " string 'Eden' inserted to cell [0,0]");


                    break;
                case 2:
                    string s1 = spreadSheet.getCell(0, 0);

                    Tuple<int, int> t = spreadSheet.searchString(s1);

                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "]" + " string " + s1 + " founded in cell [0,0]");

                    break;
                case 3:
                    spreadSheet.exchangeRows(2, 3);
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] row 2 exchange with row 3");
                    break;
                case 4:
                    spreadSheet.exchangeCols(1, 4);
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] col 1 exchange with col 4");
                    break;
                case 5:
                    Random rnd1 = new Random();
                    int c = rnd.Next(0, cols);
                    //string s4 = spreadSheet.getCell(0, c);

                    int Row = spreadSheet.searchInRow(0, "testcell0");
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] string " + "testsell0" + " was found in Row " + Row.ToString());

                    break;
                case 6:
                    Random rnd2 = new Random();
                    //int row = rnd.Next(0, rows);
                    //string s5 = spreadSheet.getCell(0, row);

                    int Col = spreadSheet.searchInCol(0, "testcell61");
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] string" + "testcell61" + " was found in Col " + Col.ToString());

                    break;
                case 7:
                    string s6 = spreadSheet.getCell(0, 0);
                    spreadSheet.searchInRange(0, 3, 0, 3, s6);
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + s6 + " was found in a range col(0,3) and row(0,3) in cell [0,0]");
                    break;
                case 8:
                    spreadSheet.addRow(0);
                    Globals.Rows++;

                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "add another row after row 0");
                    break;
                case 9:
                    spreadSheet.addCol(0);
                    Globals.Columns++;

                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "add another colum after column 0");

                    break;
                case 10:
                    //string s7 = spreadSheet.getCell(0, 2);
                    spreadSheet.findAll("testcell99", true);
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "the string " + "testcell99" + " was found in cell (0,2)");
                    break;
                case 11:
                    //string s8 = spreadSheet.getCell(0, 3);
                    spreadSheet.setAll("testcell65", "omer", true);
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + "the string " + "testcell65" + " was replaced with the string 'omer'");
                    break;
                case 12:
                    spreadSheet.getSize();

                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] the current size of columns:" + Globals.Columns.ToString() + " and current size of rows:" + Globals.Rows.ToString());
                    break;
                case 13:
                    spreadSheet.setConcurrentSearchLimit(nThreads);
                    Console.WriteLine("User[" + ID.ToString() + "]:" + "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] update the current size to " + nThreads.ToString());
                    break;


            }
            Thread.Sleep(mssleep);

        }



    }
}












namespace ShareableSpreadSheet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            void Simulator(int rows, int cols, int nThreads, int nOperations, int mssleep)
            {
                SharableSpreadSheet spreadSheet = new SharableSpreadSheet(rows, cols);
                int count = 1;
                Globals.Rows = rows;
                Globals.Columns = cols;


                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        spreadSheet.setCell(i, j, "testcell" + count.ToString());
                        //spreadSheet.spreadSheet[i, j] = "test";
                        count++;
                        Thread.Sleep(20);
                    }
                }

                //Dictionary<int, Thread> threads = new Dictionary<int, Thread>();

                List<Thread> threads = new List<Thread>();

                for (int i = 0; i < nThreads; i++)
                {
                    Thread.Sleep(mssleep);

                    var thread = new Thread(new ParameterizedThreadStart(ThreadWork.DoWork));
                    threads.Add(thread);
                    thread.Start(nOperations, mssleep, spreadSheet, i, Globals.Rows, Globals.Columns, nThreads);
                }

                foreach (Thread thread in threads)
                    thread.Join();


            }

            Simulator(5, 5, 2, 5, 500);
        }
    }
}
