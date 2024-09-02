﻿using Microsoft.AspNetCore.Components;  
using MudBlazor;  
using Shared.Models;  
using SharedWebComponents.Services;  
using System;  
using System.Collections.Generic;  
using System.Linq;  
using System.Threading;  
using System.Threading.Tasks;  
  
namespace SharedWebComponents.Pages  
{  
    public sealed partial class ExamplePage : IDisposable  
    {  
        private readonly CancellationTokenSource _cancellationTokenSource = new();  
        private MudForm _form = null!;  
        private Task _getSchemasTask = null!;  
        private Task _getExamplesTask = null!;  
        private bool _isLoadingSchemas = false;  
        private bool _isLoadingExamples = false;  
        private bool _isOperationInProgress = false;  
        private string _filter = "";  
        private string _exampleFilter = "";  
        private string ConnectionString { get; set; } = string.Empty;  
        private Example Example { get; set; } = new Example();  
        private readonly List<string> _logs = new();  
        private readonly HashSet<TableSchema> _tableSchemas = new();  
        private readonly HashSet<Example> _examples = new();  
  
        [Inject] public required ApiClient Client { get; set; }  
        [Inject] public required ISnackbar Snackbar { get; set; }  
        [Inject] public required ILogger<Docs> Logger { get; set; }  
  
        protected override void OnInitialized()  
        {  
            _getSchemasTask = GetTableSchemasAsync();  
            _getExamplesTask = GetExamplesAsync();  
        }  
  
        private bool OnFilter(TableSchema schema) =>  
            schema is not null &&  
            (string.IsNullOrWhiteSpace(_filter) ||  
             schema.TableName.Contains(_filter, StringComparison.OrdinalIgnoreCase));  
  
        private bool OnExampleFilter(Example example) =>  
            example is not null &&  
            (string.IsNullOrWhiteSpace(_exampleFilter) ||  
             example.TableName.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase) ||  
             example.DatabaseName.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase) ||  
             example.ServerName.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase) ||  
             example.SqlExample.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase) ||  
             example.UserPromptExample.Contains(_exampleFilter, StringComparison.OrdinalIgnoreCase));  
  
        private async Task GetTableSchemasAsync()  
        {  
            _isLoadingSchemas = true;  
            try  
            {  
                var schemas = await Client.GetAllDatabaseSchemasAsync(CancellationToken.None).ToListAsync();  
                foreach (var schema in schemas)  
                {  
                    _tableSchemas.Add(schema);  
                    StateHasChanged();  
                }  
            }  
            finally  
            {  
                _isLoadingSchemas = false;  
                StateHasChanged();  
            }  
        }  
  
        private async Task GetExamplesAsync()  
        {  
            _isLoadingExamples = true;  
            try  
            {  
                var examples = await Client.GetAllExamplesAsync(CancellationToken.None).ToListAsync();  
                foreach (var example in examples)  
                {  
                    _examples.Add(example);  
                    StateHasChanged();  
                }  
            }  
            finally  
            {  
                _isLoadingExamples = false;  
                StateHasChanged();  
            }  
        }  
  
        private async Task SubmitConnectionStringForUploadAsync()  
        {  
            if (!string.IsNullOrWhiteSpace(ConnectionString))  
            {  
                _isOperationInProgress = true;  
                _logs.Clear();  
                try  
                {  
                    await foreach (var logMessage in Client.UploadDatabaseSchemaAsync(ConnectionString, CancellationToken.None))  
                    {  
                        _logs.Add(logMessage);  
                        StateHasChanged();  
                    }  
  
                    Logger.LogInformation("Log Messages: {x}", _logs);  
                    Snackbar.Add($"Uploaded database schema. Check logs for details.", Severity.Success, options =>  
                    {  
                        options.ShowCloseIcon = true;  
                        options.VisibleStateDuration = 10_000;  
                    });  
  
                    // Refresh the table schemas  
                    _tableSchemas.Clear();  
                    await GetTableSchemasAsync();  
                }  
                finally  
                {  
                    _isOperationInProgress = false;  
                    StateHasChanged();  
                }  
            }  
        }  
  
        private async Task SubmitExampleAsync()  
        {  
            if (Example != null)  
            {  
                _isOperationInProgress = true;  
                _logs.Clear();  
                try  
                {  
                    var documentId = await Client.UploadExampleAsync(Example, CancellationToken.None);  
                    if (documentId != null)  
                    {  
                        Snackbar.Add($"Uploaded example with DocumentId: {documentId}", Severity.Success, options =>  
                        {  
                            options.ShowCloseIcon = true;  
                            options.VisibleStateDuration = 10_000;  
                        });  
  
                        // Refresh the examples  
                        _examples.Clear();  
                        await GetExamplesAsync();  
                    }  
                    else  
                    {  
                        Snackbar.Add("Failed to upload example.", Severity.Error, options =>  
                        {  
                            options.ShowCloseIcon = true;  
                            options.VisibleStateDuration = 10_000;  
                        });  
                    }  
                }  
                finally  
                {  
                    _isOperationInProgress = false;  
                    StateHasChanged();  
                }  
            }  
        }  
  
        private void ClearConnectionString()  
        {  
            ConnectionString = string.Empty;  
        }  
  
        private void ClearExampleFields()  
        {  
            Example = new Example();  
        }  
  
        public void Dispose() => _cancellationTokenSource.Cancel();  
    }  
}  
