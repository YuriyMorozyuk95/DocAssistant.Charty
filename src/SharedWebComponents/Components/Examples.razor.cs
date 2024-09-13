using Microsoft.AspNetCore.Components;  
using Microsoft.AspNetCore.Components.Web;  
using System;  
using System.Collections.Generic;  
using System.Linq;  
using System.Threading.Tasks;  
  
namespace SharedWebComponents.Components  
{  
    public sealed partial class Examples  
    {  
        [Parameter, EditorRequired] public required string Message { get; set; }  
        [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }  
          
        [Inject] public required ApiClient Client { get; set; }  
          
        private List<Example> _examples = new();  
        private bool _isLoadingExamples;  
        private Task _getExamplesTask;  
  
        private string Question1 { get; set; } = "What is included in my Northwind Health Plus plan that is not in standard?";
        private string Question2 { get; set; } = "What happens in a performance review?";
        private string Question3 { get; set; } = "What does a Product Manager do?";
  
        protected override void OnInitialized()  
        {  
            _getExamplesTask = GetExamplesAsync();  
        }  
  
        private async Task GetExamplesAsync()  
        {  
            _isLoadingExamples = true;  
            try  
            {  
                var examples = await Client.GetAllExamplesAsync(CancellationToken.None).Take(3).ToListAsync();  
                _examples = examples;
  
                if (_examples.Count >= 3)  
                {  
                    var random = new Random();  
                    Question1 = _examples[random.Next(_examples.Count)].UserPromptExample;  
                    Question2 = _examples[random.Next(_examples.Count)].UserPromptExample;  
                    Question3 = _examples[random.Next(_examples.Count)].UserPromptExample;  
                }  
  
                StateHasChanged();  
            }  
            finally  
            {  
                _isLoadingExamples = false;  
                StateHasChanged();  
            }  
        }  
  
        private async Task OnClickedAsync(string exampleText)  
        {  
            if (OnExampleClicked.HasDelegate)  
            {  
                await OnExampleClicked.InvokeAsync(exampleText);  
            }  
        }  
    }  
}  
