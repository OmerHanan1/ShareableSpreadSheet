using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShareableSpreadSheet
{
    internal class SharableSpreadSheet
    {
        private int row;                                    // Number of rows 
        private int column;                                 // Number of columns  
        private string[,] spreadSheet;                      // The main data resource holds the string values
        private Mutex[] indexMutices;                       // Array of mutices responsible for lock the rows by index
        private Mutex readMutex;                            // Lock read mode operations
        private Mutex writeMutex;                           // Lock write mode exclusive operations
        private Mutex changeSpreadSheetStructureMutex;      // Lock operations that will change the main resource structure
        private Semaphore modeSwitcher;                     // Controls and switchs what operation is reflecting (read/write) 
        private SemaphoreSlim searchSemaphore;              // Privilege the number of searchers as described in setConcurrentSearchLimit func. documentation
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
            searchSemaphore = null;
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
            if (numberOfUsers != -1 && searchSemaphore != null)
                searchSemaphore.Wait();
        }
        
        /// <summary>
        /// Lock releasing, search
        /// </summary>
        private void searchReleaseLock() 
        {
            if (numberOfUsers != -1 && searchSemaphore != null)
                searchSemaphore.Release();
        }

        /// <summary>
        /// Lock, structure
        /// </summary>
        private void structureChangeLock()
        {
            changeSpreadSheetStructureMutex.WaitOne();
            modeSwitcher.WaitOne();
        }

        /// <summary>
        /// Lock releasing, structure
        /// </summary>
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
        /// <returns>row,col index pair</returns>
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
        /// replacing one row with another by given indexes
        /// </summary>
        /// <param name="row1">first row index in the exchange</param>
        /// <param name="row2">second row index in the exchange</param>
        public void exchangeRows(int row1, int row2)
        {
            structureChangeLock();
            if(this.row < row1 || this.row < row2) 
            {
                structureChangeReleaseLock();
                return; 
            }

            string str;
            for (int i = 0; i < column; i++)
            {
                str = spreadSheet[row1, i].ToString();
                spreadSheet[row1, i] = spreadSheet[row2, i];
                spreadSheet[row2, i] = str;
            }
            structureChangeReleaseLock();
        }

        /// <summary>
        /// exchange the content of col1 and col2
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        public void exchangeCols(int col1, int col2)
        {
            structureChangeLock();
            if (this.column < col1 || this.column < col2) 
            {
                structureChangeReleaseLock();
                return;
            }

            string str;
            for (int i = 0; i < row; i++)
            {
                str = spreadSheet[i,col1].ToString();
                spreadSheet[i,col1] = spreadSheet[i, col2];
                spreadSheet[i,col2] = str;
            }
            structureChangeReleaseLock();
        }

        /// <summary>
        /// perform search in specific row
        /// </summary>
        /// <param name="row"></param>
        /// <param name="str"></param>
        /// <returns>returns col number if exists, -1 if not</returns>
        public int searchInRow(int row, String str)
        {
            readerLock();
            searchLock();

            if (this.row < row)
            {
                readerReleaseLock();
                searchReleaseLock();
                return -1;
            }

            for (int i = 0; i < column; i++)
            {
                if (str == spreadSheet[row, i].ToString())
                {
                    return i;
                }
            }
            searchReleaseLock();
            readerReleaseLock();
            return -1;
        }

        /// <summary>
        /// perform search in specific col
        /// </summary>
        /// <param name="col"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public int searchInCol(int col, String str)
        {
            readerLock();
            searchLock();

            if (this.column < col) 
            {
                searchReleaseLock();
                readerReleaseLock();
                return -1;
            }

            for (int i = 0; i < row; i++)
            {
                if (str == spreadSheet[i, col].ToString()) 
                {
                    return i;
                }
            }
            searchReleaseLock();
            readerReleaseLock();
            return -1;
        }

        /// <summary>
        /// perform search within spesific range: [row1:row2,col1:col2]
        /// includes col1,col2,row1,row2
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        /// <param name="row1"></param>
        /// <param name="row2"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public Tuple<int, int> searchInRange(int col1, int col2, int row1, int row2, String str)
        {
            readerLock();
            searchLock();
            if ((col1 < 0) || (col2 < 0) || this.column < col1 || this.column < col2 || row2 < row1 || col2 < col1)
            {
                searchReleaseLock();
                readerReleaseLock();
                return null;
            }

            for (int i = row1; i < row2; i++)
            {
                for (int j = col1; j < col2; j++)
                {
                    if (str == spreadSheet[i, j])
                    {
                        searchReleaseLock();
                        readerReleaseLock();
                        return new Tuple<int, int>(i, j);
                    }
                }
            }

            searchReleaseLock();
            readerReleaseLock();
            return null;
        }

        /// <summary>
        /// add a row after row1
        /// </summary>
        /// <param name="row1"></param>
        public void addRow(int row1)
        {
            structureChangeLock();
            if (this.row < row1)
            {
                structureChangeReleaseLock();
                return;
            }
            string[,] str = new string[this.row +1, this.column];
            for (int i = 0; i < row1; i++) 
            {
                for (int j = 0; j < this.column; j++)
                {
                    str[i, j] = spreadSheet[i, j];
                }
            }
            for (int i = row1 + 2; i < this.row +1; i++)
            {
                for (int j = 0; j < this.column; j++)
                {
                    str[i, j] = spreadSheet[i-1, j];
                }
            }

            this.spreadSheet = str;
            this.row = row + 1;
            this.indexMutices = new Mutex[this.row];
            for (int i = 0; i < this.row; i++)
            {
                this.indexMutices[i] = new Mutex();
            }
            structureChangeReleaseLock();
        }

        /// <summary>
        /// add a column after col1
        /// </summary>
        /// <param name="col1"></param>
        public void addCol(int col1)
        {
            structureChangeLock();
            if (this.column < col1)
            {
                structureChangeReleaseLock();
                return;
            }

            String[,] temp_grid = new String[this.row, this.column + 1];

            for (int i = 0; i <= col1; i++)
            {
                for (int j = 0; j < this.row; j++)
                {
                    temp_grid[j, i] = this.spreadSheet[j, i];
                }
            }

            for (int i = col1 + 2; i < this.column + 1; i++)
            {
                for (int j = 0; j < this.row; j++)
                {
                    temp_grid[j, i] = this.spreadSheet[j, i - 1];
                }
            }
            this.spreadSheet = temp_grid;
            this.column = column +1;

            structureChangeReleaseLock();
        }

        /// <summary>
        /// perform search and return all relevant cells according to caseSensitive param
        /// </summary>
        /// <param name="str"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        public Tuple<int, int>[] findAll(String str, bool caseSensitive)
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();
            readerLock();
            searchLock();
            for (int i = 0; i < this.row; i++)
            {
                for (int j = 0; j < this.column; j++)
                {
                    if (caseSensitive)
                    {
                        if (str.Equals(this.spreadSheet[i, j]))
                        {
                            result.Add(new Tuple<int, int>(i, j));
                        }
                    }
                    else
                    {
                        if (str.ToLower().Equals(this.spreadSheet[i, j].ToLower()))
                        {
                            result.Add(new Tuple<int, int>(i, j));
                        }
                    }
                }
            }

            searchReleaseLock();
            readerReleaseLock();
            Tuple<int,int>[] tuples = new Tuple<int,int>[result.Count];
            for (int i=0; i<tuples.Length;i++)
            {
                tuples[i] = result[i];
            }
            return tuples;
        }

        /// <summary>
        /// replace all oldStr cells with the newStr str according to caseSensitive param
        /// </summary>
        /// <param name="oldStr"></param>
        /// <param name="newStr"></param>
        /// <param name="caseSensitive"></param>
        public void setAll(String oldStr, String newStr, bool caseSensitive)
        {
             
        }

        /// <summary>
        /// return the size of the spreadsheet in nRows, nCols
        /// </summary>
        /// <returns></returns>
        public Tuple<int, int> getSize()
        {
            readerLock();
            Tuple<int,int> result = new Tuple<int, int>(this.row, this.column);
            readerReleaseLock();
            return result;
        }

        /// <summary>
        /// this function aims to limit the number of users that can perform the search operations concurrently.
        /// The default is no limit. When the function is called, the max number of concurrent search operations is set to nUsers. 
        /// In this case additional search operations will wait for existing search to finish.
        /// This function is used just in the creation
        /// </summary>
        /// <param name="nUsers"></param>
        public void setConcurrentSearchLimit(int nUsers)
        {
            structureChangeLock();
            this.searchSemaphore = new SemaphoreSlim(nUsers-1, nUsers);
            structureChangeReleaseLock();
        }


        /// <summary>
        /// save the spreadsheet to a file fileName.
        /// you can decide the format you save the data. There are several options.
        /// </summary>
        /// <param name="fileName"></param>
        public void save(String fileName)
        {
            structureChangeLock();
            string filePathName = fileName;
            if(File.Exists(filePathName))
                File.Delete(filePathName);

            using (StreamWriter streamWriter = File.AppendText(filePathName))
            {
                streamWriter.WriteLine(row);
                streamWriter.WriteLine(column);
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < column; j++)
                    {
                        streamWriter.WriteLine(spreadSheet[i, j]);
                    }
                }
            }
            structureChangeReleaseLock();
        }

        /// <summary>
        /// load the spreadsheet from fileName
        /// replace the data and size of the current spreadsheet with the loaded data
        /// </summary>
        /// <param name="fileName"></param>
        public void load(String fileName)
        {
            structureChangeLock();

            structureChangeReleaseLock();
        }
    }
}
