class HabitRecord{
    public required int HabitID {get; set;}
    public required bool IsDone {get; set;}
    public required string Name {get; set;}
    public required string Desc {get; set;}
    public required int Streak {get; set;}
}

class Habit{
    public required int ID {get; set;}
    public required string Name {get; set;}
    public required string Desc {get; set;}
    public required int Streak {get; set;}
}

namespace RequestBody{

    interface IHasUserID{
        public int UserID {get;set;}
    }

    class MarkDone: IHasUserID{
        public required int HabitID {get; set;}
        public int UserID {get; set;}
    }

    class AddHabit: IHasUserID{
        public required string Name {get; set;}
        public required string Desc {get; set;} = "";
        public int UserID {get; set;}
    }

    class RemoveHabit: IHasUserID{
        public required int ID {get; set;}
        public int UserID {get; set;}
    }
    
    class EditHabit: IHasUserID{
        public required int ID {get; set;}
        public required string Name {get; set;}
        public required string Desc {get; set;} = "";
        public int UserID {get; set;}
    }

    class GetToken{
        public required int UserID {get; set;}
        public required string Password {get; set;}
    }
}
