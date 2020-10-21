using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml; //needed to read/convert RSS feed into XML to get values from 
using System.ServiceModel.Syndication; //need to read RSS feed and grab elements from RSS to find trending words installed through NuGet Package Manager
using System.ServiceModel.Configuration;
using System.IO; //Needed to use streamreader to read CSV
using ServiceStack; //need in order to interact with AlphaVantage API. Installed through NuGet
using ServiceStack.Serialization;

namespace WSBYOLO
{
    // Enums for menu options
    enum Menu { Debug ,Trending, Verbose, TestAPI, Exit }

    class Program
    {
        static void Main(string[] args)
        {
            //set Console size to specific size so show all the stuff on console
            Console.SetWindowSize(100, 60);
            //call menu function
            MenuOptions.MainMenu();

        }
    }
    //main menu switch loop
    public class MenuOptions
    {
        public static void MainMenu()
        {
            //Print welcome onto console
            Console.WriteLine("Welcome to the WallStreetBets Stock YOLO APP\nThis app can tell you all about trending stocks on WSB and what to YOLO your money towards\n\nThe app pulls RSS titles from the r/WSB subreddit and will find stock symbols that are being mentioned on the front page of r/WSB");

            //Print WSB ASCII
            ASCIIImages.PrintWSB();

            //While loop to ask user what they want to do. While loop continues until valid option is chosen
            int menuOne = 99;
            //Ask user what they would like to do with the app
            while (menuOne != 6)
            {
                Console.WriteLine("\nWhat would you like to do?\n1: Give me the trending\n2: Run in VERBOSE (default sort to Hot)\n3: Test API (Checks MSFT)\n4: Exit");
                //ask user input what menu option they would like to execute
                menuOne = Convert.ToInt32(Console.ReadLine());
                // switch statement to execute menu function
                switch(menuOne)
                {
                    //will open sub menu to pass value to findstock method
                    case (int)Menu.Trending:
                        //Call trending function
                        Console.WriteLine("How would you like to get the subreddit data?\n1: Top\n2: Rising\n3: Hot\n4: New \n5: Exit");
                        int sortMenu = 0;
                        while (sortMenu != 6)
                        {
                            sortMenu = Convert.ToInt32(Console.ReadLine());
                            switch (sortMenu)
                            {
                                case 1:
                                    Stocks.FindStock(1);
                                    sortMenu = 6; //return it back to originally menu after finding stock
                                    break;
                                case 2:
                                    Stocks.FindStock(2);
                                    sortMenu = 6; //return it back to originally menu after finding stock
                                    break;
                                case 3:
                                    Stocks.FindStock(3);
                                    sortMenu = 6; //return it back to originally menu after finding stock
                                    break;
                                case 4:
                                    Stocks.FindStock(4);
                                    sortMenu = 6; //return it back to originally menu after finding stock
                                    break;
                                case 5:
                                    sortMenu = 6; //return it back to originally menu after finding stock
                                    break;
                                default:
                                    Console.WriteLine("Invalid input");
                                    break;
                            }
                        }
                        break;
                    //run program in verbose
                    case (int)Menu.Verbose:
                        //CALL VERBOSE MODE
                        Stocks.FindStockVerbose();
                        break;
                    //test the api by just running the API method
                    case (int)Menu.TestAPI:
                        Stocks.TestAPI();
                        break;
                    //Exit the environment
                    case (int)Menu.Exit:
                        //Exit the environment
                        Environment.Exit(0);
                        break;
                    default: //tell user to try again clean up the console
                        Console.WriteLine("Invalid input try again!");
                        Console.Clear();
                        break;
                }
            }
        }
    }

    //Class that interacts with Alpha Advantage API and returns all the stock stuff
    public class Stocks
    {
        //AlphaVantage can return the following values for the stocks passed through
        //set the values from AlphaVantage API and retrieve the values when called 
        public decimal Open { get; set; } // opening value of stock
        public decimal High { get; set; } // high value of stock
        public decimal Low { get; set; } // low value of stock
        //public decimal Close { get; set; } //closing value of stock
        public decimal Volume { get; set; } //totoal volume traded in last 24 hours

        private static List<string> NYSEStocks()
        {
            //new instance of StreamReader to read CSV of NYSE listed stocks
            StreamReader csvRead = new StreamReader("nyse-listed_csv.csv");//stored in bin/debug folder

            //New string list to store NYSE stock symbols
            List<string> nyseStocks = new List<string>();

            //string to use to pass in while for streamreader
            string currentLine;

            //While the current line isn't Null split the CSV into an array using ','
            //add the split into the nyseStocks array but only the 0 index items which should be the Stock symbol
            while((currentLine = csvRead.ReadLine()) != null)
            {
                string[] line = currentLine.Split(',');
                nyseStocks.Add(line[0]);
            }
            csvRead.Close(); //close th csvRead instance to save resources
            return nyseStocks;

        }

        // return the stock symbols that match the NYSE list, two parameters how to sort the subreddit and stocklist to compare against
        public static void FindStock(int input)
        {
            //how to sort the subreddit and pass that value into url string below
            string sort = "new";
            if(input == 1)
                    { sort = "top"; }
            else if(input == 2)
                    { sort = "rising"; }
            else if(input == 3)
                    { sort = "hot"; }
            else if(input == 4)
                    { sort = "new"; }

            //subreddit address that we get trendng for
            string url = $"https://www.reddit.com/r/wallstreetbets/{sort}.rss";

            //list to store the trending keywords
            string stringToCompare = ReadReddit.GetTrending(url);

            //list of stock symbols from CSV.
            List<string> listedStock = NYSEStocks();

            //pull trending stocks from string
            List<string> listToCompare = ReadReddit.FindInTrending(stringToCompare);


            //loop through the listToCompare and if listedStock contains the item
            //if stock is contained then pass stock symbol to AlphaVantage API
            //Console.WriteLine($"There are {listToCompare.Count} possible trending symbols");


            // if there are matching symbols this int will incremement by 1 according to the if statement
            // if the matching int stays at zero below statement returns no trending
            int matching = 0; 
            foreach(var symb in listToCompare)
            {
                if(listedStock.Contains(symb))
                {
                    try //catch error argument out of range error caused in line 193 when CSVReader looks for index out of range
                        //Possible time issue with CSV reader?
                    {
                        
                        var apiKey = "IFN5P986UUWQM8M1"; //Key to access Alpha advantage API distributed by AlphaAdvantage

                        //create and object called stock that interacts with AlphaAdvantage API
                        //API requires three parameteres passed function, sympbol, and apiKey
                        //Included datatype which the API will then output in comma seperated value file
                        //Use get string to read string of provided by the URL then parse the date using FromCsv
                        //Stores the data in the StockShow class with the properties specified in the class
                        var stock = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symb}&apikey={apiKey}&datatype=csv"
                        .GetStringFromUrl().FromCsv<List<Stocks>>();
                        


                        var maxPrice = stock.Max(u => u.High); //get the closing value of returned stock
                        var minPrice = stock.Min(u => u.Low); //get the lowest value of stock
                                                              //var closing = stock.Max(u => u.Close); //get the closing 
                        var volume = stock.Max(u => u.Volume); //get the total volume of sales
                        var open = stock.Max(u => u.Open); // get the opening value
                        matching++; //
                        Console.WriteLine($"\nYou should YEET your money towards {symb:c}\nOpening value: {open:c}\nHigh: {maxPrice:c}\nLow:  {minPrice:c}\nTotal Volume: {volume:c}");
                    }
                    catch(ArgumentOutOfRangeException e) //Error thrown
                    {
                        Console.WriteLine("CSV READER ENCOUNTERED AN ERROR PLEASE CLOSE APP AND OPEN AGAIN"); //throw error waring
                        string log = e.ToString(); //move this to logging function of app (future implementation)
                    }
                }
            }
            //if the loop above did not increment matching it means non of the trending words matched any known symbols so return no match
            if(matching == 0)
            {
                Console.WriteLine("No stocks trending today!");
            }
            ASCIIImages.PrintRocket();//print a sweet rocket
        }

        //VERBOSE to check if API is returning. Will show stock symbols found in trending and also being pulled from NYSE CSV
        public static void FindStockVerbose()
        {
            //show that the gettrending function works
            string stringToCompare = ReadReddit.GetTrending("https://www.reddit.com/r/wallstreetbets/hot.rss");
            Console.WriteLine("\n\n\n VERBOSE Building string of RSS titles from subreddit.....");
            Console.WriteLine(stringToCompare);

            //show that fetching NYSE symbols works
            Console.WriteLine("\n\n\n VERBOSE Building list of NYSE stock symbols from CSV.....");
            List<string> stocks = Stocks.NYSEStocks();
            foreach (var item in stocks)
            {
                Console.Write($"<<{item}>>");
            }

            //FindInTrendingVerbose
            Console.WriteLine("\n\n\nVERBOSE Building list of words that are possibly Stock Symbols.....");
            List<string> listToCompare = ReadReddit.FindInTrending(stringToCompare);
            foreach(var item in listToCompare)
            {
                Console.Write($"<<{item}>>");
            }

            //FindStock Verbose
            Console.WriteLine("\n\n\n VERBOSE COMPARING STOCKS......");
            Stocks.FindStock(1);
        }
        //test to see if API is returning values
        public static void TestAPI()
        {
            try
            {
                string symb = "MSFT";
                //var symbol = $"{symb}"; //sets the symbol to one of the words in the trendingList
                var apiKey = "IFN5P986UUWQM8M1"; //Key to access Alpha advantage API distributed by AlphaAdvantage

                //create and object called stock that interacts with AlphaAdvantage API
                //API requires three parameteres passed function, sympbol, and apiKey
                //Included datatype which the API will then output in comma seperated value file
                //Use get string to read string of provided by the URL then parse the date using FromCsv
                //Stores the data in the StockShow class with the properties specified in the class
                var stock = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symb}&apikey={apiKey}&datatype=csv"
                .GetStringFromUrl().FromCsv<List<Stocks>>();

                var maxPrice = stock.Max(u => u.High); //get the closing value of returned stock
                                                       //var closing = stock.Max(u => u.Close); //closing amount
                var minPrice = stock.Max(u => u.Low); //get the lowest value of stock
                var volume = stock.Max(u => u.Volume); //get the total volume of sales
                var open = stock.Max(u => u.Open); // get the opening value
                Console.WriteLine($"\nYou should YEET your money towards {symb:c}\nOpening value: {open:c}\nHigh: {maxPrice:c}\nLow:  {minPrice:c}\nTotal Volume: {volume:c}");
                ASCIIImages.PrintRocket();
            }
            catch (ArgumentOutOfRangeException e) //Error thrown
            {
                Console.WriteLine("CSV READER THREW AN ERROR PLEASE CLOSE APP AND OPEN AGAIN"); //throw error warning
                string log = e.ToString(); //move this to logging function of app (future implementation)
            }
        }

    }
    //class to store ASCII and print images
    public class ASCIIImages
    {
        //print rocketship that we ride to the mood
        public static void PrintRocket()
        {
            string rck = @"
                           *     .--.
                                / /  `
               +               | |
                      '         \ \__,
                  * +'--' *
                      +   /\
         +              .'  '.   *
                *      /======\      +
                      ;:.  _;
                      |:. (_)  |
                      |:.  _   |
            +         |:. (_)  | *
                      ;:.WSB   ;
                    .' \:.    / `.
                   /.- '':._.'`-. \
                   |/    /||\    \|
                   _..--````````--.._
           _.- '``                    ``' -._
         - '                                ' -
                    ";

            Console.WriteLine(rck.PadLeft(100, ' '));
        }

        public static void PrintWSB()
        {
            //Print onto console wWSB ASCII icon
            string wsb = @"                                                            
                            @@@@@@@@                        
                       ,,,,,,**,,,,,,,,&                    
                  ,,,*//*//*,,,,,@//,,,**                   
                  */(@*/*,.@/((////*@##@*@                  
             @*****,//(((@(((((#,,,,,,,,,.                  
                ***/,*,,,,,,,,,,,,,,,,,,.@                  
                ,(%(@,,,.@@@@@@@@@@@,,@@@@@@@               
.                  /*.,,,@@@@@@@@@@@@.@@@@@@@               
,, @*            .**,,,,,,* @@@@@@@@,@@@@@@@                
*,.&. @         %,***.,,,,,,*@@@@..,, ,,,,@                 
,,,%,  @&&&/     ,,,,,,,,,,,,,,,,,,**,,,,#               .*,
      ,&&&&&&&&&&&&@&&&&@,,,,,,,,,,,,,,*@           *@.@,,,@
   @  &&&&&&&&&&&&&@&&&@    %@@@@@@@@&@&&&&&&&&&&&&&&&@ .@,,
        @&&&&&&&@&&@@&&@   @%%%,...&&&&&&&&&&&&&&&&&&&&@    
             @@@&&&@&&&&@   @%    &@@@&&@&&&&&&&&&&&&&&@    
               @&&&&&@@&&@  %#&  &&&@&&&&&&&&&&&&@@@        
               @&&&&&&&&@&@%%%% &&&@&&&&&@                  
               @&&&&&&&&&&@@%%%&&@&&&&&&&@                  
               @&&&&&&&&&&&&&&@&&&&&&&&&&@                  
               @&&&&&&&&&&&&&&&&&&&&&&&&@                   
               @&&&&&&&&&&&&&&,&&&&&&&&&@                   
               @&&&&&&&&&&&&&&*@&&&&&&&&@                   
                 .@@@@@@@&&&&&&@&&&&&@@@ ";
            Console.WriteLine(wsb.PadLeft(40, ' '));
        }
    }

    //Class to read and get trending stock values from reddit
    public class ReadReddit
    {
        //method to get the trending stock symbols from RSS feed of WSB extracting 3-4 letter words
        public static string GetTrending(string url)
        {
            //new method instance of XMLreader. Creates an XML of the RSS URL that's passed through
            //SyndicationFeed only takes XmlReader variables
            XmlReader readrss = XmlReader.Create(url);

            //Loads XML created by readrss and assigns it to the feed value
            SyndicationFeed feed = SyndicationFeed.Load(readrss);

            //Close XmlReader (documentation says it's needed for resource management)
            readrss.Close();

            //string to store stock symbols found
            string stockSyms = "";

            //For each Item in the RSS feed find the elements wrapped in <Title> and store it in stockSyms string
            foreach (SyndicationItem sym in feed.Items)
            {
                stockSyms += sym.Title.Text;//takes RSS items wrapped in <Title> and stores them in the string
            }
            return stockSyms;

        }
        public static List<string> FindInTrending(string stockSyms)
        {
            //Turn the string into a list of stock symbols
            //First clean up the string of common symbols
            //Then split the string by ' ' and store in the list

            //List to store stock symbols
            List<string> trendingStocks = new List<string>();

            //clean up the string
            stockSyms = stockSyms.Replace(",", "");//remove commas from string
            stockSyms = stockSyms.Replace(".", "");//remove peroids
            stockSyms = stockSyms.Replace("-", "");//remove dashes
            stockSyms = stockSyms.Replace(":", "");//remove colons 
            stockSyms = stockSyms.Replace("!", "");//remove exclamations
            stockSyms = stockSyms.Replace("?", "");//remove question marks
            //stockSyms = stockSyms.ToUpper();

            //split the stockSyms string by ' ' and store in word array
            string[] symArray = stockSyms.Split(' ');

            //loop through symArray and store in a list
            foreach(var sym in symArray)
            {
                //if the word is 3 or 3 characters long then continue
                if(sym.Length == 3 || sym.Length == 4)
                {
                    //if the first three characters in the word are all caps then stor in trendingStocks array
                    //ALL Caps for first three are more likely to be stock symbols
                    if (Char.IsUpper(sym[0]) == true && (Char.IsUpper(sym[1]) == true) && (Char.IsUpper(sym[2]) == true))
                    {
                        //if the trending stocks list doesn't already contain the sym then add it. Prevents duplicates
                        if(!trendingStocks.Contains(sym))
                        {
                            trendingStocks.Add(sym);
                        }
                    }
                }
            }

            return trendingStocks;
        }
     }
}
