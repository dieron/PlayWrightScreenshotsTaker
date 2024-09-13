using System;
using System.Diagnostics;
using Microsoft.Playwright;

const int resolution_x = 320;
const int resolution_y = 240;

const float scale = 2.0f;

TimeSpan screenshotDelay = TimeSpan.FromSeconds(0.1);

Console.WriteLine("Hello, World!");
Console.WriteLine("Here we are going to run a PlayRight session, open a browser in a given resolution, and take a screenshot each time the content of a web page is changed.");

var playwright = Playwright.CreateAsync().Result;

// page is flickering while taking a screenshot in Chromium
await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
{
	Headless = false
});

var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
	AcceptDownloads = false,
	IsMobile = false,
	HasTouch = true,
	ViewportSize = new ViewportSize
	{
		Width = (int)(resolution_x * scale),
		Height = (int)(resolution_y * scale)
	},
	ScreenSize = new ScreenSize
	{
		Width = resolution_x,
		Height = resolution_y
	},
	DeviceScaleFactor = scale,

});

var page = await context.NewPageAsync();

await page.GotoAsync("https://www.google.com");

// waiting for a page to be loaded and rendered completely
await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

DateTime lastScreenshotTime = DateTime.UtcNow;

// Inject a script to observe DOM changes
await page.EvaluateAsync(@"() => {
    const observer = new MutationObserver((mutationsList, observer) => {
        console.log('DOM change detected');
        window.notifyDomChange();
    });

    observer.observe(document, { attributes: true, childList: true, subtree: true });

    window.notifyDomChange = () => {
        // This function will be called whenever a DOM change is detected
        console.log('DOM change notification sent');
    };
}");


// Handle the notifications in C#
page.Console += async (_, msg) =>
{
	if (msg.Text.Contains("DOM change detected"))
	{
		Console.WriteLine("DOM change detected!");

		// Take a screenshot or perform any other action
		var timeSinceLastScreenshot = DateTime.UtcNow - lastScreenshotTime;

		if (timeSinceLastScreenshot > screenshotDelay)
		{
			lastScreenshotTime = DateTime.UtcNow;

			var msSinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
			var last3Digits = msSinceEpoch.Milliseconds.ToString().PadLeft(3, '0');
			var screenshotPath = $"screenshots/screenshot_{DateTime.Now:yyyyMMdd_HHmmss}_{last3Digits}.png";

			_ = await page.ScreenshotAsync(new PageScreenshotOptions
			{
				Path = screenshotPath,
				Type = ScreenshotType.Png,
				Caret = ScreenshotCaret.Initial,
			});

			Console.WriteLine($"Screenshot saved to {screenshotPath}");
		}
	}
};

// waiting for a user to close the console or browser
Console.WriteLine("Press any key to close the browser and exit the program...");
Console.Read();

await browser.CloseAsync();

Console.WriteLine("Goodbye, World!");
