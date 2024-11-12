using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LundsLångLagerSQL
{
    internal class Pallet
    {
        public readonly int palletID; //Tanken med att Pallet skulle ha readonly fält var att objekt av denna klass skulle
                                      //vara en "snapshot" av ett dåvarande pallobjekt, och att det därför inte var
                                      //rimligt att ändra på data i objekt av denna klass.
        public int palletSize;          //Oscar behövde ändra på palletsize i en av sina metoder av någon anledning,
                                        //så denna kunde inte vara readonly
        public readonly int? storageID;
        public readonly DateTime? arrivalTime;

        public Pallet(int PalletID) //Konstruktor som Oscar använde
        {
            this.palletID = PalletID;
        }

        public Pallet(int palletID, int? storageID) //Konstruktor som Oscar använde
        {
            this.palletID = palletID;
            this.storageID = storageID;
        }

        public Pallet(int palletID, int palletSize, int? storageID, DateTime arrivalTime) //Konstruktor som Josef använde
        {
            this.palletID = palletID;
            this.palletSize = palletSize;
            this.storageID = storageID;
            this.arrivalTime = arrivalTime;
        }
    }
}
