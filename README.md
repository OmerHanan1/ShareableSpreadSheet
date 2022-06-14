# ShareableSpreadSheet
In this project, we asked to make shareable spreadsheet (shareable: can be accessed by multiplying users). \
For make it possible, \
we had to set locks on resources, \
in a way that will prevent a deadlock or another reader-writer common problems (dirty write, dirty read, phantom, and so). \
</br>
The approach we have adopted is to separate the Spread Sheet object methods into sections, \
and each section will get a unique locking mechanism using the Spread Sheet object data members. \
The methods were separated into 4 groups: Reading, Writing, Structure changing, Searching. \
Each group have a unique locking mechanism responsibility. \
The lockers groups are: ReadLock, WriteLock, StructureChangeLock, SearchLock. \
(Every lock option has the mirroring operation. \
</br>
<b>For example: </b> \
ReadLock method that locking the related resources in Reading operations, \
have the opposite ReadLockRelease methos which releasing the locks). \
Each method has the same method structure inside of it. \
Every critical section of every function is wrapped by locking and releasing, \
the lock type depending on method purposes (methods groups). \
</br>
<b>For example,</b> \
the method “Search string” which searching for a cell contains a given string value: \
![image](https://user-images.githubusercontent.com/79142560/173582947-75196577-4383-4bbc-9ac1-989ce6ec3d3d.png) \
As can be seen, the method’s critical section is wrapped by Read Lock and Search Lock. \
</br>
The Spread Sheet object data members that used in order to be able to manage the locking system: (following comments explains each data members purpose). \
![image](https://user-images.githubusercontent.com/79142560/173583041-5b235267-113f-4b4f-a79a-02fb6162ecd5.png) \
A diagram represents the locking flow of an example method “Add row”: \
![image](https://user-images.githubusercontent.com/79142560/173583098-d054d7a2-acff-4f91-9d6e-1026c329fd05.png) \
