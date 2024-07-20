public class RecordNameCreator{
    public static string Habit(int ID){
        return $"habits:{ID}";
    }
    public static string HabitRecord(int habitID, int userID, string todayFormatted){
        return $"habits_records:{habitID}:{userID}:{todayFormatted}";
    }
    public static string AllHabitRecordsToday(int userID, string todayFormatted){
        return $"habits_records:*:{userID}:{todayFormatted}";
    }
    public static string UserHabits(int userID){
        return $"users:{userID}:habits";
    }
    public static string Password(int userID){
        return $"users:{userID}:hashed_password";
    }
    public static string Token(int userID){
        return $"token:{userID}";
    }
}