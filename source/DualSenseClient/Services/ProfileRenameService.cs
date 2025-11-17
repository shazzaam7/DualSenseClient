using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using DualSenseClient.ViewModels.Controls;
using FluentAvalonia.UI.Controls;

namespace DualSenseClient.Services;

public interface IProfileRenameService
{
    Task<bool> RenameProfileAsync(ControllerProfileViewModel viewModel);
}

public class ProfileRenameService : IProfileRenameService
{
    public async Task<bool> RenameProfileAsync(ControllerProfileViewModel viewModel)
    {
        if (viewModel.SelectedProfile == null)
        {
            return false;
        }

        ContentDialog dialog = new ContentDialog
        {
            Title = "Rename Profile",
            Content = CreateRenameDialogContent(viewModel.SelectedProfile.Name),
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel"
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            TextBox? textBox = dialog.Content as TextBox;
            if (!string.IsNullOrWhiteSpace(textBox?.Text))
            {
                // Set the NewProfileName property in the ViewModel
                viewModel.NewProfileName = textBox.Text.Trim();
                // Call the rename command
                viewModel.RenameSelectedProfileCommand.Execute(null);
                return true;
            }
        }

        return false;
    }

    private TextBox CreateRenameDialogContent(string currentName)
    {
        TextBox textBox = new TextBox
        {
            Text = currentName,
            Watermark = "Enter profile name",
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Select all text for easy editing
        textBox.AttachedToVisualTree += (s, e) =>
        {
            textBox.SelectAll();
            textBox.Focus();
        };

        return textBox;
    }
}