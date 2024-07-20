using System;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.WebAssembly.Http;

using Newtonsoft.Json;
using System.Globalization;

public class Helper{
    private HttpClient _httpClient;
    // NavigateToLogin is going to be set in LoadToHelper.razor
    public Action NavigateToLogin;

    public Helper(string BaseAddress){
        this._httpClient = new HttpClient(){
            BaseAddress = new System.Uri(BaseAddress)
        };
    }

    private async Task<T> GetRequest<T>(string requestUri, bool includeCreds = true){
        HttpRequestMessage requestMessage = new HttpRequestMessage(){
            Method = new HttpMethod("GET"),
            RequestUri = new Uri(_httpClient.BaseAddress + requestUri),
        };

        if(includeCreds){
            requestMessage.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        }

        HttpResponseMessage response = await this._httpClient.SendAsync(requestMessage);

        if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized){
            Console.WriteLine("Navigated from GetRequest");
            this.NavigateToLogin();
            return default;
        }
        if(!response.IsSuccessStatusCode){
            return default;
        }

        T data = await response.Content.ReadFromJsonAsync<T>();
        return data;
        
    }

    private async Task<string> PostRequest(object requestBody, string requestUri, bool includeCreds = true){
        var data = requestBody;
        string dataSerialized = JsonConvert.SerializeObject(data);
        var content = new StringContent(dataSerialized, Encoding.UTF8, "application/json");

        HttpRequestMessage requestMessage = new HttpRequestMessage(){
            Method = new HttpMethod("POST"),
            RequestUri = new Uri(_httpClient.BaseAddress + requestUri),
            Content = content
        };

        if(includeCreds){
            requestMessage.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        }

        try{
            HttpResponseMessage response = await this._httpClient.SendAsync(requestMessage);

            if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized){
                Console.WriteLine("Navigated from PostRequest");
                this.NavigateToLogin();
                return null;
            }

            if(response.IsSuccessStatusCode){
                string responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
        }
        catch(System.Exception ex){
            Console.WriteLine($"Exception occurred: {ex.Message}");
        }

        return null;
    }

    // DEMO STUFF
    public async Task<string> GoTomorrow(){
        HttpRequestMessage requestMessage = new HttpRequestMessage(){
            Method = new HttpMethod("GET"),
            RequestUri = new Uri(_httpClient.BaseAddress + "go_tomorrow"),
        };

        requestMessage.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        
        HttpResponseMessage response = await this._httpClient.SendAsync(requestMessage);

        if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized){
            Console.WriteLine("Navigated from GetRequest");
            this.NavigateToLogin();
            return default;
        }
        if(!response.IsSuccessStatusCode){
            return default;
        }

        string data = await response.Content.ReadAsStringAsync();
        return data;
    }
    // ===

    public async Task<string> Login(int userID, string password){
        var data = new RequestBody.Login(){
            UserID = userID,
            Password = password
        };

        return await this.PostRequest(data, "login");   
    }

    public async Task<Habit[]> GetHabits(){
        return await GetRequest<Habit[]>("get_habits");
    }

    public async Task<HabitRecord[]> GetTodayHabits(){
        return await GetRequest<HabitRecord[]>("get_records");
    }

    public async Task<string> AddHabit(string habitName, string habitDesc, int userID){
        var data = new RequestBody.AddHabit(){
            Name = habitName,
            Desc = habitDesc,
        };

        return await this.PostRequest(data, "add_habit");
    }
    public async Task<string> RemoveHabit(int habitID){
        var data = new RequestBody.RemoveHabit(){
            ID = habitID
        };

        return await this.PostRequest(data, "remove_habit");
    }
    
    public async Task<string> EditHabit(int habitID, string habitName, string habitDesc){
        var data = new RequestBody.EditHabit(){
            ID = habitID,
            Name = habitName,
            Desc = habitDesc
        };

        return await this.PostRequest(data, "edit_habit");
    }
    
    public async Task<string> MarkDoneRequest(int habitID){
        var data = new RequestBody.MarkDone(){
                HabitID = habitID
        };

        return await this.PostRequest(data, "mark_done");
    }
}
