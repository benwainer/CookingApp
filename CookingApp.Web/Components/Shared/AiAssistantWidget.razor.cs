using CookingApp.Core.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.RegularExpressions;

namespace CookingApp.Web.Components.Shared;

public partial class AiAssistantWidget
{
    private bool isOpen = false;
    private bool isThinking = false;
    private string userInput = string.Empty;
    private List<AiChatMessage> messages = [];
    private ElementReference messagesDiv;

    // Detect current recipe ID from URL if user is on a recipe page
    private int? CurrentRecipeId
    {
        get
        {
            var uri = Nav.Uri;
            var match = Regex.Match(uri, @"/recipe/(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }
    }

    private void ToggleOpen() => isOpen = !isOpen;

    private async Task Send()
    {
        if (string.IsNullOrWhiteSpace(userInput) || isThinking) return;

        var userMsg = userInput.Trim();
        userInput = string.Empty;
        messages.Add(new AiChatMessage("user", userMsg));
        isThinking = true;
        StateHasChanged();

        try
        {
            var response = await Api.AiChatAsync(new AiChatRequest(
                History: messages.SkipLast(1).ToList(),
                NewMessage: userMsg,
                CurrentRecipeId: CurrentRecipeId
            ));

            messages.Add(new AiChatMessage("assistant", response.Reply));
        }
        catch
        {
            messages.Add(new AiChatMessage("assistant",
                "Sorry, I couldn't connect to the AI right now. Please try again."));
        }

        isThinking = false;
        StateHasChanged();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await Send();
    }

    // Simple markdown-like formatting: **bold** and newlines to <br>
    private static string FormatMessage(string text)
    {
        text = System.Net.WebUtility.HtmlEncode(text);
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        text = text.Replace("\n", "<br/>");
        return text;
    }
}
