using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.VisualBasic;
using RequestBody;

/*
TODO


    - Login Page
        - Token Creator
            - set expiration
    - Register Page

*/

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(corsBuilder => corsBuilder
 .WithOrigins(builder.Configuration["Frontend"])
 .AllowAnyMethod()
 .AllowAnyHeader()
 .AllowCredentials()
);

string redisHost = builder.Configuration["RedisHost"];

var redisHelper = new RedisHelper(redisHost);
app.UseMiddleware<AuthHandler>(redisHost, new List<string> { "/login", "/init_db" });

static async Task HandlePostRequest<RequestBodyModel>(HttpContext context, Func<RequestBodyModel, Task<string>> function){
    RequestBodyModel body;

    try{
        body = await JsonSerializer.DeserializeAsync<RequestBodyModel>(context.Request.Body);
        if(body is IHasUserID castedBody){
            // the condition checks whether body is a IHasUserID or not
            // and if it is, it `castedBody = (IHasUserID) body`

            // Read UserID from context.Items
            context.Items.TryGetValue("userID", out object userID);
            // setting the userID value to be used in RedisHelper
            castedBody.UserID = (int) userID;
        }
    }
    catch (JsonException)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid Data");
        return;
    }

    string response = await function(body);
    await context.Response.WriteAsync(response);
}

static async Task<ResponseBodyModel> HandleGetRequest<ResponseBodyModel>
            (
                HttpContext context, 
                Func<   int, Task<ResponseBodyModel>   > function
            ){
    int userID = (int) context.Items["userID"];
    var response = await function(userID);
    return response;
}

app.MapGet("/", ()=>{
    return "yes";
});

app.MapGet("/init_db", async ()=>{
    if(await redisHelper.InitDB()){
        return "ok";
    };
    return "bad";
});

// == GET ==
app.MapGet("/get_records", async (HttpContext context) =>{
    return await HandleGetRequest<HabitRecord[]>(context, redisHelper.GetTodayRecords);
});

app.MapGet("/get_habits", async (HttpContext context)=>{
    return await HandleGetRequest<Habit[]>(context, redisHelper.GetHabits);
});

app.MapGet("/go_tomorrow", async(HttpContext context)=>{
    return await HandleGetRequest<string>(context, redisHelper.AddToAddedDays);
});

// == POST ==

app.MapPost("/add_habit", async(HttpContext context)=>{
    await HandlePostRequest<RequestBody.AddHabit>(context, redisHelper.AddHabit);
});

app.MapPost("/edit_habit", async (HttpContext context)=>{
    await HandlePostRequest<RequestBody.EditHabit>(context, redisHelper.EditHabit);
});

app.MapPost("/remove_habit", async (HttpContext context)=>{
    await HandlePostRequest<RequestBody.RemoveHabit>(context, redisHelper.RemoveHabit);
});

app.MapPost("/mark_done", async(HttpContext context)=>{
    await HandlePostRequest<RequestBody.MarkDone>(context, redisHelper.MarkDone);
});

app.MapPost("/login", async(HttpContext context)=>{
    var body = await context.Request.ReadFromJsonAsync<RequestBody.GetToken>();
    var response = new HttpResponseMessage();

    string token = await redisHelper.GetToken(body);

    if(token.StartsWith("Error")){
        await context.Response.WriteAsync(token);
        return;
    }

    var cookieOption = new CookieOptions()
    {
        Secure = true,
        SameSite = SameSiteMode.None,
    };

    context.Response.Cookies.Append("token", token, cookieOption);
    context.Response.Cookies.Append("userID", body.UserID.ToString(), cookieOption);

    await context.Response.WriteAsync("Ok");
    
});

app.Run();