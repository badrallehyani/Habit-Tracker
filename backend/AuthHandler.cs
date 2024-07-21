using System.Security.Claims;
using Microsoft.AspNetCore.Server.IISIntegration;
using StackExchange.Redis;

public class AuthHandler{
    private readonly RequestDelegate _next;

    private readonly ConnectionMultiplexer _ConMul;
    private readonly IDatabase _database;

    private readonly List<string> _exceptionPaths;


    public AuthHandler(RequestDelegate next, string url, List<string> exceptionPaths){
        this._next = next;

        this._ConMul = ConnectionMultiplexer.Connect(url);
        this._database = _ConMul.GetDatabase();

        this._exceptionPaths = exceptionPaths;
    }

    public async Task InvokeAsync(HttpContext context){

        Console.Write($"Attempting To Access: '{context.Request.Path}' - ");

        if(_exceptionPaths.IndexOf(context.Request.Path) != -1){
            Console.WriteLine("Exception URL");
            await _next(context);
            return;
        }

        string userIDString = context.Request.Cookies["userid"];
        string token  = context.Request.Cookies["token"];

        if(userIDString == null || token == null){
            context.Response.StatusCode = 401;
            Console.WriteLine("Missing 'UserID' or 'Token' in Cookies");
            await context.Response.WriteAsync("Missing 'UserID' or 'Token' in Cookies");
            return;
        }
        bool isParsed = int.TryParse(userIDString, out int userID);
        
        if(!isParsed){
            context.Response.StatusCode = 401;
            Console.WriteLine("UserID Must Be An Integer");
            await context.Response.WriteAsync("UserID Must Be An Integer");
            return;
        }

        bool tokenIsOk = await this.IsValidToken(userID, token);
        if(!tokenIsOk){
            context.Response.StatusCode = 401;
            Console.WriteLine("Invalid Token/User");
            await context.Response.WriteAsync("Invalid Token/User");
            return;
        }

        context.Items["userID"] = userID;
        context.Items["token"] = token;

        Console.WriteLine("Success");
        await _next(context);
    }

    private async Task<bool> IsValidToken(int userID, string token){
        string recordName = RecordNameCreator.Token(userID);
        string CorrectToken = await this._database.StringGetAsync(recordName);
        
        if(CorrectToken == null){
            return false;
        }

        return CorrectToken.Equals(token);
    }
}