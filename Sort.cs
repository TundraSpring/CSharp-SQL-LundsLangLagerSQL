using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LundsLångLagerSQL
{
    internal class Sort
    {
        private const string strConnection = @"Data Source=JOSEF\SQLEXPRESS; Initial Catalog=LundsLångLager; Integrated Security=true; TrustServerCertificate = true;";
        
        /// <summary>
        /// Oscar
        /// </summary>
        public void Run()
        {
            try
            {
                bool success = ProcessHalvpallar();
                Console.WriteLine(success ? "Process successfully finished" : "Process failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }


        /// <summary>
        /// Oscar
        /// </summary>
        /// <returns></returns>
        //Metod för att hantera halvpallar
        public bool ProcessHalvpallar()
        {
            using (SqlConnection connection = new SqlConnection(strConnection))
            {
                connection.Open();

                try
                {
                    //Här hämtar man listan för singel havpallarna från databasen
                    List<Pallet> lonelyPallets = GetLonelyPallets(connection);

                    //Bearbetar halvpallarna så länge det finns två
                    while (lonelyPallets.Count > 1)
                    {
                        //Hämtar StorageID från två singel halvpallar
                        int storageId1 = (int)lonelyPallets[0].storageID;
                        int storageId2 = (int)lonelyPallets[1].storageID;

                        //här uppdaterar den ena halvpallen till den andra halvpallens StorageID, så det får samma 
                        UpdateStorageId(connection, lonelyPallets[1].palletID, storageId1);

                        //Hämtar listan av singel halvpallar igen
                        lonelyPallets = GetLonelyPallets(connection);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error has occured: ", ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Oscar
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        //Denna metoden hämtar halvpallarna från databasen
        public List<Pallet> GetLonelyPallets(SqlConnection connection)
        {
            //Lista för att lagra singel halvpallar
            List<Pallet> lonelyPallets = new List<Pallet>();

            try
            {
                using (SqlCommand command = new SqlCommand("spGetLonelyPallets", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //Skapar en Pallet-objekt och lägger till det i listan
                            Pallet pallet = new Pallet((int)reader["PalletID"], (int)reader["StorageID"]);
                            lonelyPallets.Add(pallet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }

            return lonelyPallets;
        }

        /// <summary>
        /// Oscar
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="palletId"></param>
        /// <param name="storageId"></param>
        //Metod för att uppdatera storageID för en Halvpall
        public void UpdateStorageId(SqlConnection connection, int palletId, int storageId)
        {
            try
            {
                using (SqlCommand command = new SqlCommand("spSortLonelyPallets", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@StorageID", storageId);
                    command.Parameters.AddWithValue("@PalletID", palletId);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured: ", ex.Message);
            }
        }

    }
}
