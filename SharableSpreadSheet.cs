using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShareableSpreadSheet
{
    internal class SharableSpreadSheet
    {
        private readonly int row;                           // Number of rows 
        private readonly int column;                        // Number of columns  
        private string[,] spreadSheet;                      // The main data resource holds the string values
        private Mutex[] indexMutices;                       // Array of mutices responsible for lock the rows by index
        private Mutex readMutex;                            // Lock read mode operations
        private Mutex writeMutex;                           // Lock write mode exclusive operations
        private Mutex changeSpreadSheetStructureMutex;      // Lock operations that will change the main resource structure
        private Semaphore modeSwitcher;                     // Controls and switchs what operation is reflecting (read/write) 
        private Semaphore searchSemaphore;                  // Privilege the number of searchers as described in setConcurrentSearchLimit func. documentation
        private int readers;                                // Counts number of readers
        private int writers;                                // Counts number of writers

        public SharableSpreadSheet(int nRows, int nCols, int nUsers = -1)
        {
            row = nRows;
            column = nCols;
            spreadSheet = new string[nRows, nCols];
            indexMutices = new Mutex[nRows];
            for (int i = 0; i < nRows; i++)
                indexMutices[i] = new Mutex();
            readMutex = new Mutex();
            writeMutex = new Mutex();
            changeSpreadSheetStructureMutex = new Mutex();
            modeSwitcher = new Semaphore(1,1);
            searchSemaphore = new Semaphore(1,nUsers);
            readers = 0;
            writers = 0;
        }

        public String getCell(int row, int col)
        {
            if (this.row < row || this.column < col) { return null; }

            readerLock();
            string result = spreadSheet[row, col];
            readerRelease();
            return result;
        }
        public void setCell(int row, int col, String str)
        {
            if (this.row < row || this.column < col) { return; }

            writerLock(row, col);
            spreadSheet[row, col] = str;
            writerRelease(row, col);
        }

        public Tuple<int, int> searchString(String str)
        {
            int row, col;
            // return first cell indexes that contains the string (search from first row to the last row)
            return < row, col >;
        }
        public void exchangeRows(int row1, int row2)
        {
            // exchange the content of row1 and row2
        }
        public void exchangeCols(int col1, int col2)
        {
            // exchange the content of col1 and col2
        }
        public int searchInRow(int row, String str)
        {
            int col;
            // perform search in specific row
            return col;
        }
        public int searchInCol(int col, String str)
        {
            int row;
            // perform search in specific col
            return row;
        }
        public Tuple<int, int> searchInRange(int col1, int col2, int row1, int row2, String str)
        {
            int row, col
            // perform search within spesific range: [row1:row2,col1:col2] 
            //includes col1,col2,row1,row2
            return < row,col >;
        }
        public void addRow(int row1)
        {
            //add a row after row1
        }
        public void addCol(int col1)
        {
            //add a column after col1
        }
        public Tuple<int, int>[] findAll(String str, bool caseSensitive)
        {
            // perform search and return all relevant cells according to caseSensitive param
        }
        public void setAll(String oldStr, String newStr bool caseSensitive)
        {
            // replace all oldStr cells with the newStr str according to caseSensitive param
        }
        public Tuple<int, int> getSize()
        {
            int nRows, int nCols;
            // return the size of the spreadsheet in nRows, nCols
            return< nRows,nCols >;
        }
        public void setConcurrentSearchLimit(int nUsers)
        {
            // this function aims to limit the number of users that can perform the search operations concurrently.
            // The default is no limit. When the function is called, the max number of concurrent search operations is set to nUsers. 
            // In this case additional search operations will wait for existing search to finish.
            // This function is used just in the creation
        }

        public void save(String fileName)
        {
            // save the spreadsheet to a file fileName.
            // you can decide the format you save the data. There are several options.
        }
        public void load(String fileName)
        {
            // load the spreadsheet from fileName
            // replace the data and size of the current spreadsheet with the loaded data
        }

        private void readerLock() 
        {
            readMutex.WaitOne();
            Interlocked.Increment(ref readers);
            if (readers == 1)
                modeSwitcher.WaitOne();
            readMutex.ReleaseMutex();
        }
        private void readerRelease() 
        {
            readMutex.WaitOne();
            Interlocked.Decrement(ref readers);
            if (readers == 0)
                modeSwitcher.WaitOne();
            readMutex.ReleaseMutex();
        }
        private void writerLock(int rowIndex, int columnIndex) 
        {
            if (rowIndex < 0 || columnIndex < 0 || row < rowIndex || column < columnIndex) 
            {
                // do nothing
            }
            else 
            {
                writeMutex.WaitOne();
                Interlocked.Increment(ref writers);
                if (writers == 1)
                    modeSwitcher.WaitOne();
                writeMutex.ReleaseMutex();
                indexMutices[rowIndex].WaitOne();
            }
        }
        private void writerRelease(int rowIndex, int columnIndex) 
        {
            if (rowIndex < 0 || columnIndex < 0 || row < rowIndex || column < columnIndex)
            {
                // do nothing
            }
            else
            {
                indexMutices[rowIndex].ReleaseMutex();
                writeMutex.WaitOne();
                Interlocked.Decrement(ref writers);
                if (writers == 0)
                    modeSwitcher.WaitOne();
                writeMutex.ReleaseMutex();
            }
        }
        private void structureChangeLock() 
        {

        }
        private void structureChangeRelease() 
        {

        }
    }
}
