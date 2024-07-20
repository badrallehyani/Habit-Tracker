// Home.razor

public class HabitRecord{
    public int HabitID { get; set; }
    public bool IsDone { get; set; }
    public string Name { get; set; }
    public string Desc { get; set; }
    public int Streak {get; set;}
    //  ShowDesc is used to control showing the description
    public bool ShowDesc { get; set; } = false;
}
public class Habit{
    public int ID {get; set;}
    public string Name {get; set;}
    public string Desc {get; set;}

    public bool Visible {get; set;} = true;
    public bool Editable {get; set; } = false;

    public string Nameinput {get; set;}
    public string DescInput {get; set;}

    public Habit(int ID, string Name, string Desc){
        this.ID = ID;
        this.Name = Name;
        this.Desc = Desc;

        this.Nameinput = Name;
        this.DescInput = Desc;

        this.Visible = true;
        this.Editable = false;
    }
}

namespace RequestBody{
    public class MarkDone{
        public required int HabitID { get; set; }
    }

    class AddHabit{
        public required string Name {get; set;}
        public required string Desc {get; set;}
    }

    class RemoveHabit{
        public required int ID;
    }

    class EditHabit{
        public int ID {get; set;}
        public string Name {get; set;}
        public string Desc {get; set;}
    }

    class Login{
        public required int UserID {get; set;}
        public required string Password {get; set;}
    }

}
