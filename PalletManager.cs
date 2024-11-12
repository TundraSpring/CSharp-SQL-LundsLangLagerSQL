using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LundsLångLagerSQL
{
    internal class PalletManager
    {
        //Connection-strängen behöver variera från dator till dator, så ändra på denna om programmet inte körs på Josefs dator.
        private const string strConnection = @"Data Source=JOSEF\SQLEXPRESS; Initial Catalog=LundsLångLager; Integrated Security=true; TrustServerCertificate = true;";

        /// <summary>
        /// (Josef) Removes a pallet and returns (total storage cost for said pallet) or (-1 if the pallet doesn't exist) or (-2 if data of the pallet failed to be parsed)
        /// </summary>
        /// <returns></returns>
        public int TryToRemovePallet(string requestedPalletID)
        {
            if (!Int32.TryParse(requestedPalletID, out _))
                return -1; //skickar -1 om skickad sträng inte kan göras till en int
            Pallet pallet = GetPalletData(requestedPalletID);
            if (pallet is not null)
            {
                AddPalletToArchive(pallet); //Pallåningar arkiveras innan pallar hämtas ut
                RemovePallet(requestedPalletID);
                return CalculateTotalPalletCost(pallet.arrivalTime, DateTime.Now, pallet.palletSize);
            }
            else
                return -1;
        }

        /// <summary>
        /// (Josef) Removes pallets that have the sent parameter as their ID
        /// </summary>
        /// <param name="requestedPalletID"></param>
        private void RemovePallet(string requestedPalletID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(strConnection)) //
                                                                                    //make strConnection into a parameter?
                {
                    string strQuery = "Update Pallet " +
                                      "SET StorageID = NULL " +
                                      "WHERE PalletID = " + requestedPalletID;
                    SqlCommand command = new SqlCommand(strQuery, connection);

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }


        /// <summary>
        /// (Josef) Returns -2 if it fails to parse pallet data
        /// </summary>
        /// <param name="strArrivalTime"></param>
        /// <param name="strPalletType"></param>
        /// <returns></returns>
        //This method could be static if necessary
        private int CalculateTotalPalletCost(DateTime? nullableArrivalTime, DateTime? nullableDepartureTime, int palletSize)
        {
            try
            {
                DateTime arrivalTime = (DateTime)nullableArrivalTime;
                DateTime departureTime = (DateTime)nullableDepartureTime;
                double costPerHour = palletSize * 0.8;
                TimeSpan timeSpan = departureTime.Subtract((DateTime)arrivalTime);
                return (timeSpan.Hours + 1) * (int)costPerHour;
            }
            catch
            {
                return -2;
            }
        }

        /// <summary>
        /// (Josef) Sends an object of pallet with the specified ID. Returns null if it doesn't exist.
        /// </summary>
        /// <param name="requestedPalletID"></param>
        /// <returns></returns>
        public Pallet GetPalletData(string requestedPalletID)
        {
            Pallet pallet = null; //skickar tillbaka null om pallen inte finns
            if (!Int32.TryParse(requestedPalletID, out _))
                return pallet;
            try
            {
                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    string query = "SELECT PalletID, PalletSize, StorageID, ArrivalTime " +
                                   "FROM Pallet " +
                                   "WHERE PalletID = " + requestedPalletID + " AND StorageID IS NOT NULL";
                    SqlCommand command = new SqlCommand(query, connection);

                    connection.Open();
                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        while (dataReader.Read()) //Om pallen finns så sparas dess nuvarande data i ett Pallet-objekt,
                        {                         //som sedan skickas tillbaka
                            int palletID = Convert.ToInt32(dataReader["PalletID"]);
                            int palletSize = Convert.ToInt32(dataReader["PalletSize"]);
                            int? storageID = ConvertDatabaseValueToInt32(dataReader["StorageID"]);
                            DateTime arrivalTime = Convert.ToDateTime(dataReader["ArrivalTime"]);
                            pallet = new Pallet(palletID, palletSize, storageID, arrivalTime);
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
            return pallet;
        }

        /// <summary>
        /// (Josef) Converts a potential NUll sql value to a int? value
        /// </summary>
        /// <param name="databaseValue"></param>
        /// <returns></returns>
        private int? ConvertDatabaseValueToInt32(object databaseValue)
        {
            string palletID = Convert.ToString(databaseValue);
            bool isValueNotNull = Int32.TryParse(palletID, out int intValue);
            if (isValueNotNull)
            {
                return intValue;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// (Josef) Converts a potential NUll sql value to a DateTime? value
        /// </summary>
        /// <param name="databaseValue"></param>
        /// <returns></returns>
        private DateTime? ConvertDatabaseValueToDateTime(object databaseValue)
        {
            string palletID = Convert.ToString(databaseValue);
            bool isValueNotNull = DateTime.TryParse(palletID, out DateTime dateTimeValue);
            if (isValueNotNull)
            {
                return dateTimeValue;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// (Josef) Uses a pallet object to add a row to the archive table. Vg.
        /// </summary>
        /// <param name="pallet"></param>
        private void AddPalletToArchive(Pallet pallet)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    string query = "INSERT INTO Archive (palletID, StorageID, ArrivalTime, DepartureTime) " +
                                   "SELECT p.PalletID, p.StorageID, p.ArrivalTime, GETDATE() " +
                                   "FROM Pallet p " +
                                   "WHERE p.PalletID = " + pallet.palletID;
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }

        /// <summary>
        /// (Josef) Returns a list of pallets stored during a chosen timespan, with a specified size. VG.
        /// </summary>
        /// <param name="minDatetime"></param>
        /// <param name="maxDatetime"></param>
        /// <param name="strPalletSize"></param>
        /// <returns></returns>
        public List<Pallet> GetPalletSizeCount(DateTime minDatetime, DateTime maxDatetime, string strPalletSize)
        {
            List<Pallet> pallets = new List<Pallet>();
            try
            {
                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    string sqlMinParam = Utilities.GetSQLDateTimeString(minDatetime);
                    string sqlMaxParam = Utilities.GetSQLDateTimeString(maxDatetime);

                    string sqlQuery = "SELECT p.* " +
                                      "FROM Pallet p " +
                                      "JOIN Archive a " +
                                      "  ON p.PalletID = a.PalletID " +
                                      "WHERE a.ArrivalTime >= cast(" + sqlMinParam + "as datetime) AND a.DepartureTime <= cast(" + sqlMaxParam + "as datetime) AND p.PalletSize = " + strPalletSize;
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    connection.Open();
                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            int palletID = Convert.ToInt32(dataReader["PalletID"]);
                            int palletSize = Convert.ToInt32(dataReader["PalletSize"]);
                            int? storageID = ConvertDatabaseValueToInt32(dataReader["StorageID"]);
                            DateTime? arrivalTime = ConvertDatabaseValueToDateTime(dataReader["ArrivalTime"]);
                            Pallet pallet = new Pallet(palletID, palletSize, storageID, (DateTime)arrivalTime);
                            pallets.Add(pallet);

                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
            return pallets;
        }

        /// <summary>
        /// (Josef) Returns profits earned during a chosen timespan
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public int GetProfitsDuringTimespan(DateTime startTime, DateTime endTime)
        {
            int profitsSum = 0;
            string sqlMinParam = Utilities.GetSQLDateTimeString(startTime);
            string sqlMaxParam = Utilities.GetSQLDateTimeString(endTime);
            //Data retrieved from the sql query is stored in the three List<> below.
            //The first pallet haas arrivalTimes[0], departureTimes[0] and palletSize[0]
            List<DateTime?> arrivalTimes = new List<DateTime?>();
            List<DateTime?> departureTimes = new List<DateTime?>();
            List<int> palletSizes = new List<int>();

            try
            {
                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    string sqlQuery = "SELECT a.ArrivalTime, a.DepartureTime, p.PalletSize " +
                                      "FROM Archive a " +
                                      "JOIN Pallet p " +
                                      "  ON a.PalletID = p.PalletID " +
                                      "WHERE a.DepartureTime >= cast(" + sqlMinParam + " as datetime) AND a.DepartureTime <= cast(" + sqlMaxParam + " as datetime)";
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    connection.Open();
                    using (SqlDataReader dataReader = command.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            arrivalTimes.Add(ConvertDatabaseValueToDateTime(dataReader["ArrivalTime"]));
                            departureTimes.Add(ConvertDatabaseValueToDateTime(dataReader["DepartureTime"]));
                            palletSizes.Add(Convert.ToInt32(dataReader["PalletSize"]));
                        }
                    }
                    connection.Close();
                    //After all pallet data is retireved, it's time to start calculating the profits
                    for (int i = 0; i < palletSizes.Count; i++)
                    {
                        int palletProfit = CalculateTotalPalletCost(arrivalTimes[i], departureTimes[i], palletSizes[i]);
                        if (palletProfit == -2)
                            return -2;
                        profitsSum += palletProfit;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
            return profitsSum;
        }


        /// <summary>
        /// Oscar
        /// </summary>
        /// <param name="pallet"></param>
        /// <param name="newStorageID"></param>
        // Denna metoden flyttar en Pallet till en ny Storage spot
        public void MovePallet(Pallet pallet, int newStorageID)
        {
            //If else satsen kollar om det är möjligt att flytta Pallet
            if (CanMovePallet(pallet, newStorageID))
            {
                //Om vilkoren är fyllda så uppdateras Pallets StorageID
                UpdatePalletStorage(pallet, newStorageID);
                Console.WriteLine("Pallet moved!");
            }
            else
            {
                //Om vilkoren inte uppfylls!
                Console.WriteLine("Unable to move pallet to that location!");
            }
        }


        /// <summary>
        /// Oscar
        /// </summary>
        /// <param name="pallet"></param>
        /// <param name="newStorageID"></param>
        /// <returns></returns>
        // Denna metoden kontrollerar om det går att flytta Pallet till en ny Storage spot
        public static bool CanMovePallet(Pallet pallet, int newStorageID)
        {
            try
            {
                if (newStorageID < 1 || newStorageID > 20)
                {
                    Console.WriteLine($"Invalid StorageID: {newStorageID}");
                    return false;
                }

                using (SqlConnection GatesOfHell = new SqlConnection(strConnection))
                {
                    GatesOfHell.Open();

                    //Kollar om pallen finns
                    string checkPalletExistsQuery = "Select count(*) from Pallet where PalletID = @palletID AND StorageID IS NOT NULL";
                    SqlCommand checkPalletExistsCmd = new SqlCommand(checkPalletExistsQuery, GatesOfHell);
                    checkPalletExistsCmd.Parameters.AddWithValue("@palletID", pallet.palletID);
                    int palletCount = (int)checkPalletExistsCmd.ExecuteScalar();
                    if (palletCount > 0)
                    {
                        // Hämtar Pallet
                        string getPalletSizeQuery = "SELECT PalletSize FROM Pallet WHERE PalletID = @palletID";
                        using (SqlCommand getSizeCmd = new SqlCommand(getPalletSizeQuery, GatesOfHell))
                        {


                            getSizeCmd.Parameters.AddWithValue("@palletID", pallet.palletID);
                            pallet.palletSize = (int)getSizeCmd.ExecuteScalar();

                            if (pallet.palletSize == 50)
                            {
                                //Kollar om den nya Storage platsen har en pallet med palletsize 50 och 100 
                                string countSize50Query = "SELECT COUNT(*) FROM Pallet WHERE StorageID = @newStorageID AND PalletSize = 50";
                                string countSize100Query = "SELECT COUNT(*) FROM Pallet WHERE StorageID = @newStorageID AND PalletSize = 100";

                                using (SqlCommand countSize50Cmd = new SqlCommand(countSize50Query, GatesOfHell))
                                using (SqlCommand countSize100Cmd = new SqlCommand(countSize100Query, GatesOfHell))
                                {
                                    countSize50Cmd.Parameters.AddWithValue("@newStorageID", newStorageID);
                                    countSize100Cmd.Parameters.AddWithValue("@newStorageID", newStorageID);

                                    int countSize50InNewStorage = (int)countSize50Cmd.ExecuteScalar();
                                    int countSize100InNewStorage = (int)countSize100Cmd.ExecuteScalar();

                                    // kollar om det finns mindre än 2 Pallets med palletsize 50 och att ingen pallet med Palletsize 100 finns på den nya Storage spot
                                    return countSize50InNewStorage < 2 && countSize100InNewStorage == 0;
                                }
                            }
                            else if (pallet.palletSize == 100)
                            {
                                //Kollar om det finns någon Pallet med palletsize 100 på den ny Storage spot
                                string countSize100Query = "SELECT COUNT(*) FROM Pallet WHERE StorageID = @newStorageID";

                                using (SqlCommand countSize100Cmd = new SqlCommand(countSize100Query, GatesOfHell))
                                {
                                    countSize100Cmd.Parameters.AddWithValue("@newStorageID", newStorageID);
                                    int countSize100InNewStorage = (int)countSize100Cmd.ExecuteScalar();

                                    //kollar så det inte finns en Pallet med PalletSize 100 på den nya Storage spot
                                    return countSize100InNewStorage == 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Can't move a Pallet doesn't exist!");
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
                return false;
            }
        }



        /// <summary>
        /// Oscar
        /// </summary>
        /// <param name="pallet"></param>
        /// <param name="newStorageID"></param>
        //Updaterar Pallet med den nya platsen i Storage
        public static void UpdatePalletStorage(Pallet pallet, int newStorageID)
        {
            try
            {
                using (SqlConnection GatesOfHell = new SqlConnection(strConnection))
                {
                    GatesOfHell.Open();
                    string query = "UPDATE Pallet SET StorageID = @newStorageID WHERE PalletID = @palletID";

                    using (SqlCommand cmd = new SqlCommand(query, GatesOfHell))
                    {
                        cmd.Parameters.AddWithValue("@palletID", pallet.palletID);
                        cmd.Parameters.AddWithValue("@newStorageID", newStorageID);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }

        /// <summary>
        /// Oscar
        /// </summary>
        public void PrintStorage()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    connection.Open();

                    string query = "select s.StorageID, p.PalletID, p.PalletSize, p.ArrivalTime " +
                                 "from Pallet p " +
                                 "full join Storage s " +
                                 "on p.StorageID = s.StorageID " +
                                 "where s.StorageID is not null " +
                                 "order by s.StorageID ";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Storage List (Palletsize Whole = 100 and Half = 50)\n");

                            Console.WriteLine("ID  PalletID  Palletsize         Arrival time ");

                            while (reader.Read())
                            {
                                int? storageID = reader.IsDBNull(0) ? null : (int?)reader.GetInt32(0);
                                int? palletID = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
                                int? palletSize = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
                                DateTime? arrivalTime = reader.IsDBNull(3) ? null : (DateTime?)reader.GetDateTime(3);


                                Console.WriteLine($"{storageID,2} {palletID,6} {palletSize,10} {arrivalTime,28}");

                            }
                        }
                    }
                }

                Console.WriteLine("\nPress any key to continue.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }

        /// <summary>
        /// Julia
        /// </summary>
        /// <param name="palletSize"></param>
        /// <param name="storageID"></param>
        /// <returns></returns>
        static bool FindAvailableSpot(int palletSize, int storageID)
        {
            try
            {
                //string connectionString = @"Data Source=JULIAS_DATOR\SQLEXPRESSCUSTOM ;Initial Catalog=LLL ;Integrated Security=true; TrustServerCertificate = true;";
                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    connection.Open();

                    //Hämtar pallstorleken som finns på vald plats i lagret
                    string getSizeFromStorage = "SELECT PalletSize FROM Pallet WHERE StorageID = @storageID";
                    using (SqlCommand getSizeFromStorageCommand = new SqlCommand(getSizeFromStorage, connection))
                    {
                        //här skickas @storageID in i StorageID vilket letar upp PalletSize på den platsen via kommandot ovan
                        getSizeFromStorageCommand.Parameters.AddWithValue("@StorageID", storageID);
                        int palletSizeInStorage = 0;

                        //dataReader läser in om det finns ett nullvärde innan if- och else if-satsen nedan börjar kolla efter 50 och 100-värden.
                        using (SqlDataReader dataReader = getSizeFromStorageCommand.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                palletSizeInStorage += (dataReader.GetInt32(0));
                            }
                        }

                        //om palletSize som följer med från AddPallet() är 50 hoppar den in i denna if-sats
                        if (palletSize == 50)
                        {
                            //Kollar om storageplatsen har en pallet med storlek 50 eller 100 
                            string checkForSize50 = "SELECT COUNT(*) FROM Pallet WHERE StorageID = @storageID AND PalletSize = 50";
                            string checkForSize100 = "SELECT COUNT(*) FROM Pallet WHERE StorageID = @storageID AND PalletSize = 100 ";

                            using (SqlCommand checkForSize50Command = new SqlCommand(checkForSize50, connection))
                            using (SqlCommand checkForSize100Command = new SqlCommand(checkForSize100, connection))
                            {
                                checkForSize50Command.Parameters.AddWithValue("@storageID", storageID);
                                checkForSize100Command.Parameters.AddWithValue("@storageID", storageID);

                                int checkForSize50InStorage = (int)checkForSize50Command.ExecuteScalar();
                                int checkForSize100InStorage = (int)checkForSize100Command.ExecuteScalar();

                                // kollar om det finns mindre än 2 pallar med pallstorlek 50 och att ingen pall med pallstorlek 100 finns på den nya platsen
                                return checkForSize50InStorage < 2 && checkForSize100InStorage == 0;
                            }
                        }

                        else if (palletSize == 100)
                        {
                            //Kollar om det finns någon pall med storlek 100 på den ny platsen
                            string checkForSize100 = "SELECT COUNT(*) FROM Pallet WHERE StorageID = @storageID";

                            using (SqlCommand checkForSize100Command = new SqlCommand(checkForSize100, connection))
                            {
                                checkForSize100Command.Parameters.AddWithValue("@storageID", storageID);
                                int checkForSize100InStorage = (int)checkForSize100Command.ExecuteScalar();

                                //kollar så det inte finns en pall med storlek 100 på den nya platsen
                                return checkForSize100InStorage == 0;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Julia
        /// </summary>
        public static void AddPallet()
        {
            //string connectionString = @"Data Source=JULIAS_DATOR\SQLEXPRESSCUSTOM ;Initial Catalog=LLL ;Integrated Security=true; TrustServerCertificate = true;";
            string insertCommand = $"INSERT INTO Pallet ( ArrivalTime, PalletSize, StorageId ) VALUES ( @ArrivalTime, @PalletSize, @StorageId );";

            using (SqlConnection connection = new SqlConnection(strConnection))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(insertCommand, connection))
                    {
                        Console.WriteLine("Add Pallet\n\n");
                        Console.WriteLine("Enter the number of the pallet size you want to add to the storage.");
                        Console.WriteLine("1. Whole\n2. Half");

                        //här skriver man in 1 för helpall och 2 för halvpall
                        int palletSizeInput = int.Parse(Console.ReadLine());
                        int palletSize = 0;

                        //om användaren väljer helpall används denna if-sats som tilldelar palletSize värdet 100
                        if (palletSizeInput == 1)
                        {
                            palletSize = 100;
                        }
                        //om användaren väljer halvpall används denna som tilldelar palletSize värdet 50
                        else if (palletSizeInput == 2)
                        {
                            palletSize = 50;
                        }
                        //om palletSizeInput inte är 1 eller 2 kommer detta meddelande upp
                        else
                        {
                            Console.WriteLine("Error.\nYou have entered an invalid size of the pallet.");
                        }

                        //detta kommer endast upp om användaren lagt in ett giltigt nummer och därmed fått en storlek bestämd på sin pall
                        if (palletSize == 50 || palletSize == 100)
                        {
                            //här skriver användaren in var hen vill lagra pallen
                            Console.WriteLine("\nEnter the storage ID between 1 - 20 you want to use for the pallet:");
                            int storageID = int.Parse(Console.ReadLine());

                            //om användaren skrivit in ett giltigt tal går den in i denna if-sats
                            if (storageID > 0 && storageID < 21)
                            {
                                bool EmptySpot = FindAvailableSpot(palletSize, storageID);

                                if (EmptySpot == true)
                                {
                                    DateTime arrivalTime = DateTime.Now;
                                    command.Parameters.AddWithValue("@ArrivalTime", arrivalTime);
                                    command.Parameters.AddWithValue("@PalletSize", palletSize);
                                    command.Parameters.AddWithValue("@StorageId", storageID);

                                    //här utförs kommandot och data läggs till i databasen
                                    int rowsAffected = command.ExecuteNonQuery();

                                    //om några rader blivit ändrade kommer detta meddelande upp
                                    if (rowsAffected > 0)
                                    {
                                        Console.WriteLine($"\nYour pallet has been placed at {storageID}");
                                        Console.WriteLine("Thank you for using Lunds Långlager\n");
                                    }
                                }

                                else if (EmptySpot == false)
                                {
                                    Console.WriteLine("Error. There is not enough room in that spot.");
                                }
                            }

                            //om användaren skriver in ett för högt eller för lågt storage-ID kommer det inte gå att lägga in någon pall
                            else
                            {
                                Console.WriteLine("Error. There are only 20 storage spots at Lunds Långlager.");
                            }
                        }
                    }
                }
                //skulle något gå fel kommer detta meddelande upp
                catch (Exception ex)
                {
                    Console.WriteLine($"Error.\n {ex.Message}");
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Julia
        /// </summary>
        public static void FindPalletsInDatabase()
        {
            //string connectionString = @"Data Source=JULIAS_DATOR\SQLEXPRESSCUSTOM ;Initial Catalog=LLL ;Integrated Security=true; TrustServerCertificate = true;";
            string countPallets = "SELECT COUNT(*) FROM Pallet WHERE StorageID IS NOT NULL";
            //räknar alla pallar som finns i lagret

            using (SqlConnection connection = new SqlConnection(strConnection))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand countPalletsCommand = new SqlCommand(countPallets, connection))
                    {
                        int palletCount = (int)countPalletsCommand.ExecuteScalar();

                        //om det inte finns några pallar i lagret läggs de till med denna metod i if-satsen
                        if (palletCount == 0)
                        {
                            AddInitialPallets(connection);
                        }

                        //om det redan finns pallar i lagret kommer inget läggas till
                        else
                        {
                            Console.WriteLine($"There are {palletCount} pallets in storage. New pallets will not be initialized.\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error.\n {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Julia
        /// </summary>
        /// <param name="connection"></param>
        public static void AddInitialPallets(SqlConnection connection)
        {
            try
            {
                DateTime arrivalTime = DateTime.Now;

                //lägger till tre halvpallar
                for (int i = 1; i <= 3; i++)
                {
                    int storageID = i;
                    int palletSize = 50;
                    string insertPallet = $"INSERT INTO Pallet(ArrivalTime, PalletSize, StorageId) VALUES(@ArrivalTime, @PalletSize, @StorageId);";

                    using (SqlCommand insertPalletCommand = new SqlCommand(insertPallet, connection))
                    {
                        insertPalletCommand.Parameters.AddWithValue("@ArrivalTime", arrivalTime);
                        insertPalletCommand.Parameters.AddWithValue("@PalletSize", palletSize);
                        insertPalletCommand.Parameters.AddWithValue("@StorageId", storageID);

                        insertPalletCommand.ExecuteNonQuery();
                    }
                }

                //lägger till tre helpallar
                for (int i = 4; i <= 6; i++)
                {
                    int storageID = i;
                    int palletSize = 100;
                    string insertPallet = $"INSERT INTO Pallet(ArrivalTime, PalletSize, StorageId) VALUES(@ArrivalTime, @PalletSize, @StorageId);";

                    using (SqlCommand insertPalletCommand = new SqlCommand(insertPallet, connection))
                    {
                        insertPalletCommand.Parameters.AddWithValue("@ArrivalTime", arrivalTime);
                        insertPalletCommand.Parameters.AddWithValue("@PalletSize", palletSize);
                        insertPalletCommand.Parameters.AddWithValue("@StorageId", storageID);

                        insertPalletCommand.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("Six pallets have been added to the database.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }

        /// <summary>
        /// Julia (Josef gjorde andra halvan)
        /// </summary>
        public void CreateArrayForStorageSpots()
        {
            //string connectionString = @"Data Source=JULIAS_DATOR\SQLEXPRESSCUSTOM ;Initial Catalog=LLL ;Integrated Security=true; TrustServerCertificate = true;";

            //i dessa listor ska värden från databasen lagras
            List<int> listOfInvalidStorageIDs = new List<int>();
            List<int> listOfStorageIDs = new List<int>();
            int[] storageIDsToBeAdded = new int[20];

            try
            {
                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    connection.Open();

                    string findStorageSpots = "SELECT StorageID FROM Storage";

                    using (SqlCommand findStorageSpotsCommand = new SqlCommand(findStorageSpots, connection))
                    {
                        using (SqlDataReader readStorageSpots = findStorageSpotsCommand.ExecuteReader())
                        {
                            while (readStorageSpots.Read())
                            {
                                int? storageValue = readStorageSpots["StorageID"] as int?;

                                if (storageValue > 20 || storageValue < 1)
                                {
                                    listOfInvalidStorageIDs.Add(storageValue.Value);
                                }

                                if (storageValue >= 1 && storageValue <= 20)
                                {
                                    listOfStorageIDs.Add(storageValue.Value);
                                }
                            }
                        }
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        storageIDsToBeAdded[i] = i + 1;
                    }

                    storageIDsToBeAdded = RemoveFromStorageIDsToBeAddedArray(storageIDsToBeAdded, listOfStorageIDs);
                    AddStorageSpots(storageIDsToBeAdded);
                    RemoveInvalidStorageIDs(listOfInvalidStorageIDs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }

        /// <summary>
        /// Josef
        /// </summary>
        /// <param name="storageIDsToBeAdded"></param>
        /// <param name="listOfStorageIDs"></param>
        /// <returns></returns>
        private int[] RemoveFromStorageIDsToBeAddedArray(int[] storageIDsToBeAdded, List<int> listOfStorageIDs)
        {
            //Ha en for-loop som går igenom alla element i listan som innehåller alla hämtade storageIDs (som inte är över 20 eller under 1)
            //storageIDsToBeAdded[listOfValidStorageIDs[i]] = 0;

            for (int i = 0; i < listOfStorageIDs.Count; i++)
            {
                storageIDsToBeAdded[listOfStorageIDs[i] - 1] = 0;
            }
            return storageIDsToBeAdded;
        }

        /// <summary>
        /// Julia
        /// </summary>
        /// <param name="storageIDsToBeAdded"></param>
        static void AddStorageSpots(int[] storageIDsToBeAdded)
        {
            try
            {
                //string connectionString = @"Data Source=JULIAS_DATOR\SQLEXPRESSCUSTOM ;Initial Catalog=LLL ;Integrated Security=true; TrustServerCertificate = true;";

                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    connection.Open();

                    string insertStorageSpots = "INSERT INTO Storage (StorageID) VALUES (@StorageID)";


                    for (int i = 0; i < storageIDsToBeAdded.Length; i++)
                    {
                        if (storageIDsToBeAdded[i] != 0)
                        {
                            using (SqlCommand insertStorageSpotsCommand = new SqlCommand(insertStorageSpots, connection))
                            {
                                insertStorageSpotsCommand.Parameters.AddWithValue("@StorageID", storageIDsToBeAdded[i]);

                                insertStorageSpotsCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }

        /// <summary>
        /// Josef
        /// </summary>
        /// <param name="listOfInvalidStorageIDs"></param>
        private void RemoveInvalidStorageIDs(List<int> listOfInvalidStorageIDs)
        {
            try
            {
                //string connectionString = @"Data Source=JULIAS_DATOR\SQLEXPRESSCUSTOM ;Initial Catalog=LLL ;Integrated Security=true; TrustServerCertificate = true;";

                using (SqlConnection connection = new SqlConnection(strConnection))
                {
                    connection.Open();

                    string insertStorageSpots = "DELETE FROM Storage WHERE StorageID = @StorageID";


                    for (int i = 0; i < listOfInvalidStorageIDs.Count; i++)
                    {
                        using (SqlCommand insertStorageSpotsCommand = new SqlCommand(insertStorageSpots, connection))
                        {
                            insertStorageSpotsCommand.Parameters.AddWithValue("@StorageID", listOfInvalidStorageIDs[i]);
                            insertStorageSpotsCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }
    }
}
