using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace IliasScraper;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a Playwright instance
        using var playwright = await Playwright.CreateAsync();

        // Launch a new browser instance
        await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false // Set to true if you don't want to see the browser
        });

        // Open a new page in the browser
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Navigate to the login page
        Console.WriteLine("Navigating to the login page...");
        await page.GotoAsync("https://ilias-guests.fhv.at/"); // Replace with the actual login URL

        // Wait until the user has logged in (you can customize the selector)
        Console.WriteLine("Please log in to your account...");
        await page.WaitForSelectorAsync("h1[class='il-page-content-header media-heading ilHeader ']", new PageWaitForSelectorOptions
        {
            Timeout = 0 // Wait indefinitely until the selector appears
        });

        Console.WriteLine("Login detected. please select the test results to scrape.");
        
        // Filter for the specific element by class and text content
        var header = page.Locator("h2.ilHeader.ilTableHeaderTitle", new PageLocatorOptions
        {
            HasText = "Detaillierte Testergebnisse für Testdurchlauf",
        });

        await header.WaitForAsync();
        
        // Check if the element is found and perform actions
        if (await header.IsVisibleAsync())
        {
            Console.WriteLine("Detailed test results found. starting scraping...");
            // Perform further actions on the element if needed
        }
        else
        {
            await Console.Error.WriteLineAsync("Detailed test results not found.");
        }
        
        var element = page.Locator("h1[class^='il-page-content-header']");
        string? content = await element.TextContentAsync();

        content = string.Join("", content.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c) && !char.IsPunctuation(c)));
        
        string baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IliasScraperResults", content!.Replace(" ",""));

        if (!Directory.Exists(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
        }
        
        var rowCount = await page.Locator("html > body > div:nth-of-type(2) > main > div:nth-of-type(2) > div > div > div > div:nth-of-type(3) > div:nth-of-type(4) > div > div > form > div:nth-of-type(2) > div > table > tbody > tr").CountAsync();

        // Loop through each row and click the corresponding button
        for (int i = 1; i <= rowCount; i++)
        {
            var selector = $"html > body > div:nth-of-type(2) > main > div:nth-of-type(2) > div > div > div > div:nth-of-type(3) > div:nth-of-type(4) > div > div > form > div:nth-of-type(2) > div > table > tbody > tr:nth-of-type({i}) > td:nth-of-type(3) > a";
            Console.WriteLine($"Clicking button in row {i}...");
                
            // Click the button in the current row
            await page.ClickAsync(selector);
            
            var testNameHeader = page.Locator("h2[class^='ilc']");
            string testName = await testNameHeader.TextContentAsync();
            
            string filteredtestName = Regex.Replace(testName, @"\s*\(\d+punkte\)$", "");

            await page.Locator("div.ilc_question_Standard").Last.ScrollIntoViewIfNeededAsync();

            await Task.Delay(TimeSpan.FromMilliseconds(300));

            await page.Locator("div.ilc_question_Standard").Last.ScreenshotAsync(new()
            {
                Path = Path.Combine(baseFolder, filteredtestName + ".png")
            });
            
            await page.ClickAsync(
                "html > body > div:nth-of-type(3) > main > div:nth-of-type(2) > div > div > div > ul > li:nth-of-type(1) > a");
        }

        Console.WriteLine("Done. results stored in folder:" + baseFolder);
        // Close the browser
        await browser.CloseAsync();
    }
}