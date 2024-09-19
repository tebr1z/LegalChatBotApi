﻿
public class UserStatusService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserStatusService> _logger;

    public UserStatusService(IServiceProvider serviceProvider, ILogger<UserStatusService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<HuquqDbContext>();

                    // Get all users
                    var users = dbContext.Users.ToList();

                    foreach (var user in users)
                    {
                        // Check and reset RequestCount every 24 hours
                        if (!user.IsPremium)
                        {
                            if ((DateTime.Now - user.LastQuestionDate).TotalHours >= 24)
                            {
                                user.RequestCount = 10; // Reset to 10
                                user.LastQuestionDate = DateTime.Now;
                            }
                        }

                        // Check if premium status has expired
                        if (user.IsPremium && user.PremiumExpirationDate.HasValue)
                        {
                            if (DateTime.Now >= user.PremiumExpirationDate.Value)
                            {
                                user.IsPremium = false;
                                user.PremiumExpirationDate = null; // Reset the expiration date
                            }
                        }
                    }

                    // Save changes to the database
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resetting request counts and checking premium status.");
            }

            // Run this check every hour
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
