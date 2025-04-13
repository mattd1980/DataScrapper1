using Microsoft.Playwright;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreightningScrapper;

public class OrderTracker
{
    public async Task<List<StatusHistory>> GetStatusHistoryAsync(string trackingNumber)
    {
        var results = new List<StatusHistory>();

        try
        {
            AppLogger.Info("Launching browser...");
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            AppLogger.Info("Navigating to GRG tracking page...");
            await page.GotoAsync("https://grguweb.tmwcloud.com/trace/external.msw", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000
            });

            AppLogger.Info("Filling in tracking number...");
            var inputFields = await page.QuerySelectorAllAsync("input[name='search_value[]']");
            if (inputFields.Count == 0)
            {
                AppLogger.Error("Could not find input fields.");
                return results;
            }

            await inputFields[0].FillAsync(trackingNumber);

            AppLogger.Info("Waiting for popup window to open after submission...");
            var popupTask = page.WaitForPopupAsync();
            await page.ClickAsync("input[name='Submit']");
            var popup = await popupTask;

            AppLogger.Success("Popup window loaded.");

            // Debug step: print all section headers
            var headers = await popup.QuerySelectorAllAsync("div.k-header.k-state-selected.tmw_section");
            foreach (var h in headers)
            {
                var txt = await h.InnerTextAsync();
                AppLogger.Info($"Found section header: \"{txt}\"");
            }

            // Wait for the "Historique du Status" section to appear
            AppLogger.Info("Waiting for 'Historique du Status' section to load...");
            var statusSectionHeader = await popup.WaitForSelectorAsync(
                "xpath=//div[contains(@class, 'tmw_section') and contains(text(), 'Historique du Status')]",
                new() { Timeout = 10000 }
            );

            if (statusSectionHeader == null)
            {
                AppLogger.Warn("Status history section not found.");
                return results;
            }

            // Find parent container of the section
            var widgetHandle = await statusSectionHeader.EvaluateHandleAsync("node => node.closest('div.k-widget')");
            var statusWidgetContainer = widgetHandle.AsElement();
            if (statusWidgetContainer == null)
            {
                AppLogger.Warn("Could not convert widget container to element.");
                return results;
            }

            // Query rows inside the correct section
            var statusRows = await statusWidgetContainer.QuerySelectorAllAsync("div.k-grid-content tbody tr");
            foreach (var row in statusRows)
            {
                var cells = await row.QuerySelectorAllAsync("td");
                if (cells.Count >= 2)
                {
                    var timestamp = await cells[0].InnerTextAsync();
                    var status = await cells[1].InnerTextAsync();
                    results.Add(new StatusHistory(timestamp, status));
                    AppLogger.Info($"Row: {timestamp} | {status}");
                }
            }

            if (results.Count == 0)
            {
                AppLogger.Warn("Table loaded, but no status rows found.");
            }
            else
            {
                AppLogger.Success($"Successfully extracted {results.Count} status entries.");
            }

            return results;
        }
        catch (PlaywrightException pex)
        {
            AppLogger.Error($"Playwright error: {pex.Message}");
            return results;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Unexpected error: {ex.Message}");
            return results;
        }
    }
}