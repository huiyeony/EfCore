using EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

public class Program
{
    [DbFunction]
    public static Double? CalcAverage(int itemId)
    {
        throw new NotImplementedException("DO NOT CALL");
    }

    static void Main(string[] args)
    {
        DbCommands.InitializeDb(forceReset : true);//강제 초기화 

        Console.WriteLine("Enter Commands ");
        Console.WriteLine("[0] Initialize Db ");
        Console.WriteLine("[1] ShowItems ");
        Console.WriteLine("[2] CalcAverageReviewScore ");
        Console.Write(">");

        while (true)
        {
            string cmd = Console.ReadLine();

            switch (cmd)
            {
                case "0":
                    DbCommands.InitializeDb(true);
                    break;
                case "1":
                    DbCommands.ShowItems();
                    break;
                case "2":
                    DbCommands.CalcAverageReviewScore();
                    break;

            }
        }
        
    }
}