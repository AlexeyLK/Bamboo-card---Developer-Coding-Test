# Bamboo-card---Developer-Coding-Test
## How to run
### Open MS Visual Studio:

Launch Microsoft Visual Studio.
Create a new console application:

Choose "Create a new project."
In the search bar, type "Console App" and select "Console App (.NET Core)."
Click "Next."
Provide a project name (e.g., "HackerNewsConsoleApp") and choose the save location.
Click "Create."
Add dependencies:

Open the HackerNewsConsoleApp.csproj file.
Add the System.Net.Http package to the dependencies section.

### Write code:

Open the Program.cs file.
Replace the existing code with the provided program code.
Run the application:

Press F5 or click "Start" to build and run the application.
Enter the number of top stories (n):

The console will prompt you to enter the number of top stories.
Enter a positive integer and press Enter.
View the results:

The program will display the top stories along with their details in the console.
Total execution time:

After displaying the results, the total execution time of the program will be shown in milliseconds.
Note: Make sure that your development environment has internet access to fetch data from the Hacker News API. The program uses a cache mechanism to optimize API requests and improve performance. The cache expiration time is set to 5 minutes by default.

### Assumptions and  changes

Although I added sorting the history by points, I noticed that the stories were stored in an already sorted form.
The program runs for quite a long time, which can be fixed by reworking the function for obtaining history data using multithreading
