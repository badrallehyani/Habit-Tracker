using StackExchange.Redis;
using System.Text.RegularExpressions;

class RedisHelper{
    private string _EndPoint;
    private ConnectionMultiplexer _ConMul;
    private IDatabase _database;

    public RedisHelper(string url){
        this._EndPoint = url;
        this._ConMul = ConnectionMultiplexer.Connect(url, x=>x.AllowAdmin=true);
        this._database = _ConMul.GetDatabase();
    }

    private string[] GetKeys(string pattern){
        return this._ConMul.GetServer(this._EndPoint)
        .Keys(pattern: pattern)
        .Select(e=>e.ToString())
        .ToArray();
    }

    static async Task<string> GetFomattedDate_(int addDays = 0){
        await Task.Delay(0); // just to make it async somehow to match with the demo getformatdate
        return DateTime.Now.Date.AddDays(addDays).ToString("yyyy-MM-dd");   
    }

    // ============ DEMO ============ 
    private async Task<string> GetFomattedDate(int addDays = 0){
        addDays += await this.GetAddedDays();
        return DateTime.Now.Date.AddDays(addDays).ToString("yyyy-MM-dd");   
    }

    public  async Task<string> AddToAddedDays(int userID){
        int currentAddedDays = await this.GetAddedDays();
        await this._database.StringSetAsync("added_days", currentAddedDays + 1);
        return "Ok";
    }

    private async Task<int> GetAddedDays(){
        return (int) await this._database.StringGetAsync("added_days");
    }
    // ============  ============ ============

    static string _GetFomattedDate(int addDays = 0){
        return DateTime.Now.Date.AddDays(addDays).ToString("yyyy-MM-dd");   
    }

    private async Task<Habit> GetHabit(int habitID){
        string habitNameInDB = RecordNameCreator.Habit(habitID);
        string habitName = await _database.HashGetAsync(habitNameInDB, "name");
        string habitDesc = await _database.HashGetAsync(habitNameInDB, "desc");
        
        string habitStreakString = await _database.HashGetAsync(habitNameInDB, "streak");
        int  habitStreak = int.Parse(habitStreakString);

        return new Habit(){
            ID = habitID,
            Name = habitName,
            Desc = habitDesc,
            Streak = habitStreak
        };
    }

    public async Task<bool> InitDB(){
        await this._ConMul.GetServer(this._EndPoint).FlushAllDatabasesAsync();
        await this._database.StringSetAsync("new_habit_id", "0");
        await this._database.StringSetAsync("new_habit_record_id", "0");

        await this._database.StringSetAsync("added_days", "0"); // demo-related

        await this._database.StringSetAsync(
            "users:1:hashed_password", 
            "$2a$10$HlpBkjXQ70pKB6YLEoj1h.znJubbLM4g1t1hgtRfQvLzrnrXg2IOO" // hashed "Demo"
        );
        return true;
    }

    private async Task<int> GetAndUpdateStreak(int habitID, int userID, bool plusOne = false){
        // Checking Yesterday record 
        string yesterday = await GetFomattedDate(-1);
        string yesterdayRecordName = RecordNameCreator.HabitRecord(habitID, userID, yesterday);
        string IsDoneYesterday = await this._database.StringGetAsync(yesterdayRecordName);
        // Reading Current Streak
        string habitRecordName = RecordNameCreator.Habit(habitID);
        string habitStreakString = await this._database.HashGetAsync(habitRecordName, "streak");
        int habitStreak = int.Parse(habitStreakString);
        // Getting The Streak based on whether habit was done yesterday or not
        int newStreak;
        if(IsDoneYesterday != "1"){
            newStreak = 0;
        }else{
            newStreak = habitStreak;
        }
        // this is used in MarkDone to make sure that updated streak is correct
        if(plusOne)
            newStreak++;

        // setting the streak in database
        await this._database.HashSetAsync(habitRecordName, "streak", newStreak);

        return newStreak;
    }

    private async Task<HabitRecord> HabitRecordNameInDBToHabitRecord(string habitRecordNameInDB){
        // habitRecordNameInDB -> habits_records:4:1:2024-07-17
        //                     -> habits_records:<habitID>:<UserID>:<YYYY-MM-DD>

        string habitIsDone = await _database.StringGetAsync(habitRecordNameInDB);

        string habitIDString = new Regex(@":(\d+):")
                            .Match(habitRecordNameInDB)
                            .Groups[1]
                            .Value.ToString();

            int habitID = int.Parse(habitIDString);
            Habit habit = await this.GetHabit(habitID);

            return new HabitRecord(){
                HabitID = habit.ID,
                Name = habit.Name,
                Desc = habit.Desc,
                Streak = habit.Streak,
                IsDone = habitIsDone == "1",
            };
    }


    private async Task<HabitRecord> CreateOneHabitRecord(int userID, Habit habit){
        string today = await GetFomattedDate();

        string recordName = RecordNameCreator.HabitRecord(habit.ID, userID, today);
        await _database.StringSetAsync(recordName, "0");
        
        int newStreak = await this.GetAndUpdateStreak(habit.ID, userID);
        habit.Streak = newStreak;

        return new HabitRecord(){
            HabitID = habit.ID,
            Name = habit.Name,
            Desc = habit.Desc,
            Streak = habit.Streak,
            IsDone = false,
        };
    }

    private async Task<HabitRecord[]> CreateHabitRecords(int userID){
        Habit[] habits = await GetHabits(userID);
        HabitRecord[] habitsRecords = new HabitRecord[habits.Length];
        for(int i = 0; i != habits.Length; i++){
            var habit = habits[i];
            habitsRecords[i] = await CreateOneHabitRecord(userID, habit);
        }
        return habitsRecords;
    }

    private async Task<RedisValue[]> GetHabitsIDs(int userID){
        string recordName = RecordNameCreator.UserHabits(userID);
        return await this._database.ListRangeAsync(recordName);
    }
    
    
    
    
    
    public async Task<Habit[]> GetHabits(int userID){
        RedisValue[] habitsIDs = await GetHabitsIDs(userID);
        Habit[] habits = new Habit[habitsIDs.Length];

        for (int i = 0; i != habitsIDs.Length; i++){
            int habitID = int.Parse(habitsIDs[i]);
            Habit habit = await this.GetHabit(habitID);
            habits[i] = habit;
        }

        return habits;
    }


    public async Task<HabitRecord[]> GetTodayRecords(int userID){
        string today = await GetFomattedDate();

        string pattern = RecordNameCreator.AllHabitRecordsToday(userID, today);
        
        string[] habitsRecordsInDB = this.GetKeys(pattern);
        // example:
            // 1) habits_records:1:1:2024-07-15
            // 2) habits_records:3:1:2024-07-16
            // 3) habits_records:3:1:2024-07-17

        var habitsRecords = new HabitRecord[habitsRecordsInDB.Length];

        if(habitsRecords.Length == 0){
            return await CreateHabitRecords(userID);
        }

        for(int i = 0; i != habitsRecordsInDB.Length; i++){
            string habitRecordInDB = habitsRecordsInDB[i];
            habitsRecords[i] = await HabitRecordNameInDBToHabitRecord(habitRecordInDB);
        }

        return habitsRecords;
    }

    public async Task<string> AddHabit(RequestBody.AddHabit body){
        string name = body.Name;
        string desc = body.Desc?? ""; // ?? ""     is to make sure we dont pass a null to HashSetAsync
        int userID = body.UserID;

        string habitIDString = await this._database.StringGetAsync("new_habit_id");
        int habitID = int.Parse(habitIDString);
        await _database.StringSetAsync("new_habit_id", habitID + 1);

        string recordName = RecordNameCreator.Habit(habitID);
        var fields = new HashEntry[]{
            new HashEntry("name", name),
            new HashEntry("desc", desc),
            new HashEntry("streak", "0")
        };
        // adding the new habit to database
        await _database.HashSetAsync(recordName, fields);

        // adding the habit id to the user habits
        recordName = RecordNameCreator.UserHabits(userID);
        long response = await _database.ListLeftPushAsync(recordName, habitID);

        // creating a new record for the newly created habit
        await this.CreateOneHabitRecord(userID, new Habit{
            ID = habitID,
            Name = name,
            Desc = desc,
            Streak = 0
        });

        return "Ok";
    }

    public async Task<string> RemoveHabit(RequestBody.RemoveHabit body){
        int userID = body.UserID;
        int ID = body.ID;

        RedisValue[] habits = await this.GetHabitsIDs(userID);

        bool found = habits.ToList().IndexOf(ID.ToString()) != -1;
        if(!found){
            return "Not Found";
        }

        string recordName = RecordNameCreator.UserHabits(userID);
        long deletedItems = await this._database.ListRemoveAsync(recordName, ID);

        if(!deletedItems.Equals(1)){
            return "Not Ok";
        }

        return "Ok";

    }
    public async Task<string> EditHabit(RequestBody.EditHabit body){
        int ID = body.ID;
        int userID = body.UserID;
        string name = body.Name;
        string desc = body.Desc;

        RedisValue[] habits = await this.GetHabitsIDs(userID);

        bool found = habits.ToList().IndexOf(ID.ToString()) != -1;
        if(!found){
            return "Not Found";
        }

        string recordName = RecordNameCreator.Habit(ID);

        var fields = new HashEntry[]{
            new HashEntry("name", name),
            new HashEntry("desc", desc)
        };
        
        await _database.HashSetAsync(recordName, fields);

        return "Ok";
    }

    public async Task<string> MarkDone(RequestBody.MarkDone body){
        int userID = body.UserID;
        int habitID = body.HabitID;

        string today = await GetFomattedDate();
        string recordName = RecordNameCreator.HabitRecord(habitID, userID, today);

        string isDone = await this._database.StringGetAsync(recordName);
        if(isDone == null){
            return $"USER:HABIT_ID:TODAY {userID}:{habitID}:{today} Does Not Exist";
        }
        if(isDone == "1"){
            return "Already Done";
        }
        // updating streak
        int streak = await this.GetAndUpdateStreak(habitID, userID, plusOne: true);

        await this._database.StringSetAsync(recordName, "1");
        return "Ok";
    }

    public async Task<string> GetToken(RequestBody.GetToken body){
        int userID = body.UserID;
        string password = body.Password;

        Console.WriteLine($"userID: {userID}");
        Console.WriteLine($"Password: {password}");

        string passwordRecordName = RecordNameCreator.Password(userID);
        string correctHashedPassword = await this._database.StringGetAsync(passwordRecordName);

        if(correctHashedPassword == null){
            return "Error: User Does Not Exist";
        }

        if(!BCrypt.Net.BCrypt.Verify(password, correctHashedPassword)){
            return "Error: Invalid Password";
        }

        string newToken = System.Guid.NewGuid().ToString();
        string tokenRecordName = RecordNameCreator.Token(userID);
        await this._database.StringSetAsync(tokenRecordName, newToken);

        return newToken;
    }
}