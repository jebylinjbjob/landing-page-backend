namespace landing_page_backend;

public static class DateSeed
{
    public static void SeedData(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        //以下加入想要的測試資料
    }
}
