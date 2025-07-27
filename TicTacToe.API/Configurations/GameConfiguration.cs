namespace TicTacToe.Configurations;

public class GameConfiguration
{
    private readonly IConfiguration _configuration;
    public int BoardSize {get; set;}
    public int WinCondition {get; set;}
    

    public GameConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        BoardSize = _configuration.GetValue<int>("BoardSize");
        WinCondition = _configuration.GetValue<int>("WinCondition");
    }
    
}