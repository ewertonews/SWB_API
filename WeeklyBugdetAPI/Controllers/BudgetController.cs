using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using WeeklyBugdetAPI.Models;


namespace WeeklyBugdetAPI.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class BudgetController : ApiController
    {

        public static List<Budget> AllBudgets = new List<Budget>();
        public static List<string> usersId = new List<string>();

        [HttpGet]
        [Route("api/CreateBudget")]
        public HttpResponseMessage CreateBudget(string usrId)
        {
            
            if (usersId.Contains(usrId))
            {
                return Request.CreateResponse(HttpStatusCode.NotAcceptable, "User already exists");
                throw new InvalidOperationException();
            }
            else
            {
                Budget budget = new Budget(usrId);
                AllBudgets.Add(budget);
                usersId.Add(usrId);
                return Request.CreateResponse(HttpStatusCode.OK, budget);
            }
        }


        [HttpGet]
        [Route("api/CalculateBudget")]
        public HttpResponseMessage calculateBudget(DayOfWeek cycleEnds, double saldo, string userId)
        {
            getBudgetFromUser(userId).Saldo = saldo;

            DateTime today = DateTime.Today;

            int lastDayOfMonth = DateTime.DaysInMonth(today.Year, today.Month);

            string completeDateOfLastDayOfMonth = String.Format("{0}/{1}/{2}", lastDayOfMonth.ToString(), today.Month.ToString(), today.Year.ToString());

            DateTime lastBussnessDayOfMonth = getLastBussnessDayOfMonth(Convert.ToDateTime(completeDateOfLastDayOfMonth));

            var remainingDays = lastBussnessDayOfMonth.Day - today.Day;

            double amountPerDay = Math.Round(saldo / (remainingDays + 1), 2);
            //dailyBudget = amountPerDay;

            List<String> budget = new List<String>();

            double weekNbudget = 0;
            String weekBudget;

            //24
            DateTime currentDate = today;

            //lastDayOfMonth = 31
            while (today.Day != lastBussnessDayOfMonth.Day)
            {
                List<int> daysOfweekN = new List<int>();
                do
                {
                    daysOfweekN.Add(today.Day);
                    weekNbudget = weekNbudget + amountPerDay;
                    if (!(today.DayOfWeek.ToString()).Equals(cycleEnds))
                    {
                        today = today.AddDays(1);
                    }

                } while (today.DayOfWeek != cycleEnds && today.Day != lastBussnessDayOfMonth.Day);

                daysOfweekN.Add(today.Day);

                if (today.DayOfWeek == cycleEnds)
                {
                    weekNbudget = weekNbudget + amountPerDay;
                    if (currentDate.Day == today.Day)
                    {
                        weekBudget = String.Format("{0}:{1}", today.Day.ToString(), amountPerDay.ToString());
                    }
                    else
                    {
                        weekBudget = String.Format("{0} a {1}:{2}", daysOfweekN[0], today.Day.ToString(), weekNbudget.ToString());
                    }

                    getBudgetFromUser(userId).WeeklyBudget.Add(daysOfweekN, weekNbudget);
                    weekNbudget = 0;
                    today = today.AddDays(1);
                }
                else
                {
                    weekNbudget = weekNbudget + amountPerDay;
                    weekBudget = String.Format("{0} a {1}:{2}", daysOfweekN[0], today.Day.ToString(), weekNbudget.ToString());
                    getBudgetFromUser(userId).WeeklyBudget.Add(daysOfweekN, weekNbudget);
                }
                budget.Add(weekBudget);

            }


            //return budget;
            return Request.CreateResponse(HttpStatusCode.OK, budget);
        }
               

        private static double GetAmountPerDay(double saldo, DateTime start, DateTime end )
        {
            var remainingDays = end.Day - start.Day;

            double amountPerDay = Math.Round(saldo / (remainingDays + 1), 2);
            return amountPerDay;
        }



        [HttpGet]
        [Route("api/Save")]
        public HttpResponseMessage save(string userId, int month, double amount)
        {
            var matchedMonth = getBudgetFromUser(userId).savings.Find((SavedPair) => SavedPair.Value == month);

            if (!matchedMonth.Equals(null))
            {
                var newSavedAmount = new KeyValuePair<double, int>(matchedMonth.Key + amount, matchedMonth.Value);
            }else
            {
                getBudgetFromUser(userId).savings.Add(new KeyValuePair<double, int>(amount, month));
            }           

            return Request.CreateResponse(HttpStatusCode.OK, getBudgetFromUser(userId).savings);
        }

        [HttpGet]
        [Route("api/AddFromLastWeekToThisWeekOnly")]
        public HttpResponseMessage addFromLastWeekToThisWeekOnly(int day, string userId)
        {
            var previousAmount = getLastWeekbudget(day, userId);
            var currentWeekBudget = getBudgetFromUser(userId).WeeklyBudget[getWeekOfGivenDay(day, userId)];
            getBudgetFromUser(userId).WeeklyBudget[getWeekOfGivenDay(day, userId)] = currentWeekBudget + previousAmount;

            return Request.CreateResponse(HttpStatusCode.OK, getBudgetFromUser(userId).WeeklyBudget);
        }

        [HttpGet]
        [Route("api/AddFromLastWeekToRemainingWeeks")]
        public HttpResponseMessage addFromLastWeekToRemainingWeeks(int day, double amount, string userId)
        {
            var previousAmount = getLastWeekbudget(day, userId);

            var budgetWeeks = getBudgetFromUser(userId).WeeklyBudget.Keys.ToList();

            int indexOfweek = getIndexOfWeekOfGivenDay(day, userId);

            int amountOfRemainingWeeks = budgetWeeks.Count - indexOfweek;

            double splitBudget = previousAmount / amountOfRemainingWeeks;

            while (indexOfweek < budgetWeeks.Count)
            {
                getBudgetFromUser(userId).WeeklyBudget[budgetWeeks[indexOfweek]] = getBudgetFromUser(userId).WeeklyBudget[budgetWeeks[indexOfweek]]
                                + splitBudget;
                indexOfweek++;
            }

            return Request.CreateResponse(HttpStatusCode.OK, getBudgetFromUser(userId).WeeklyBudget);
        }

        [HttpGet]
        [Route("api/Spend")]
        public HttpResponseMessage Spend(int day, double amount, string userId)
        {

            var listWhereDayIs = getWeekOfGivenDay(day, userId);

            var oldValue = getBudgetFromUser(userId).WeeklyBudget[listWhereDayIs];

            var updatedWeekBudget = oldValue - amount;

            getBudgetFromUser(userId).WeeklyBudget[listWhereDayIs] = updatedWeekBudget;

            return Request.CreateResponse(HttpStatusCode.OK, Math.Round(updatedWeekBudget, 2));
            
        }

        [HttpGet]
        [Route("api/GetMyBudget")]
        public HttpResponseMessage getMyBudget(String userId)
        {
            var userBudget = getBudgetFromUser(userId).WeeklyBudget;
            List<string> formatedBudget = new List<string>();

            foreach (List<int> week in userBudget.Keys)
            {
                string weeklyBudget = String.Format("{0} a {1}: R$ {2}", week[0], week[week.Count - 1], Math.Round(userBudget[week], 2));
                formatedBudget.Add(weeklyBudget);
            }

            return Request.CreateResponse(HttpStatusCode.OK, formatedBudget);
        }

        private List<int> getWeekOfGivenDay(int day, string userId)
        {
            var userBugdet = getBudgetFromUser(userId).WeeklyBudget;
            var listWhereDayIs = userBugdet.Keys.First(list => list.Contains(day));

            return listWhereDayIs;
        }

        private int getIndexOfWeekOfGivenDay(int day, string userId)
        {
            int i = 0;

            var budgetWeeks = getBudgetFromUser(userId).WeeklyBudget.Keys.ToList();
            while (!budgetWeeks[i].Contains(day))
            {
                i++;
            }
            return i;
        }
        //check the amount left or devendo of last week, if != 0; ask what to do (save or subtract from weeks)
        private double getLastWeekbudget(int date, string userId)
        {
            int i = getIndexOfWeekOfGivenDay(date, userId);
            double lastWeekBudget = 0;
            var budgetWeeks = getBudgetFromUser(userId).WeeklyBudget.Keys.ToList();
            if (i > 0)//meaning that there is last week
            {
                lastWeekBudget = getBudgetFromUser(userId).WeeklyBudget[budgetWeeks[i - 1]];
            }
            return lastWeekBudget;
        }

        private Budget getBudgetFromUser(string userId)
        {
            return AllBudgets.Where(b => b.UserId == userId).ToList()[0];
        }
        private DateTime getDateFromCycleEndsDay(DayOfWeek cycleEndsDay, DateTime limitDate)
        {

            DateTime dateCycleEnd = DateTime.Now.Date;
            int dateOfCycleEndsDay = dateCycleEnd.Day;

            if (dateCycleEnd.DayOfWeek != cycleEndsDay && dateCycleEnd.CompareTo(limitDate) != 0)
            {
                while (dateCycleEnd.DayOfWeek != cycleEndsDay && dateCycleEnd.CompareTo(limitDate) != 0)
                {
                    dateCycleEnd = dateCycleEnd.AddDays(1);
                }
                
            }

            return dateCycleEnd;
        }
        //data no formato 22/04/2016    
        private DateTime getLastBussnessDayOfMonth(DateTime lastDayOfMonth)
        {
            if (Convert.ToDateTime(lastDayOfMonth).DayOfWeek == DayOfWeek.Sunday  || Convert.ToDateTime(lastDayOfMonth).DayOfWeek == DayOfWeek.Saturday)
            {
                return getLastBussnessDayOfMonth(lastDayOfMonth.AddDays(-1));
            }
            else
            {
                return lastDayOfMonth;
            }
        }
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}