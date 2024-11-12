using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LundsLångLagerSQL
{
    internal class PalletIO
    {
        //Om det inte står något namn ovanför en metod så är det en metod som Josef har skapat för att lägga ihop
        //de olika metoderna. (t.ex menyn och dess metoder)


        

        

        //Kolla igenom dagboken



        
        
        
        
        
        PalletManager palletManager = new PalletManager();
        public void Run()
        {




            palletManager.CreateArrayForStorageSpots();
            PalletManager.FindPalletsInDatabase();
            while (true)
            {
                DisplayUserOptions();
                string userInput = GetUserInput();
                bool exitProgram = ApplyUserInput(userInput);
                if (exitProgram)
                {
                    Console.Clear();
                    break;
                }
                else
                    ClearDisplayAfterUserInput();
            }
        }

        private void DisplayUserOptions()
        {
            Console.WriteLine("Choose an alternative:\n" +
                              "1: Store a pallet\n" +
                              "2: Retrieve a pallet with a specific ID\n" +
                              "3: Search for pallet with a specific ID\n" +
                              "4: Move pallet from one place to another\n" +
                              "5: Print out list of all stored pallets\n" +
                              "6: Count number of pallets stored during a chosen timespan\n" +
                              "7: Count profits made during a chosen timespan\n" +
                              "8: Sort storage so that halfpallets are placed together\n" +
                              "0: Exit program");
        }

        private string GetUserInput()
        {
            while (true)
            {
                string userInput = Console.ReadLine();
                if (userInput == "1" || userInput == "2" || userInput == "3" || userInput == "4" || userInput == "5" || userInput == "6" || userInput == "7" || userInput == "8" || userInput == "0")
                {
                    return userInput;
                }
            }
        }

        private bool ApplyUserInput(string userInput)
        {
            bool exitProgram = false;
            switch (userInput)
            {
                case "1": //Ju
                    PalletManager.AddPallet();
                    break;
                case "2": //Jo
                    TryToRetrievePallet();
                    break;
                case "3": //Jo
                    SearchForPallet();
                    break;
                case "4": //Os
                    MovePalletFromOnePlaceToAnother();
                    break;
                case "5": //Os
                    palletManager.PrintStorage();
                    break;
                case "6": //Jo VG
                    CountPalletsStoredDuringTimespan();
                    break;
                case "7": //Jo VG
                    ShowPalletProfits();
                    break;
                case "8": //Os VG
                    //palletManager.PalletSortingFromHell();
                    Sort sort = new Sort();
                    sort.Run();
                    break;
                case "0":
                    exitProgram = true;
                    break;
            }
            return exitProgram;
        }

        /// <summary>
        /// (Josef) Tries to retrieve a pallet
        /// </summary>
        private void TryToRetrievePallet()
        {
            Console.WriteLine("Type in the ID of the pallet that you want to retrieve");
            string requestedPalletID = Console.ReadLine();
            int storageCost = palletManager.TryToRemovePallet(requestedPalletID);
            if (storageCost != -1 && storageCost != 2) //-1 betyder att pallen inte finns, -2 betyder att dess
            {                                          //data inte kunde bli läst
                Console.WriteLine("\nRetrieved the pallet.\n" +
                                  "Cost: " + storageCost);
            }
            else if (storageCost == -1)
            {
                Console.WriteLine("\nThe pallet is not in the Storage");
            }
            else // storageCost == -2
            {
                Console.WriteLine("\nERROR: FAILED TO PARSE PALLET DATA");
            }
        }

        /// <summary>
        /// (Josef) returns data of pallet with parameter as it's palletID
        /// </summary>
        private void SearchForPallet()
        {
            Console.WriteLine("Type in the ID of the pallet you want to search for");
            string requestedPalletID = Console.ReadLine();
            Pallet pallet = palletManager.GetPalletData(requestedPalletID);
            if (pallet is null)
                Console.WriteLine("\nThe pallet is not in the storage");
            else
            {
                string palletType = Utilities.GetPalletTypeFromInt(pallet.palletSize);
                Console.WriteLine("\nThe pallet is in the storage.\n" +
                                  "Pallet ID: {0}\n" +
                                  "Time it was stored: {1}\n" +
                                  "Pallet Type: {2}\n" +
                                  "Storage spot: {3}",
                                  pallet.palletID, pallet.arrivalTime, palletType, pallet.storageID);
            }
        }

        /// <summary>
        /// (Josef) What it says on tin. One of the VG parts.
        /// </summary>
        private void CountPalletsStoredDuringTimespan()
        {
            Console.WriteLine("Put in the date and time that pallets must be stored AFTER.");
            DateTime minDateTime = GetDateTimeInput();

            Console.WriteLine("\nPut in the date and time that pallets must be stored BEFORE.");
            DateTime maxDateTime = GetDateTimeInput();

            //Istället för att hämta båda listor på en gång så körs den två gånger, en för varje palltyp.
            //Det är mer flexibelt på detta sättet, även om det tar lite extra tid
            List<Pallet> wholePallets = palletManager.GetPalletSizeCount(minDateTime, maxDateTime, "100");
            List<Pallet> halfPallets = palletManager.GetPalletSizeCount(minDateTime, maxDateTime, "50");
            Console.WriteLine("\nTotal Whole pallets: {0}\n" +
                              "Total halfpallets: {1}", wholePallets.Count, halfPallets.Count);
        }

        /// <summary>
        /// (Josef) Makes a datetime value from user input
        /// </summary>
        /// <returns></returns>
        private DateTime GetDateTimeInput()
        {
            Console.WriteLine("YYYY-MM-DD-HH-MM-SS-mmm. Year may be no lower than 1753"); //1753 är minimi-värdet i
            while (true)                                                                  //sqls datetime
            {
                try
                {
                    string strInput = Console.ReadLine(); //Få input
                    string[] strInputArray = strInput.Split("-");
                    int[] input = new int[7];
                    for (int i = 0; i < 7; i++) //konvertera input
                    {
                        input[i] = Int32.Parse(strInputArray[i]);
                    }
                    if (input[0] < 1753) //se till att inputs Year-värde inte är mindre än 1753
                    {
                        throw new Exception();
                    }
                    DateTime dateTime = new DateTime(input[0], input[1], input[2], input[3], input[4], input[5], input[6]);
                    return dateTime;
                }
                catch
                {
                    Console.WriteLine("Invalid data. Try again.");
                }
            }
        }

        /// <summary>
        /// (Josef) Shows pallet profits during a chosen timespan. One of the VG parts.
        /// </summary>
        private void ShowPalletProfits()
        {
            Console.WriteLine("Put in the date and time that pallets must be retrieved AFTER.");
            DateTime minDateTime = GetDateTimeInput();

            Console.WriteLine("\nPut in the date and time that pallets must be retrieved BEFORE.");
            DateTime maxDateTime = GetDateTimeInput();

            int profitsSum = palletManager.GetProfitsDuringTimespan(minDateTime, maxDateTime);
            if (profitsSum != -2)
                Console.WriteLine("\nProfits made during chosen timespan: " + profitsSum);
            else
                Console.WriteLine("\nERROR: FAILED TO PARSE PALLET DATA");
        }

        private void ClearDisplayAfterUserInput()
        {
            Console.ReadLine();
            Console.Clear();
        }

        /// <summary>
        /// Oscar
        /// </summary>
        private void MovePalletFromOnePlaceToAnother()
        {
            try
            {
                Console.Write("Enter PalletID you want to move: ");
                int palletID = int.Parse(Console.ReadLine());
                Console.WriteLine();
                Console.WriteLine("Storage size is 1 - 20");
                Console.Write("Enter StorageID you want to move to: ");
                int newStorageID = int.Parse(Console.ReadLine());

                // Skapar ett nytt pall objekt
                Pallet pallet = new Pallet(palletID);

                // Flyttar pallt till nya Storage platsen
                palletManager.MovePallet(pallet, newStorageID);

                Console.WriteLine("Press any key to continue!");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
