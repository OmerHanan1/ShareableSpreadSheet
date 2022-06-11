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
        private int numberOfUsers;                          // Given number of users

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nRows">number of rows in shared spreadsheet</param>
        /// <param name="nCols">number of columns in shared spreadsheet</param>
        /// <param name="nUsers">upper bound of users can access shared spreadsheet at once</param>
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

        /// <summary>
        /// Lock, read
        /// </summary>
        private void readerLock()
        {
            readMutex.WaitOne();
            Interlocked.Increment(ref readers);
            if (readers == 1)
                modeSwitcher.WaitOne();
            readMutex.ReleaseMutex();
        }

        /// <summary>
        /// Lock releasing, read
        /// </summary>
        private void readerReleaseLock()
        {
            readMutex.WaitOne();
            Interlocked.Decrement(ref readers);
            if (readers == 0)
                modeSwitcher.WaitOne();
            readMutex.ReleaseMutex();
        }

        /// <summary>
        /// Lock, write
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
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

        /// <summary>
        /// Lock releasing, write
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        private void writerReleaseLock(int rowIndex, int columnIndex)
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

        /// <summary>
        /// Lock, search
        /// </summary>
        private void searchLock() 
        {
            if(numberOfUsers != -1 && searchSemaphore != null)
                searchSemaphore.WaitOne();
        }
        
        /// <summary>
        /// Lock releasing, search
        /// </summary>
        private void searchReleaseLock() 
        {
            if (numberOfUsers != -1 && searchSemaphore != null)
                searchSemaphore.Release();
        }

        private void structureChangeLock()
        {
            changeSpreadSheetStructureMutex.WaitOne();
            modeSwitcher.WaitOne();
        }

        private void structureChangeReleaseLock()
        {
            modeSwitcher.Release();
            changeSpreadSheetStructureMutex.ReleaseMutex();
        }

        /// <summary>
        /// taks the string value of the cell by given row and column indexes
        /// </summary>
        /// <param name="row">row index</param>
        /// <param name="col">column index</param>
        /// <returns>returns the string value inside the cell</returns>
        public String getCell(int row, int col)
        {
            if (this.row < row || this.column < col) { return null; }

            readerLock();
            string result = spreadSheet[row, col];
            readerReleaseLock();
            return result;
        }

        /// <summary>
        /// set the cell value to string value passed as argument
        /// </summary>
        /// <param name="row">row index</param>
        /// <param name="col">column index</param>
        /// <param name="str">string value</param>
        public void setCell(int row, int col, String str)
        {
            if (this.row < row || this.column < col) { return; }

            writerLock(row, col);
            spreadSheet[row, col] = str;
            writerReleaseLock(row, col);
        }

        /// <summary>
        /// searching for cell contains the given string value
        /// </summary>
        /// <param name="str">string value to search for</param>
        /// <returns></returns>
        public Tuple<int, int> searchString(String str)
        {
            readerLock();
            searchLock();
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    if(spreadSheet[i,j].Contains(str))
                        return new Tuple<int, int>(i, j);
                }
            }
            searchReleaseLock();
            readerReleaseLock();
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row1"></param>
        /// <param name="row2"></param>
        public void exchangeRows(int row1, int row2)
        {
            // exchange the content of row1 and row2
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
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
    }
}
