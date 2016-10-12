using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WeeklyBugdetAPI.Models
{
    public class Budget
    {
        public Dictionary<List<int>, double> WeeklyBudget { get; set; }
        public string UserId { get; set; }
        public double Saldo { get; set; }
        public List<KeyValuePair<double, int>> savings { get; set; }

        public Budget(string usrId)
        {
            WeeklyBudget = new Dictionary<List<int>, double>();
            UserId = usrId;
            Saldo = 0;
            savings = new List<KeyValuePair<double, int>>();
        }
    }
}