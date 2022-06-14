using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShareableSpreadSheet
{
    public class SharableSpreadSheet
    {
        private int row;                                    // Number of rows 
        private int column;                                 // Number of columns  
        public string[,] spreadSheet;                       // The main data resource holds the string values
        private Mutex[] rowIndexMutices;                    // Array of mutices responsible for lock the rows by index
        private Mutex readMutex;                            // Lock read mode operations
        private Mutex writeMutex;                           // Lock write mode exclusive operations
        private Mutex changeSpreadSheetStructureMutex;      // Lock operations that will change the main resource structure
        private Semaphore modeSwitcher;                     // Controls and switchs what operation is reflecting (read/write) 
        private SemaphoreSlim searchSemaphore;              // Privilege the number of searchers as described in setConcurrentSearchLimit func. documentation
        private int readers;                                // Counts number of readers
        private int writers;                                // Counts number of writers

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
            rowIndexMutices = new Mutex[nRows];
            for (int i = 0; i < nRows; i++)
                rowIndexMutices[i] = new Mutex();
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
            Interlocked.Increment(ref this.readers);
            if (readers == 1)
            {
                modeSwitcher.WaitOne();
                //Console.WriteLine("ReadLock-In");

            }
            readMutex.ReleaseMutex();
        }

        /// <summary>
        /// Lock releasing, read
        /// </summary>
        private void readerReleaseLock()
        {
            readMutex.WaitOne();
            Interlocked.Decrement(ref this.readers);
            if (readers == 0)
            {
                modeSwitcher.Release();
                //Console.WriteLine("ReadRealease-In");
            }
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
                throw new ArgumentException("Arguments provided are negative or out of range");
            }
            else
            {
                writeMutex.WaitOne();
                Interlocked.Increment(ref this.writers);
                if (writers == 1)
                {
                    modeSwitcher.WaitOne();
                    //Console.WriteLine("WriteLock-In");
                }
                writeMutex.ReleaseMutex();
                rowIndexMutices[rowIndex].WaitOne();
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
                throw new ArgumentException("Arguments provided are negative or out of range");
            }
            else
            {
                rowIndexMutices[rowIndex].ReleaseMutex();
                writeMutex.WaitOne();
                Interlocked.Decrement(ref this.writers);
                if (writers == 0)
                {
                    modeSwitcher.Release();
                    //Console.WriteLine("WriteRealse-In");
                }
                writeMutex.ReleaseMutex();
            }

        }

        /// <summary>
        /// Lock, search
        /// </summary>
        private void searchLock() 
        {

            if (searchSemaphore != null)
            {
                searchSemaphore.Wait();
                //Console.WriteLine("SearchLock-In");
            }

        }

        /// <summary>
        /// Lock releasing, search
        /// </summary>
        private void searchReleaseLock() 
        {

            if (searchSemaphore != null)
            {
                searchSemaphore.Release();
                //Console.WriteLine("SearchRelease-In");
            }
        }

        /// <summary>
        /// Lock, structure
        /// </summary>
        private void structureChangeLock()
        {

            changeSpreadSheetStructureMutex.WaitOne();
            modeSwitcher.WaitOne();
            //Console.WriteLine("StructureLock-In");

        }

        /// <summary>
        /// Lock releasing, structure
        /// </summary>
        private void structureChangeReleaseLock()
        {

            modeSwitcher.Release();
            changeSpreadSheetStructureMutex.ReleaseMutex();
            //Console.WriteLine("StructureRelease-In");

        }

        /// <summary>
        /// taks the string value of the cell by given row and column indexes
        /// </summary>
        /// <param name="row">row index</param>
        /// <param name="col">column index</param>
        /// <returns>returns the string value inside the cell</returns>
        public String getCell(int row, int col)
        {
            //Console.WriteLine("Get cell");
            if (this.row < row || this.column < col) 
            {
                throw new ArgumentOutOfRangeException($"Arguments supplied: row {row}:col {col}, are out of range");
            }

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
            //Console.WriteLine("Set cell");
            if (this.row < row || this.column < col) {
                throw new ArgumentOutOfRangeException($"Arguments supplied: row {row}:col {col}, are out of range");
            }

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
            //Console.WriteLine("Search string");
            readerLock();
            searchLock();
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    if (spreadSheet[i, j] != null)
                    {
                        if (spreadSheet[i, j].Contains(str))
                        {
                            searchReleaseLock();
                            readerReleaseLock();
                            return new Tuple<int, int>(i, j);
                        }
                    }
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
            //Console.WriteLine("Echange rows");
            structureChangeLock();
            if(this.row < row1 || this.row < row2 || row1<0 || row2 <0) 
            {
                structureChangeReleaseLock();
                throw new ArgumentOutOfRangeException($"Arguments supplied: row1 {row1}:row2 {row2}, are out of range");
            }

            if (row1 == row2)
            {
                structureChangeReleaseLock();
                return;
            }

            string[] str= new string[this.column];
            for (int i = 0; i < column; i++)
            {

                str[i] = spreadSheet[row1, i];
                spreadSheet[row1, i] = spreadSheet[row2, i];
                spreadSheet[row2, i] = str[i];
                
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
            //Console.WriteLine("Exchange columns");
            structureChangeLock();
            if (this.column < col1 || this.column < col2 || col1 <0 || col2 <0) 
            {
                structureChangeReleaseLock();
                throw new ArgumentOutOfRangeException($"Arguments supplied: col1 {col1}:col2 {col2}, are out of range");
            }

            if (col1 == col2)
            {
                structureChangeReleaseLock();
                return;
            }

            string[] str= new string[this.row];
            for (int i = 0; i < row; i++)
            {
                str[i] = spreadSheet[i, col1];
                spreadSheet[i,col1] = spreadSheet[i, col2];
                spreadSheet[i,col2] = str[i];
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
            //Console.WriteLine("Search in row");
            readerLock();
            searchLock();

            if (this.row < row)
            {
                readerReleaseLock();
                searchReleaseLock();
                throw new ArgumentOutOfRangeException($"Argument supplied: row {row} is out of range");
            }

            for (int i = 0; i < column; i++)
            {
                if (str == spreadSheet[row, i].ToString())
                {
                    searchReleaseLock();
                    readerReleaseLock();
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
            //Console.WriteLine("Search in column");
            readerLock();
            searchLock();

            if (this.column < col) 
            {
                searchReleaseLock();
                readerReleaseLock();
                throw new ArgumentOutOfRangeException($"Argument supplied: col {col} is out of range");
            }

            for (int i = 0; i < row; i++)
            {
                if (spreadSheet[i, col] != null)
                {
                    if (str == spreadSheet[i, col].ToString())
                    {
                        searchReleaseLock();
                        readerReleaseLock();
                        return i;
                    }
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
            //Console.WriteLine("Search in range");
            readerLock();
            searchLock();
            if ((col1 < 0) || (col2 < 0) || this.column < col1 || this.column < col2 || row2 < row1 || col2 < col1)
            {
                searchReleaseLock();
                readerReleaseLock();
                throw new ArgumentOutOfRangeException($"One or more arguments supplied: row1 {row1}:col1 {col1}:row2 {row2}:col2{col2}, are out of range");
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
            //Console.WriteLine("Add row");

            if (row1 > this.row)
            {
                throw new ArgumentOutOfRangeException($"Argument supplied: row1 {row1} is out of range");
            }
            structureChangeLock();

            string[,] str = new string[row + 1, column];
            
            Mutex[] rowTempLocker = new Mutex[row + 1];
            for (int i = 0; i <= row1; i++)
            {
                rowTempLocker[i] = rowIndexMutices[i];
                for (int j = 0; j < this.column; j++)
                {
                    str[i, j] = spreadSheet[i, j];
                }
            }

            rowTempLocker[row1 + 1] = new Mutex();
            for (int i = 0; i < this.column; i++)
            {
                str[row1 + 1, i] = ($"NewRow: ({row1+1},{i+1})");
            }
            this.row++;

            for (int i = row1 + 1; i < this.row - 1; i++)
            {
                rowTempLocker[i + 1] = rowIndexMutices[i];
                for (int j = 0; j < this.column; j++)
                {
                    str[i + 1, j] = spreadSheet[i, j];
                }
            }
            rowIndexMutices = rowTempLocker;
            this.spreadSheet = str;
            structureChangeReleaseLock();        
        }

        /// <summary>
        /// add a column after col1
        /// </summary>
        /// <param name="col1"></param>
        public void addCol(int col1)
        {

            //Console.WriteLine("Add col");

            if (col1 > this.column)
            {
                throw new ArgumentOutOfRangeException($"Argument supplied: row1 {col1} is out of range");
            }
            structureChangeLock();

            string[,] str = new string[row, column + 1];

            for (int i = 0; i < this.row; i++)
            {
                for (int j = 0; j <= col1; j++)
                {
                    str[i, j] = spreadSheet[i, j];
                }
            }

            for (int i = 0; i < this.row; i++)
            {
                str[i, col1 + 1] = ($"NewCol: ({i + 1},{col1 + 1})");
            }
            this.column++;

            for (int i = 0; i < this.row; i++)
            {
                for (int j = col1 + 1; j < this.column - 1; j++)
                {
                    str[i, j + 1] = spreadSheet[i, j];
                }
            }

            this.spreadSheet = str;
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
            //Console.WriteLine("Find all");
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
            //Console.WriteLine("Set all");
            readerLock();
            searchLock();
            for (int i = 0; i < this.row; i++)
            {
                for (int j = 0; j < this.column; j++)
                {
                    if (caseSensitive)
                    {
                        if (oldStr.Equals(this.spreadSheet[i, j]))
                        {
                            writerLock(i,j);
                            spreadSheet[i, j] = newStr;
                            writerReleaseLock(i,j);
                        }
                    }
                    else
                    {
                        if (oldStr.ToLower().Equals(this.spreadSheet[i, j].ToLower()))
                        {
                            writerLock(i, j);
                            spreadSheet[i, j] = newStr;
                            writerReleaseLock(i, j);
                        }
                    }
                }
            }
            searchReleaseLock();
            readerReleaseLock();
        }

        /// <summary>
        /// return the size of the spreadsheet in nRows, nCols
        /// </summary>
        /// <returns></returns>
        public Tuple<int, int> getSize()
        {
            //Console.WriteLine("Get size");
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
            //Console.WriteLine("Set concurrent");
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
            //Console.WriteLine("Save");
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
            //Console.WriteLine("Load");
            structureChangeLock();
            if (File.Exists(fileName))
            {
                structureChangeReleaseLock();
                return;
            }
            string filePathName = fileName;
            using (StreamReader streamReader = new StreamReader(filePathName)) 
            {
                int readedRow = Int32.Parse(streamReader.ReadLine());
                int readedColumn = Int32.Parse(streamReader.ReadLine());
                string[,] readedSpreadSheet = new string[readedRow, readedColumn];
                this.row = readedRow;
                this.column = readedColumn;
                this.spreadSheet = readedSpreadSheet;
                for (int i = 0; i < readedRow; i++)
                {
                    for (int j = 0; j < readedColumn; j++)
                    {
                        this.spreadSheet[i,j] = streamReader.ReadLine();
                    }
                }
            }
            structureChangeReleaseLock();
        }
    }
}
